using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmOrden : Form
    {
        readonly Orden orden;
        readonly List<Platillo> menu;

        public bool Cobrada { get; private set; }

        readonly FlowLayoutPanel categorias = new FlowLayoutPanel();
        readonly ListView lstMenu = new ListView();
        readonly NumericUpDown numCantidad = new NumericUpDown();
        readonly TextBox txtNota = new TextBox();
        readonly DataGridView grid = new DataGridView();
        readonly CheckBox chkPropina = new CheckBox();
        readonly Label lblSubtotal = new Label();
        readonly Label lblPropina = new Label();
        readonly Label lblIva = new Label();
        readonly Label lblTotal = new Label();
        readonly Button btnCobrar;

        public FrmOrden(Orden orden, List<Platillo> menu)
        {
            this.orden = orden;
            this.menu = menu;
            Estilo.Formulario(this, "Mesa " + orden.Mesa, 1000, 640);

            var division = new SplitContainer { Dock = DockStyle.Fill, BackColor = Estilo.Crema };

            // el splitter truena si se le pone la distancia antes de que tenga tamano, por eso va en el Load
            Load += (s, e) =>
            {
                division.Panel1MinSize = 360;
                division.SplitterDistance = 430;
            };

            // izquierda el menu
            categorias.Dock = DockStyle.Top;
            categorias.AutoSize = true;
            categorias.Padding = new Padding(8);
            foreach (var cat in new[] { "Todo" }.Concat(Almacen.Categorias))
            {
                var b = Estilo.Boton(cat, Estilo.Gris);
                string c = cat;
                b.Click += (s, e) => MostrarMenu(c);
                categorias.Controls.Add(b);
            }

            lstMenu.Dock = DockStyle.Fill;
            lstMenu.View = View.Details;
            lstMenu.FullRowSelect = true;
            lstMenu.MultiSelect = false;
            lstMenu.HideSelection = false;
            lstMenu.Columns.Add("Platillo", 250);
            lstMenu.Columns.Add("Precio", 90, HorizontalAlignment.Right);
            lstMenu.DoubleClick += (s, e) => AgregarSeleccionado();

            var agregar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 90, Padding = new Padding(8) };
            numCantidad.Minimum = 1;
            numCantidad.Maximum = 50;
            numCantidad.Width = 60;
            txtNota.Width = 220;
            var btnAgregar = Estilo.Boton("Agregar", Estilo.Libre);
            btnAgregar.Click += (s, e) => AgregarSeleccionado();
            agregar.Controls.Add(new Label { Text = "Cant.", AutoSize = true, Margin = new Padding(4, 8, 0, 0) });
            agregar.Controls.Add(numCantidad);
            agregar.Controls.Add(new Label { Text = "Nota", AutoSize = true, Margin = new Padding(8, 8, 0, 0) });
            agregar.Controls.Add(txtNota);
            agregar.Controls.Add(btnAgregar);

            division.Panel1.Controls.Add(lstMenu);
            division.Panel1.Controls.Add(agregar);
            division.Panel1.Controls.Add(categorias);

            // derecha la orden
            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("cant", "Cant.");
            grid.Columns.Add("platillo", "Platillo");
            grid.Columns.Add("nota", "Nota");
            grid.Columns.Add("precio", "Precio");
            grid.Columns.Add("subtotal", "Subtotal");
            grid.Columns["cant"].FillWeight = 40;
            grid.Columns["platillo"].FillWeight = 150;
            grid.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
            var btnMas = Estilo.Boton("+1", Estilo.Gris);
            var btnMenos = Estilo.Boton("-1", Estilo.Gris);
            var btnQuitar = Estilo.Boton("Quitar", Estilo.Ocupada);
            btnMas.Click += (s, e) => Cambiar(1);
            btnMenos.Click += (s, e) => Cambiar(-1);
            btnQuitar.Click += (s, e) => QuitarSeleccionada();
            acciones.Controls.AddRange(new Control[] { btnMas, btnMenos, btnQuitar });

            var totales = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 200,
                ColumnCount = 2,
                Padding = new Padding(8),
                BackColor = Color.White
            };
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            chkPropina.Text = "Propina 10%";
            chkPropina.AutoSize = true;
            chkPropina.Checked = orden.ConPropina;
            chkPropina.CheckedChanged += (s, e) =>
            {
                orden.ConPropina = chkPropina.Checked;
                Refrescar();
            };

            foreach (var l in new[] { lblSubtotal, lblPropina, lblIva, lblTotal })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Right;
            }
            lblIva.ForeColor = Estilo.Gris;
            lblTotal.Font = Estilo.Total;
            lblTotal.ForeColor = Estilo.Vino;

            totales.Controls.Add(new Label { Text = "Subtotal", AutoSize = true }, 0, 0);
            totales.Controls.Add(lblSubtotal, 1, 0);
            totales.Controls.Add(chkPropina, 0, 1);
            totales.Controls.Add(lblPropina, 1, 1);
            totales.Controls.Add(new Label { Text = "TOTAL", AutoSize = true, Font = Estilo.Negrita, Anchor = AnchorStyles.Left }, 0, 2);
            totales.Controls.Add(lblTotal, 1, 2);
            totales.Controls.Add(new Label { Text = "IVA incluido", AutoSize = true, ForeColor = Estilo.Gris }, 0, 3);
            totales.Controls.Add(lblIva, 1, 3);

            var botonesFin = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, FlowDirection = FlowDirection.RightToLeft };
            btnCobrar = Estilo.Boton("Cobrar", Estilo.Vino);
            var btnGuardar = Estilo.Boton("Guardar y volver", Estilo.Gris);
            btnCobrar.Click += (s, e) => Cobrar();
            btnGuardar.Click += (s, e) => Close();
            botonesFin.Controls.AddRange(new Control[] { btnCobrar, btnGuardar });
            totales.Controls.Add(botonesFin, 0, 4);
            totales.SetColumnSpan(botonesFin, 2);

            division.Panel2.Controls.Add(grid);
            division.Panel2.Controls.Add(acciones);
            division.Panel2.Controls.Add(totales);

            Controls.Add(division);
            Controls.Add(Estilo.Encabezado("Mesa " + orden.Mesa));

            MostrarMenu("Todo");
            Refrescar();
        }

        void MostrarMenu(string categoria)
        {
            lstMenu.BeginUpdate();
            lstMenu.Items.Clear();
            var lista = categoria == "Todo" ? menu : menu.Where(x => x.Categoria == categoria).ToList();
            foreach (var p in lista.OrderBy(x => x.Categoria).ThenBy(x => x.Nombre))
            {
                var item = new ListViewItem(p.Nombre) { Tag = p };
                item.SubItems.Add(Ticket.Q(p.Precio));
                lstMenu.Items.Add(item);
            }
            lstMenu.EndUpdate();
        }

        void AgregarSeleccionado()
        {
            if (lstMenu.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Escoge un platillo del menú.", "Agregar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var platillo = (Platillo)lstMenu.SelectedItems[0].Tag;
            orden.Agregar(platillo, (int)numCantidad.Value, txtNota.Text);

            // se limpia para que la nota no se le pegue al siguiente platillo
            numCantidad.Value = 1;
            txtNota.Clear();
            Refrescar();
        }

        LineaOrden Seleccionada()
        {
            if (grid.CurrentRow == null) return null;
            return grid.CurrentRow.Tag as LineaOrden;
        }

        void Cambiar(int diferencia)
        {
            var linea = Seleccionada();
            if (linea == null) return;
            orden.CambiarCantidad(linea, diferencia);
            Refrescar(linea);
        }

        void QuitarSeleccionada()
        {
            var linea = Seleccionada();
            if (linea == null) return;
            orden.Quitar(linea);
            Refrescar();
        }

        void Refrescar(LineaOrden mantener = null)
        {
            grid.Rows.Clear();
            foreach (var l in orden.Lineas)
            {
                int i = grid.Rows.Add(l.Cantidad, l.Platillo.Nombre, l.Nota, Ticket.Q(l.PrecioUnitario), Ticket.Q(l.Subtotal));
                grid.Rows[i].Tag = l;
                if (l == mantener)
                    grid.CurrentCell = grid.Rows[i].Cells[0];
            }

            lblSubtotal.Text = Ticket.Q(orden.Subtotal);
            lblPropina.Text = Ticket.Q(orden.Propina);
            lblIva.Text = Ticket.Q(orden.IvaIncluido);
            lblTotal.Text = Ticket.Q(orden.Total);
            btnCobrar.Enabled = !orden.Vacia;
        }

        void Cobrar()
        {
            using (var f = new FrmCobro(orden.Total))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;

                var venta = Almacen.RegistrarVenta(orden, f.FormaPago);
                Cobrada = true;
                Ticket.Imprimir(Ticket.Texto(orden, venta, f.Recibido), this);
            }
            Close();
        }
    }
}
