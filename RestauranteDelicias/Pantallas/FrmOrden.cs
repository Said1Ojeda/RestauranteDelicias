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
        readonly int mesaId;
        readonly int mesaNumero;
        Orden orden;
        List<Platillo> menu = new List<Platillo>();
        string categoria = "Todo";
        bool cargando;

        public bool Cobrada { get; private set; }

        readonly FlowLayoutPanel categorias = new FlowLayoutPanel();
        readonly TextBox txtBuscar = new TextBox();
        readonly ListView lstMenu = new ListView();
        readonly NumericUpDown numCantidad = new NumericUpDown();
        readonly TextBox txtNota = new TextBox();
        readonly DataGridView grid = new DataGridView();
        readonly NumericUpDown numPersonas = new NumericUpDown();
        readonly CheckBox chkPropina = new CheckBox();
        readonly Label lblSubtotal = new Label();
        readonly Label lblPropina = new Label();
        readonly Label lblDescuento = new Label();
        readonly Label lblDescuentoTexto = new Label();
        readonly Label lblIva = new Label();
        readonly Label lblTotal = new Label();
        readonly Button btnCobrar;
        readonly Button btnEnviar;
        readonly Timer timer = new Timer();

        public FrmOrden(int mesaId, int mesaNumero)
        {
            this.mesaId = mesaId;
            this.mesaNumero = mesaNumero;
            Estilo.Formulario(this, "Mesa " + mesaNumero, 1100, 680);
            WindowState = FormWindowState.Maximized;

            var division = new SplitContainer { Dock = DockStyle.Fill, BackColor = Estilo.Crema };
            Load += (s, e) =>
            {
                division.Panel1MinSize = 380;
                division.SplitterDistance = Math.Max(420, Width * 42 / 100);
            };

            // izquierda el menu
            var buscar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 8, 8, 0) };
            txtBuscar.Dock = DockStyle.Fill;
            txtBuscar.TextChanged += (s, e) => MostrarMenu();
            buscar.Controls.Add(txtBuscar);
            buscar.Controls.Add(new Label { Text = "Buscar", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 4, 6, 0) });

            categorias.Dock = DockStyle.Top;
            categorias.AutoSize = true;
            categorias.Padding = new Padding(8, 4, 8, 4);

            lstMenu.Dock = DockStyle.Fill;
            lstMenu.View = View.Details;
            lstMenu.FullRowSelect = true;
            lstMenu.MultiSelect = false;
            lstMenu.HideSelection = false;
            lstMenu.Font = new Font("Segoe UI", 11f);
            lstMenu.Columns.Add("Platillo", 280);
            lstMenu.Columns.Add("Precio", 100, HorizontalAlignment.Right);
            lstMenu.DoubleClick += (s, e) => AgregarSeleccionado();
            lstMenu.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) AgregarSeleccionado(); };

            var agregar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(8) };
            numCantidad.Minimum = 1;
            numCantidad.Maximum = 50;
            numCantidad.Width = 60;
            txtNota.Width = 220;
            txtNota.MaxLength = 150;
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
            division.Panel1.Controls.Add(buscar);

            // derecha la orden
            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("cant", "Cant.");
            grid.Columns.Add("platillo", "Platillo");
            grid.Columns.Add("nota", "Nota");
            grid.Columns.Add("estado", "Estado");
            grid.Columns.Add("subtotal", "Subtotal");
            grid.Columns["cant"].FillWeight = 40;
            grid.Columns["platillo"].FillWeight = 150;
            grid.Columns["estado"].FillWeight = 70;
            grid.Columns["subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
            var btnMas = Estilo.Boton("+1", Estilo.Gris);
            var btnMenos = Estilo.Boton("-1", Estilo.Gris);
            var btnQuitar = Estilo.Boton("Quitar", Estilo.Ocupada);
            btnEnviar = Estilo.Boton("Enviar a cocina", Estilo.Naranja);
            var btnDescuento = Estilo.Boton("Descuento", Estilo.Azul);
            btnDescuento.Click += (s, e) => Descontar();
            btnMas.Click += (s, e) => Mas();
            btnMenos.Click += (s, e) => Menos();
            btnQuitar.Click += (s, e) => Quitar();
            btnEnviar.Click += (s, e) => Enviar(true);

            numPersonas.Minimum = 1;
            numPersonas.Maximum = 50;
            numPersonas.Width = 55;
            numPersonas.ValueChanged += (s, e) => GuardarDatos();
            acciones.Controls.AddRange(new Control[]
            {
                btnMas, btnMenos, btnQuitar, btnEnviar, btnDescuento,
                new Label { Text = "Personas", AutoSize = true, Margin = new Padding(16, 10, 0, 0) }, numPersonas
            });

            var totales = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 240,
                ColumnCount = 2,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            chkPropina.Text = "Propina " + Configuracion.Propina.ToString("0.##") + "%";
            chkPropina.AutoSize = true;
            chkPropina.Checked = true;
            chkPropina.CheckedChanged += (s, e) => GuardarDatos();

            lblDescuentoTexto.AutoSize = true;
            lblDescuento.ForeColor = lblDescuentoTexto.ForeColor = Estilo.Azul;
            foreach (var l in new[] { lblSubtotal, lblDescuento, lblPropina, lblIva, lblTotal })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Right;
            }
            lblIva.ForeColor = Estilo.Gris;
            lblTotal.Font = Estilo.Total;
            lblTotal.ForeColor = Estilo.Vino;

            totales.Controls.Add(new Label { Text = "Subtotal", AutoSize = true }, 0, 0);
            totales.Controls.Add(lblSubtotal, 1, 0);
            totales.Controls.Add(lblDescuentoTexto, 0, 1);
            totales.Controls.Add(lblDescuento, 1, 1);
            totales.Controls.Add(chkPropina, 0, 2);
            totales.Controls.Add(lblPropina, 1, 2);
            totales.Controls.Add(new Label { Text = "TOTAL", AutoSize = true, Font = Estilo.Negrita, Anchor = AnchorStyles.Left }, 0, 3);
            totales.Controls.Add(lblTotal, 1, 3);
            totales.Controls.Add(new Label { Text = "IVA incluido", AutoSize = true, ForeColor = Estilo.Gris }, 0, 4);
            totales.Controls.Add(lblIva, 1, 4);

            var botonesFin = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, FlowDirection = FlowDirection.RightToLeft };
            btnCobrar = Estilo.Boton("Cobrar", Estilo.Vino);
            // el mesero toma la orden pero el cobro es de caja
            btnCobrar.Visible = Sesion.PuedeCobrar;
            var btnVolver = Estilo.Boton("Volver a mesas", Estilo.Gris);
            btnCobrar.Click += (s, e) => Cobrar();
            btnVolver.Click += (s, e) => Close();
            botonesFin.Controls.AddRange(new Control[] { btnCobrar, btnVolver });
            totales.Controls.Add(botonesFin, 0, 5);
            totales.SetColumnSpan(botonesFin, 2);

            division.Panel2.Controls.Add(grid);
            division.Panel2.Controls.Add(acciones);
            division.Panel2.Controls.Add(totales);

            Controls.Add(division);
            Controls.Add(Estilo.Encabezado("Mesa " + mesaNumero));

            // si otro mesero le agrega algo a esta mesa desde otra compu, aca aparece solo
            timer.Interval = 5000;
            // solo cuando esta pantalla es la activa, para no moverle nada a alguien que esta cobrando o anulando
            timer.Tick += (s, e) => { if (orden != null && Form.ActiveForm == this) Recargar(false); };

            Load += (s, e) => Cargar();
            FormClosing += AlCerrar;
            FormClosed += (s, e) => timer.Stop();
        }

        void Cargar()
        {
            if (!Ui.Seguro(this, () =>
            {
                menu = MenuRepo.Platillos();
                orden = OrdenRepo.AbiertaDeMesa(mesaId);
            }))
            {
                Close();
                return;
            }

            categorias.Controls.Clear();
            foreach (var cat in new[] { "Todo" }.Concat(menu.Select(p => p.CategoriaNombre).Distinct()))
            {
                var b = Estilo.Boton(cat, Estilo.Gris);
                string c = cat;
                b.Click += (s, e) => { categoria = c; MostrarMenu(); };
                categorias.Controls.Add(b);
            }
            MostrarMenu();
            Pintar();
            timer.Start();
        }

        void MostrarMenu()
        {
            var filtro = txtBuscar.Text.Trim().ToLowerInvariant();
            lstMenu.BeginUpdate();
            lstMenu.Items.Clear();
            foreach (var p in menu.Where(x => categoria == "Todo" || x.CategoriaNombre == categoria)
                                  .Where(x => filtro.Length == 0 || x.Nombre.ToLowerInvariant().Contains(filtro)))
            {
                var item = new ListViewItem(p.Disponible ? p.Nombre : p.Nombre + " (agotado)") { Tag = p };
                item.SubItems.Add(Ticket.Q(p.Precio));
                if (!p.Disponible) item.ForeColor = Color.Silver;
                lstMenu.Items.Add(item);
            }
            lstMenu.EndUpdate();
        }

        void Recargar(bool mostrarErrores = true)
        {
            if (orden == null) return;
            var id = orden.Id;
            if (mostrarErrores)
                Ui.Seguro(this, () => orden = OrdenRepo.Cargar(id));
            else
            {
                try { orden = OrdenRepo.Cargar(id); }
                catch (Exception ex) { Log.Error("Recargar orden", ex); return; }
            }

            // si la cobraron desde otra caja se avisa y se sale
            if (orden == null || orden.Estado != "Abierta")
            {
                timer.Stop();
                MessageBox.Show(this, "Esta mesa ya fue cobrada o cerrada desde otra caja.", "Mesa " + mesaNumero,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                orden = null;
                Cobrada = true;
                Close();
                return;
            }
            Pintar();
        }

        void Pintar()
        {
            cargando = true;
            var seleccion = Seleccionada();

            grid.Rows.Clear();
            if (orden != null)
            {
                foreach (var l in orden.Activas)
                {
                    int i = grid.Rows.Add(l.Cantidad, l.Nombre, l.Nota, l.Estado == "Nuevo" ? "Sin enviar" : l.Estado, Ticket.Q(l.Subtotal));
                    grid.Rows[i].Tag = l;
                    if (l.Estado == "Nuevo") grid.Rows[i].DefaultCellStyle.ForeColor = Estilo.Naranja;
                    if (seleccion != null && l.Id == seleccion.Id) grid.CurrentCell = grid.Rows[i].Cells[0];
                }
                numPersonas.Value = Math.Min(Math.Max(orden.Personas, 1), 50);
                chkPropina.Checked = orden.ConPropina;
            }

            var o = orden ?? new Orden { ConPropina = chkPropina.Checked, PorcentajePropina = Configuracion.Propina };
            lblSubtotal.Text = Ticket.Q(o.Subtotal);
            lblPropina.Text = Ticket.Q(o.Propina);
            lblIva.Text = Ticket.Q(o.IvaIncluido(Configuracion.Iva));
            lblTotal.Text = Ticket.Q(o.Total);
            lblDescuento.Text = o.Descuento > 0 ? "-" + Ticket.Q(o.Descuento) : "";
            lblDescuentoTexto.Text = o.Descuento > 0 ? "Descuento" + (string.IsNullOrEmpty(o.DescuentoMotivo) ? "" : " (" + o.DescuentoMotivo + ")") : "";
            Text = "Mesa " + mesaNumero + (orden != null && orden.Mesero != null ? " · atiende " + orden.Mesero : "");
            btnCobrar.Enabled = orden != null && !orden.Vacia;
            btnEnviar.Enabled = orden != null && orden.SinEnviar > 0;
            btnEnviar.Text = orden != null && orden.SinEnviar > 0 ? "Enviar a cocina (" + orden.SinEnviar + ")" : "Enviar a cocina";
            cargando = false;
        }

        LineaOrden Seleccionada()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.Tag as LineaOrden;
        }

        void AgregarSeleccionado()
        {
            if (lstMenu.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Escoge un platillo del menú.", "Agregar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var platillo = (Platillo)lstMenu.SelectedItems[0].Tag;
            if (!platillo.Disponible)
            {
                MessageBox.Show(this, platillo.Nombre + " está agotado por hoy.", "Agregar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cantidad = (int)numCantidad.Value;
            var nota = txtNota.Text;
            if (Ui.Seguro(this, () =>
            {
                // la orden se crea hasta que se pide algo, asi no quedan mesas ocupadas sin nada
                var id = orden != null ? orden.Id : OrdenRepo.ObtenerOCrear(mesaId);
                if (orden == null && (!chkPropina.Checked || numPersonas.Value > 1))
                    OrdenRepo.CambiarDatos(id, (int)numPersonas.Value, chkPropina.Checked);
                OrdenRepo.Agregar(id, platillo.Id, cantidad, nota);
                orden = OrdenRepo.Cargar(id);
            }))
            {
                // se limpia para que la nota no se le pegue al siguiente platillo
                numCantidad.Value = 1;
                txtNota.Clear();
            }
            Pintar();
        }

        void Mas()
        {
            var l = Seleccionada();
            if (l == null) return;
            // si ya esta en cocina se pide aparte, asi cocina ve que es algo nuevo
            Ui.Seguro(this, () =>
            {
                if (l.YaEnCocina) OrdenRepo.Agregar(orden.Id, l.PlatilloId, 1, l.Nota);
                else OrdenRepo.CambiarCantidad(l.Id, 1);
            });
            Recargar();
        }

        void Menos()
        {
            var l = Seleccionada();
            if (l == null) return;
            if (!l.YaEnCocina)
            {
                Ui.Seguro(this, () => OrdenRepo.CambiarCantidad(l.Id, -1));
            }
            else
            {
                var motivo = FrmMotivo.Pedir(this, "1 " + l.Nombre);
                if (motivo == null) return;
                var autorizo = FrmLogin.Autorizar(this, "la anulación");
                if (autorizo == null) return;
                Ui.Seguro(this, () => OrdenRepo.Anular(l.Id, 1, motivo, autorizo.Value));
            }
            Recargar();
        }

        void Quitar()
        {
            var l = Seleccionada();
            if (l == null) return;
            if (!l.YaEnCocina)
            {
                Ui.Seguro(this, () => OrdenRepo.QuitarNueva(l.Id));
            }
            else
            {
                var motivo = FrmMotivo.Pedir(this, l.Cantidad + " " + l.Nombre);
                if (motivo == null) return;
                var autorizo = FrmLogin.Autorizar(this, "la anulación");
                if (autorizo == null) return;
                Ui.Seguro(this, () => OrdenRepo.Anular(l.Id, l.Cantidad, motivo, autorizo.Value));
            }
            Recargar();
        }

        void Enviar(bool avisar)
        {
            if (orden == null) return;
            int enviados = 0;
            if (Ui.Seguro(this, () => enviados = OrdenRepo.EnviarACocina(orden.Id)) && avisar && enviados > 0)
                MessageBox.Show(this, "Se mandaron " + enviados + " platillo(s) a cocina.", "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Recargar();
        }

        void GuardarDatos()
        {
            if (cargando) return;
            if (orden != null)
            {
                Ui.Seguro(this, () => OrdenRepo.CambiarDatos(orden.Id, (int)numPersonas.Value, chkPropina.Checked));
                Recargar();
            }
            else Pintar();
        }

        void Descontar()
        {
            if (orden == null || orden.Vacia) return;
            var texto = Microsoft.VisualBasic.Interaction.InputBox(
                "Descuento sobre el consumo de " + Ticket.Q(orden.Subtotal) + "\nEscribe un monto (25) o un porcentaje (10%). 0 para quitarlo.",
                "Descuento", orden.Descuento > 0 ? orden.Descuento.ToString("0.00") : "").Trim();
            if (texto.Length == 0) return;

            decimal valor;
            var esPorcentaje = texto.EndsWith("%");
            if (!decimal.TryParse(texto.TrimEnd('%').Replace(',', '.'), System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out valor) || valor < 0)
            {
                MessageBox.Show(this, "Escribe un número, por ejemplo 25 o 10%.", "Descuento", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var monto = esPorcentaje ? Math.Round(orden.Subtotal * valor / 100m, 2) : valor;

            string motivo = "";
            if (monto > 0)
            {
                motivo = Microsoft.VisualBasic.Interaction.InputBox("¿Por qué se hace el descuento?", "Descuento", "").Trim();
                if (motivo.Length < 3) return;
            }
            var autorizo = FrmLogin.Autorizar(this, "el descuento");
            if (autorizo == null) return;
            Ui.Seguro(this, () => OrdenRepo.Descontar(orden.Id, monto, motivo, autorizo.Value));
            Recargar();
        }

        void Cobrar()
        {
            if (orden == null) return;
            if (!Sesion.TurnoId.HasValue)
            {
                if (!Ui.Confirmar(this, "Para cobrar primero tienes que abrir tu caja. ¿Abrirla ahora?", "Caja")) return;
                using (var f = new FrmCaja()) f.ShowDialog(this);
                if (!Sesion.TurnoId.HasValue) return;
            }
            if (orden.SinEnviar > 0 && Ui.Confirmar(this,
                    "Hay " + orden.SinEnviar + " platillo(s) que no se han mandado a cocina.\n¿Mandarlos antes de cobrar?", "Cobrar"))
                Enviar(false);

            // se vuelve a leer justo antes de cobrar, para cobrar lo que de verdad tiene la mesa
            Recargar();
            if (orden == null || orden.Vacia) return;

            using (var f = new FrmCobro(orden.Total))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;

                int ticket = 0;
                List<Pago> pagos = null;
                var o = orden;
                if (!Ui.Seguro(this, () =>
                {
                    ticket = OrdenRepo.Cobrar(o.Id, o.Version, f.Pago);
                    pagos = OrdenRepo.Pagos(o.Id);
                }))
                {
                    Recargar();
                    return;
                }

                Cobrada = true;
                timer.Stop();
                try
                {
                    Ticket.Imprimir(Ticket.Texto(o, ticket, DateTime.Now, pagos), this);
                }
                catch (Exception ex)
                {
                    // ya quedo cobrada, si la impresora falla se puede reimprimir desde ventas del dia
                    Log.Error("Imprimir ticket", ex);
                    MessageBox.Show(this, "Se cobró, pero no se pudo imprimir el ticket. Puedes reimprimirlo desde Ventas del día.",
                        "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            orden = null;
            Close();
        }

        void AlCerrar(object sender, FormClosingEventArgs e)
        {
            if (orden == null || Cobrada) return;

            if (orden.SinEnviar > 0)
            {
                var r = MessageBox.Show(this,
                    "Hay " + orden.SinEnviar + " platillo(s) sin mandar a cocina.\n¿Mandarlos ahora?",
                    "Mesa " + mesaNumero, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) { e.Cancel = true; return; }
                if (r == DialogResult.Yes)
                {
                    try { OrdenRepo.EnviarACocina(orden.Id); }
                    catch (Exception ex) { Log.Error("Enviar al cerrar", ex); }
                }
            }

            try { OrdenRepo.CerrarSiVacia(orden.Id); }
            catch (Exception ex) { Log.Error("Cerrar orden vacia", ex); }
        }
    }
}
