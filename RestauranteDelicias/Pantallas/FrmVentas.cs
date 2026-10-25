using System;
using System.Collections.Generic;
using System.Linq;
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
        List<Venta> ventas = new List<Venta>();

        public FrmVentas()
        {
            Estilo.Formulario(this, "Ventas del día", 820, 540);

            var arriba = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
            fecha.Format = DateTimePickerFormat.Long;
            fecha.Width = 260;
            fecha.ValueChanged += (s, e) => Cargar();
            var btnExportar = Estilo.Boton("Exportar CSV", Estilo.Gris);
            btnExportar.Click += (s, e) => Exportar();
            arriba.Controls.Add(new Label { Text = "Día", AutoSize = true, Margin = new Padding(4, 10, 4, 0) });
            arriba.Controls.Add(fecha);
            arriba.Controls.Add(btnExportar);

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("numero", "No.");
            grid.Columns.Add("hora", "Hora");
            grid.Columns.Add("mesa", "Mesa");
            grid.Columns.Add("articulos", "Artículos");
            grid.Columns.Add("propina", "Propina");
            grid.Columns.Add("total", "Total");
            grid.Columns.Add("pago", "Pago");
            foreach (var c in new[] { "propina", "total" })
                grid.Columns[c].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            lblResumen.Dock = DockStyle.Bottom;
            lblResumen.Height = 64;
            lblResumen.Padding = new Padding(12, 8, 12, 8);
            lblResumen.Font = Estilo.Negrita;
            lblResumen.BackColor = System.Drawing.Color.White;

            Controls.Add(grid);
            Controls.Add(lblResumen);
            Controls.Add(arriba);
            Controls.Add(Estilo.Encabezado("Ventas del día"));

            Cargar();
        }

        void Cargar()
        {
            ventas = Almacen.VentasDelDia(fecha.Value);
            grid.Rows.Clear();
            foreach (var venta in ventas.OrderBy(x => x.Numero))
                grid.Rows.Add(venta.Numero.ToString("00000"), venta.Fecha.ToString("HH:mm"), venta.Mesa, venta.Articulos,
                    Ticket.Q(venta.Propina), Ticket.Q(venta.Total), venta.FormaPago);

            decimal efectivo = ventas.Where(v => v.FormaPago == "Efectivo").Sum(v => v.Total);
            decimal tarjeta = ventas.Where(v => v.FormaPago == "Tarjeta").Sum(v => v.Total);

            // la propina va aparte porque es de los meseros, no del restaurante
            lblResumen.Text = string.Format(
                "{0} ventas   ·   Total {1}   ·   Efectivo {2}   ·   Tarjeta {3}{4}Propinas {5}",
                ventas.Count, Ticket.Q(efectivo + tarjeta), Ticket.Q(efectivo), Ticket.Q(tarjeta),
                Environment.NewLine, Ticket.Q(ventas.Sum(v => v.Propina)));
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

                Almacen.ExportarVentas(ventas, dlg.FileName);
                MessageBox.Show(this, "Listo, se guardó el archivo.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
