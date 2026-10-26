using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmVentas : Form
    {
        readonly DateTimePicker fecha = new DateTimePicker();
        readonly DataGridView grid = new DataGridView();
        readonly Label lblResumen = new Label();
        readonly ComboBox cboCajero = new ComboBox();
        List<Venta> ventas = new List<Venta>();

        // el cajero solo ve lo que cobro el, el admin ve todo o escoge un cajero
        int? Cajero
        {
            get
            {
                if (!Sesion.EsAdmin) return Sesion.Id;
                var u = cboCajero.SelectedItem as Usuario;
                return u == null || u.Id == 0 ? (int?)null : u.Id;
            }
        }

        public FrmVentas()
        {
            var titulo = Sesion.EsAdmin ? "Ventas del día" : "Mis ventas del día";
            Estilo.Formulario(this, titulo, 900, 560);

            var arriba = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
            fecha.Format = DateTimePickerFormat.Long;
            fecha.Width = 260;
            fecha.ValueChanged += (s, e) => Cargar();
            var btnActualizar = Estilo.Boton("Actualizar", Estilo.Gris);
            var btnTicket = Estilo.Boton("Reimprimir ticket", Estilo.Azul);
            var btnExportar = Estilo.Boton("Exportar CSV", Estilo.Gris);
            btnActualizar.Click += (s, e) => Cargar();
            btnTicket.Click += (s, e) => Reimprimir();
            btnExportar.Click += (s, e) => Exportar();
            arriba.Controls.Add(new Label { Text = "Día", AutoSize = true, Margin = new Padding(4, 10, 4, 0) });
            arriba.Controls.Add(fecha);
            if (Sesion.EsAdmin)
            {
                cboCajero.DropDownStyle = ComboBoxStyle.DropDownList;
                cboCajero.Width = 220;
                cboCajero.DisplayMember = "Nombre";
                cboCajero.SelectedIndexChanged += (s, e) => Cargar();
                arriba.Controls.Add(new Label { Text = "Cobró", AutoSize = true, Margin = new Padding(14, 10, 4, 0) });
                arriba.Controls.Add(cboCajero);
            }
            arriba.Controls.AddRange(new Control[] { btnActualizar, btnTicket, btnExportar });

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("ticket", "Ticket");
            grid.Columns.Add("hora", "Hora");
            grid.Columns.Add("mesa", "Mesa");
            grid.Columns.Add("articulos", "Artículos");
            grid.Columns.Add("subtotal", "Consumo");
            grid.Columns.Add("propina", "Propina");
            grid.Columns.Add("total", "Total");
            grid.Columns.Add("pago", "Pago");
            grid.Columns.Add("mesero", "Mesero");
            grid.Columns.Add("cajero", "Cobró");
            foreach (var c in new[] { "subtotal", "propina", "total" })
                grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.CellDoubleClick += (s, e) => Reimprimir();

            lblResumen.Dock = DockStyle.Bottom;
            lblResumen.Height = 74;
            lblResumen.Padding = new Padding(12, 8, 12, 8);
            lblResumen.Font = Estilo.Negrita;
            lblResumen.BackColor = System.Drawing.Color.White;

            Controls.Add(grid);
            Controls.Add(lblResumen);
            Controls.Add(arriba);
            Controls.Add(Estilo.Encabezado(titulo));

            Load += (s, e) =>
            {
                if (Sesion.EsAdmin) CargarCajeros();
                Cargar();
            };
        }

        void CargarCajeros()
        {
            List<Usuario> cajeros = null;
            if (!Ui.Seguro(this, () => cajeros = VentaRepo.Cajeros())) return;
            cajeros.Insert(0, new Usuario { Id = 0, Nombre = "Todos" });
            cboCajero.DataSource = cajeros;
        }

        void Cargar()
        {
            List<TotalForma> formas = null;
            int anuladas = 0;
            if (!Ui.Seguro(this, () =>
            {
                var cajero = Cajero;
                ventas = VentaRepo.DelDia(fecha.Value, cajero);
                formas = VentaRepo.PorForma(fecha.Value, cajero);
                anuladas = VentaRepo.Anuladas(fecha.Value, cajero);
            })) return;

            grid.Rows.Clear();
            foreach (var v in ventas)
            {
                int i = grid.Rows.Add(v.NumeroTicket.ToString("00000"), v.Cierre.ToString("HH:mm"),
                    v.MesaNumero.HasValue ? v.MesaNumero.ToString() : v.Tipo, v.Articulos,
                    Ticket.Q(v.Subtotal), Ticket.Q(v.Propina), Ticket.Q(v.Total), v.Formas, v.Mesero ?? "-", v.Cajero ?? "-");
                grid.Rows[i].Tag = v;
            }

            var porForma = string.Join("   ·   ", formas.Select(f => f.Forma + " " + Ticket.Q(f.Total)));
            lblResumen.Text =
                ventas.Count + " ventas   ·   Total " + Ticket.Q(ventas.Sum(v => v.Total)) +
                (porForma.Length > 0 ? "   ·   " + porForma : "") + Environment.NewLine +
                "Consumo " + Ticket.Q(ventas.Sum(v => v.Subtotal)) + "   ·   Propinas " + Ticket.Q(ventas.Sum(v => v.Propina)) +
                (anuladas > 0 ? "   ·   " + anuladas + " platillo(s) anulados" : "");
        }

        void Reimprimir()
        {
            if (grid.CurrentRow == null || !(grid.CurrentRow.Tag is Venta)) return;
            var v = (Venta)grid.CurrentRow.Tag;
            Orden o = null;
            List<Pago> pagos = null;
            if (!Ui.Seguro(this, () =>
            {
                o = OrdenRepo.Cargar(v.Id);
                pagos = OrdenRepo.Pagos(v.Id);
            })) return;
            Ui.Seguro(this, () => Ticket.Imprimir(Ticket.Reimpresion(Ticket.Texto(o, v.NumeroTicket, v.Cierre, pagos)), this));
        }

        void Exportar()
        {
            if (ventas.Count == 0)
            {
                MessageBox.Show(this, "No hay ventas ese día.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV (*.csv)|*.csv";
                dlg.FileName = "ventas-" + fecha.Value.ToString("yyyy-MM-dd") + ".csv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var inv = CultureInfo.InvariantCulture;
                var lineas = new List<string> { "ticket,fecha,mesa,articulos,consumo,propina,total,pago,mesero,cobro" };
                lineas.AddRange(ventas.Select(v => Csv.Linea(
                    v.NumeroTicket, v.Cierre.ToString("yyyy-MM-dd HH:mm", inv),
                    v.MesaNumero.HasValue ? v.MesaNumero.ToString() : v.Tipo, v.Articulos,
                    v.Subtotal.ToString("0.00", inv), v.Propina.ToString("0.00", inv), v.Total.ToString("0.00", inv), v.Formas,
                    v.Mesero, v.Cajero)));

                // con bom para que excel agarre bien las tildes
                if (Ui.Seguro(this, () => File.WriteAllLines(dlg.FileName, lineas, new UTF8Encoding(true))))
                    MessageBox.Show(this, "Listo, se guardó el archivo.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
