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
        // una orden de mesa se busca por la mesa, un pedido para llevar por su numero de orden
        int? mesaId;
        int? mesaNumero;
        readonly int? ordenFija;
        Label lblEncabezado;
        readonly Label lblPagado = new Label();
        readonly Label lblPagadoTexto = new Label();
        Orden orden;
        List<Platillo> menu = new List<Platillo>();
        string categoria = "Todo";
        bool cargando;

        public bool Cobrada { get; private set; }

        readonly FlowLayoutPanel categorias = new FlowLayoutPanel();
        readonly TextBox txtBuscar = new TextBox();
        readonly FlowLayoutPanel panelMenu = new FlowLayoutPanel();
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
        readonly Boton btnCobrar;
        readonly Boton btnEnviar;
        readonly Boton btnEntregar;
        readonly Timer timer = new Timer();

        public FrmOrden(int mesaId, int mesaNumero) : this(mesaId, mesaNumero, null) { }

        public FrmOrden(int ordenId) : this(null, null, ordenId) { }

        FrmOrden(int? mesaId, int? mesaNumero, int? ordenFija)
        {
            this.mesaId = mesaId;
            this.mesaNumero = mesaNumero;
            this.ordenFija = ordenFija;
            Estilo.Formulario(this, TituloActual, 1100, 680);
            WindowState = FormWindowState.Maximized;

            var division = new SplitContainer { Dock = DockStyle.Fill, BackColor = Estilo.Borde, SplitterWidth = 1 };
            division.Panel1.BackColor = Estilo.Crema;
            division.Panel2.BackColor = Color.White;
            Load += (s, e) =>
            {
                division.Panel1MinSize = Estilo.Px(380);
                division.SplitterDistance = Math.Max(Estilo.Px(440), Width * 52 / 100);
            };

            // izquierda el menu en tarjetas, con buscador y las categorias como filtros
            var buscar = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(14, 12, 14, 0) };
            txtBuscar.Font = Estilo.Normal;
            var campoBuscar = Estilo.Campo(txtBuscar);
            campoBuscar.Dock = DockStyle.Fill;
            txtBuscar.TextChanged += (s, e) => MostrarMenu();
            txtBuscar.KeyDown += (s, e) =>
            {
                // enter agrega el primero que salga en la busqueda
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                var primero = Visibles().FirstOrDefault(p => p.Disponible);
                if (primero != null) Agregar(primero);
            };
            var lupa = new Label
            {
                Text = Estilo.HayIconos ? Icono.Buscar : "Buscar",
                Font = Estilo.HayIconos ? Estilo.Iconos(13f) : Estilo.Normal,
                ForeColor = Estilo.Suave,
                AutoSize = false,
                Width = Estilo.HayIconos ? 34 : 60,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter
            };
            buscar.Controls.Add(campoBuscar);
            buscar.Controls.Add(lupa);

            categorias.Dock = DockStyle.Top;
            categorias.AutoSize = true;
            categorias.Padding = new Padding(10, 8, 10, 4);

            panelMenu.Dock = DockStyle.Fill;
            panelMenu.AutoScroll = true;
            panelMenu.Padding = new Padding(8, 4, 8, 8);
            panelMenu.BackColor = Estilo.Crema;
            panelMenu.Resize += (s, e) => AcomodarMenu();

            var agregar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(12, 12, 12, 8), BackColor = Color.White };
            agregar.Paint += (s, e) => { using (var p = new Pen(Estilo.Borde)) e.Graphics.DrawLine(p, 0, 0, agregar.Width, 0); };
            numCantidad.Minimum = 1;
            numCantidad.Maximum = 50;
            numCantidad.Width = 60;
            numCantidad.Font = Estilo.Grande;
            numCantidad.BorderStyle = BorderStyle.FixedSingle;
            txtNota.MaxLength = 150;
            txtNota.Font = Estilo.Normal;
            var campoNota = Estilo.Campo(txtNota);
            campoNota.Width = 260;
            campoNota.Margin = new Padding(4, 0, 4, 0);
            agregar.Controls.Add(new Label { Text = "Cantidad", AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(0, 9, 4, 0) });
            agregar.Controls.Add(numCantidad);
            agregar.Controls.Add(new Label { Text = "Nota", AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(14, 9, 4, 0) });
            agregar.Controls.Add(campoNota);
            agregar.Controls.Add(new Label { Text = "Toca un platillo para agregarlo", AutoSize = true, ForeColor = Estilo.Suave, Font = Estilo.Chica, Margin = new Padding(10, 11, 0, 0) });

            division.Panel1.Controls.Add(panelMenu);
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
            grid.Columns["cant"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns["platillo"].FillWeight = 150;
            // el nombre con sus opciones puede ser largo, la fila crece en vez de cortarlo
            grid.Columns["platillo"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.Columns["platillo"].DefaultCellStyle.Font = Estilo.Negrita;
            grid.Columns["nota"].DefaultCellStyle.ForeColor = Estilo.Suave;
            grid.Columns["estado"].FillWeight = 70;
            grid.Columns["subtotal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["subtotal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) { Mas(); e.Handled = true; }
                else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) { Menos(); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete) { Quitar(); e.Handled = true; }
            };

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10, 10, 10, 6), BackColor = Color.White };
            var btnMas = Estilo.Chip("+1", Estilo.Gris);
            var btnMenos = Estilo.Chip("-1", Estilo.Gris);
            var btnQuitar = Estilo.Boton("Quitar", Estilo.Ocupada, Icono.Quitar);
            btnEnviar = Estilo.Boton("Enviar a cocina", Estilo.Naranja, Icono.Enviar);
            var btnDescuento = Estilo.Boton("Descuento", Estilo.Azul, Icono.Descuento);
            btnQuitar.Contorno = btnDescuento.Contorno = true;
            btnDescuento.Click += (s, e) => Descontar();
            btnMas.Click += (s, e) => Mas();
            btnMenos.Click += (s, e) => Menos();
            btnQuitar.Click += (s, e) => Quitar();
            var btnNota = Estilo.Chip("Nota", Estilo.Gris);
            btnNota.Click += (s, e) => CambiarNota();
            btnEnviar.Click += (s, e) => Enviar(true);
            // lo que cocina marco como listo, el mesero lo lleva y lo marca entregado
            btnEntregar = Estilo.Boton("Entregar listos", Estilo.Azul, Icono.Listo);
            btnEntregar.Visible = false;
            btnEntregar.Click += (s, e) => Entregar();
            var btnMover = Estilo.Boton("Mover o juntar", Estilo.Gris, Icono.Mesas);
            btnMover.Visible = mesaId.HasValue;
            btnMover.Click += (s, e) => MoverOJuntar();

            numPersonas.Minimum = 1;
            numPersonas.Maximum = 50;
            numPersonas.Width = 55;
            numPersonas.BorderStyle = BorderStyle.FixedSingle;
            numPersonas.ValueChanged += (s, e) => GuardarDatos();
            acciones.Controls.AddRange(new Control[]
            {
                btnMas, btnMenos, btnNota, btnQuitar, btnEnviar, btnEntregar, btnDescuento, btnMover,
                new Label { Text = "Personas", AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(16, 12, 2, 0) }, numPersonas
            });

            var totales = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 284,
                ColumnCount = 2,
                Padding = new Padding(20, 14, 20, 12),
                BackColor = Estilo.Crema
            };
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            chkPropina.Text = "Propina " + Configuracion.Propina.ToString("0.##") + "%";
            chkPropina.AutoSize = true;
            chkPropina.Checked = true;
            chkPropina.ForeColor = Estilo.Suave;
            chkPropina.CheckedChanged += (s, e) => GuardarDatos();

            lblDescuentoTexto.AutoSize = true;
            lblDescuento.ForeColor = lblDescuentoTexto.ForeColor = Estilo.Azul;
            foreach (var l in new[] { lblSubtotal, lblDescuento, lblPropina, lblIva, lblTotal })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Right;
            }
            lblIva.ForeColor = Estilo.Suave;
            lblTotal.Font = Estilo.Total;
            lblTotal.ForeColor = Estilo.Texto;

            totales.Controls.Add(new Label { Text = "Subtotal", AutoSize = true, ForeColor = Estilo.Suave }, 0, 0);
            totales.Controls.Add(lblSubtotal, 1, 0);
            totales.Controls.Add(lblDescuentoTexto, 0, 1);
            totales.Controls.Add(lblDescuento, 1, 1);
            totales.Controls.Add(chkPropina, 0, 2);
            totales.Controls.Add(lblPropina, 1, 2);
            totales.Controls.Add(new Label { Text = "Total", AutoSize = true, Font = Estilo.Grande, Anchor = AnchorStyles.Left }, 0, 3);
            totales.Controls.Add(lblTotal, 1, 3);
            totales.Controls.Add(new Label { Text = "IVA incluido", AutoSize = true, ForeColor = Estilo.Suave, Font = Estilo.Chica }, 0, 4);
            totales.Controls.Add(lblIva, 1, 4);
            lblPagadoTexto.AutoSize = lblPagado.AutoSize = true;
            lblPagadoTexto.ForeColor = lblPagado.ForeColor = Estilo.Libre;
            lblPagadoTexto.Font = lblPagado.Font = Estilo.Negrita;
            lblPagado.Anchor = AnchorStyles.Right;
            totales.Controls.Add(lblPagadoTexto, 0, 5);
            totales.Controls.Add(lblPagado, 1, 5);

            var botonesFin = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, FlowDirection = FlowDirection.RightToLeft, Margin = new Padding(0, 8, 0, 0) };
            btnCobrar = Estilo.Boton("Cobrar   F9", Estilo.Vino, Icono.Tarjeta);
            btnCobrar.Font = Estilo.Grande;
            btnCobrar.Padding = new Padding(22, 10, 22, 10);
            // el mesero toma la orden pero el cobro es de caja
            btnCobrar.Visible = Sesion.PuedeCobrar;
            var btnVolver = Estilo.Boton("Volver a mesas", Estilo.Gris, Icono.Volver);
            btnVolver.Contorno = true;
            btnVolver.Padding = new Padding(16, 10, 16, 10);
            btnCobrar.Click += (s, e) => Cobrar();
            btnVolver.Click += (s, e) => Close();
            botonesFin.Controls.AddRange(new Control[] { btnCobrar, btnVolver });
            totales.Controls.Add(botonesFin, 0, 6);
            totales.SetColumnSpan(botonesFin, 2);

            division.Panel2.Controls.Add(grid);
            division.Panel2.Controls.Add(acciones);
            division.Panel2.Controls.Add(totales);

            Controls.Add(division);
            var cabeza = Estilo.Encabezado(TituloActual);
            lblEncabezado = (Label)cabeza.Controls[0];
            Controls.Add(cabeza);

            // si otro mesero le agrega algo a esta mesa desde otra compu, aca aparece solo
            timer.Interval = 5000;
            // solo cuando esta pantalla es la activa, para no moverle nada a alguien que esta cobrando o anulando
            timer.Tick += (s, e) => { if (orden != null && Form.ActiveForm == this) Recargar(false); };

            // atajos para el que cobra rapido
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2) Enviar(true);
                else if (e.KeyCode == Keys.F4) Descontar();
                else if (e.KeyCode == Keys.F9 && btnCobrar.Visible && btnCobrar.Enabled) Cobrar();
                else if (e.KeyCode == Keys.F3 || (e.Control && e.KeyCode == Keys.F)) { txtBuscar.Focus(); txtBuscar.SelectAll(); }
                else if (e.KeyCode == Keys.Escape && txtBuscar.Text.Length > 0) txtBuscar.Clear();
                else if (e.KeyCode == Keys.Escape) Close();
                else return;
                e.Handled = true;
                e.SuppressKeyPress = true;
            };

            Load += (s, e) => Cargar();
            FormClosing += AlCerrar;
            FormClosed += (s, e) => timer.Stop();
        }

        void Cargar()
        {
            if (!Ui.Seguro(this, () =>
            {
                menu = MenuRepo.Platillos();
                orden = mesaId.HasValue ? OrdenRepo.AbiertaDeMesa(mesaId.Value) : OrdenRepo.Cargar(ordenFija.Value);
            }))
            {
                Close();
                return;
            }
            if (ordenFija.HasValue && (orden == null || orden.Estado != "Abierta"))
            {
                MessageBox.Show(this, "Este pedido ya fue cobrado o cerrado desde otra caja.", TituloActual, MessageBoxButtons.OK, MessageBoxIcon.Information);
                orden = null;
                Close();
                return;
            }

            Estilo.Vaciar(categorias);
            foreach (var cat in new[] { "Todo" }.Concat(menu.Select(p => p.CategoriaNombre).Distinct()))
            {
                var b = Estilo.Chip(cat, Estilo.Vino);
                b.Marcado = cat == categoria;
                string c = cat;
                b.Click += (s, e) =>
                {
                    categoria = c;
                    foreach (var otro in categorias.Controls.OfType<Boton>()) otro.Marcado = otro == b;
                    MostrarMenu();
                };
                categorias.Controls.Add(b);
            }
            MostrarMenu();
            Pintar();
            timer.Start();
            txtBuscar.Focus();
        }

        IEnumerable<Platillo> Visibles()
        {
            var filtro = txtBuscar.Text.Trim().ToLowerInvariant();
            return menu.Where(x => categoria == "Todo" || x.CategoriaNombre == categoria)
                       .Where(x => filtro.Length == 0 || x.Nombre.ToLowerInvariant().Contains(filtro));
        }

        void MostrarMenu()
        {
            panelMenu.SuspendLayout();
            Estilo.Vaciar(panelMenu);
            foreach (var p in Visibles())
            {
                var platillo = p;
                var t = new Tarjeta
                {
                    Titulo = p.Nombre,
                    Pie = Ticket.Q(p.Precio),
                    Etiqueta = p.Disponible ? "" : "Agotado",
                    Apagada = !p.Disponible,
                    Acento = Estilo.DeTexto(p.CategoriaColor, Estilo.Vino),
                    Size = new Size(Estilo.Px(168), Estilo.Px(104)),
                    Margin = new Padding(Estilo.Px(5))
                };
                t.Click += (s, e) => Agregar(platillo);
                panelMenu.Controls.Add(t);
            }
            AcomodarMenu();
            panelMenu.ResumeLayout();
        }

        void AcomodarMenu()
        {
            var disponible = panelMenu.ClientSize.Width - panelMenu.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            if (disponible > 0) Estilo.RepartirTarjetas(panelMenu, disponible, Estilo.Px(150), Estilo.Px(220));
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
                MessageBox.Show(this, "Esta cuenta ya fue cobrada o cerrada desde otra caja.", TituloActual,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                orden = null;
                Cobrada = true;
                Close();
                return;
            }
            Pintar();
        }

        string TituloActual
        {
            get
            {
                if (orden != null) return orden.Titulo;
                return mesaNumero.HasValue ? "Mesa " + mesaNumero : "Pedido";
            }
        }

        void Pintar()
        {
            cargando = true;
            // si la movieron de mesa desde otra compu, esta pantalla sigue a la orden
            if (orden != null && orden.MesaId.HasValue)
            {
                mesaId = orden.MesaId;
                mesaNumero = orden.MesaNumero;
            }
            var seleccion = Seleccionada();

            grid.Rows.Clear();
            if (orden != null)
            {
                foreach (var l in orden.Activas)
                {
                    var estado = (l.Estado == "Nuevo" ? "Sin enviar" : l.Estado) + (l.Pagada ? " · pagado" : "");
                    int i = grid.Rows.Add(l.Cantidad, l.NombreCompleto, l.Nota, estado, Ticket.Q(l.Subtotal));
                    grid.Rows[i].Tag = l;
                    grid.Rows[i].Cells["estado"].Style.ForeColor = ColorEstado(l.Estado);
                    grid.Rows[i].Cells["estado"].Style.SelectionForeColor = ColorEstado(l.Estado);
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
            lblDescuentoTexto.Text = o.Descuento > 0 ? "Descuento" + (string.IsNullOrEmpty(o.DescuentoMotivo) ? "" : " · " + o.DescuentoMotivo) : "";
            Text = TituloActual + (orden != null && orden.Mesero != null ? " · atiende " + orden.Mesero : "");
            if (lblEncabezado != null) lblEncabezado.Text = Text;
            var pagado = orden != null ? orden.Pagado : 0m;
            lblPagadoTexto.Text = pagado > 0 ? "Ya pagaron " + Ticket.Q(pagado) : "";
            lblPagado.Text = pagado > 0 ? "Saldo " + Ticket.Q(o.Saldo) : "";
            btnCobrar.Text = pagado > 0 ? "Cobrar saldo   F9" : "Cobrar   F9";
            btnCobrar.Enabled = orden != null && !orden.Vacia;
            btnEnviar.Enabled = orden != null && orden.SinEnviar > 0;
            var listos = orden == null ? 0 : orden.Activas.Count(l => l.Estado == "Listo");
            btnEntregar.Visible = listos > 0;
            btnEntregar.Text = "Entregar listos · " + listos;
            btnEnviar.Text = orden != null && orden.SinEnviar > 0 ? "Enviar a cocina · " + orden.SinEnviar + "   F2" : "Enviar a cocina   F2";
            cargando = false;
        }

        static Color ColorEstado(string estado)
        {
            switch (estado)
            {
                case "Nuevo": return Estilo.Naranja;
                case "Listo": return Estilo.Libre;
                case "Entregado": return Estilo.Suave;
                default: return Estilo.Azul;
            }
        }

        LineaOrden Seleccionada()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.Tag as LineaOrden;
        }

        void Agregar(Platillo platillo)
        {
            if (!platillo.Disponible)
            {
                MessageBox.Show(this, platillo.Nombre + " está agotado por hoy.", "Agregar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var cantidad = (int)numCantidad.Value;
            var nota = txtNota.Text;
            var opciones = new List<int>();

            // si el platillo tiene preguntas, como el estilo de los huevos, se escogen antes de agregarlo
            if (platillo.TieneOpciones)
            {
                List<GrupoOpcion> grupos = null;
                if (!Ui.Seguro(this, () => grupos = OpcionRepo.DePlatillo(platillo.Id))) return;
                if (grupos.Count > 0)
                    using (var f = new FrmOpciones(platillo, grupos, cantidad, nota))
                    {
                        if (f.ShowDialog(this) != DialogResult.OK) return;
                        opciones = f.Opciones;
                        cantidad = f.Cantidad;
                        nota = f.Nota;
                    }
            }

            var ordenEnPantalla = orden != null ? orden.Id : (int?)null;
            var personas = (int)numPersonas.Value;
            var conPropina = chkPropina.Checked;
            if (Ui.Seguro(this, () =>
            {
                // la orden se crea hasta que se pide algo, asi no quedan mesas ocupadas sin nada
                int id;
                if (mesaId.HasValue)
                    id = OrdenRepo.AgregarAMesa(mesaId.Value, ordenEnPantalla, platillo.Id, opciones, cantidad, nota, personas, conPropina);
                else
                {
                    id = ordenFija.Value;
                    OrdenRepo.AgregarAOrden(id, platillo.Id, opciones, cantidad, nota);
                }
                orden = OrdenRepo.Cargar(id);
            }))
            {
                // se limpia para que la nota no se le pegue al siguiente platillo
                numCantidad.Value = 1;
                txtNota.Clear();
            }
            else if (orden != null)
            {
                // si fallo porque la cobraron en otra caja, recargar avisa y cierra la mesa
                Recargar(false);
                return;
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
                if (l.YaEnCocina || l.Pagada) OrdenRepo.Repetir(l.Id);
                else OrdenRepo.CambiarCantidad(l.Id, 1);
            });
            Recargar();
        }

        // la nota de lo que ya se pidio, mientras no se haya mandado a cocina
        void CambiarNota()
        {
            var l = Seleccionada();
            if (l == null) return;
            if (l.YaEnCocina)
            {
                MessageBox.Show(this, "Eso ya se mandó a cocina. Para cambiarlo anúlalo y pídelo de nuevo.", "Nota",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var nota = FrmPregunta.Texto(this, "Nota", "Nota para " + l.NombreCompleto + ", por ejemplo sin cebolla o bien cocido.", l.Nota);
            if (nota == null) return;
            Ui.Seguro(this, () => OrdenRepo.CambiarNota(l.Id, nota));
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

        void Entregar()
        {
            if (orden == null) return;
            var id = orden.Id;
            Ui.Seguro(this, () => CocinaRepo.Entregar(id));
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
            var texto = FrmPregunta.Texto(this, "Descuento",
                "Descuento sobre el consumo de " + Ticket.Q(orden.Subtotal) + ". Escribe un monto como 25 o un porcentaje como 10%. Pon 0 para quitarlo.",
                orden.Descuento > 0 ? orden.Descuento.ToString("0.00") : "");
            if (string.IsNullOrWhiteSpace(texto)) return;
            texto = texto.Trim();

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
                motivo = (FrmPregunta.Texto(this, "Descuento", "¿Por qué se hace el descuento?", "") ?? "").Trim();
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

            // se cobra pago por pago hasta que el saldo llegue a cero o el cajero cancele
            // cada vuelta lee la cuenta de nuevo, asi se cobra lo que de verdad tiene la mesa
            while (true)
            {
                Recargar();
                if (orden == null || orden.Vacia || orden.Saldo <= 0) return;

                List<Pago> previos = null;
                if (!Ui.Seguro(this, () => previos = OrdenRepo.Pagos(orden.Id))) return;

                using (var f = new FrmCobro(orden, previos.Count))
                {
                    if (f.ShowDialog(this) != DialogResult.OK) return;

                    var o = orden;
                    ResultadoPago resultado = null;
                    List<Pago> pagos = null;
                    if (!Ui.Seguro(this, () =>
                    {
                        resultado = OrdenRepo.RegistrarPago(o.Id, f.SaldoVisto, f.Pago, f.Platillos);
                        pagos = OrdenRepo.Pagos(o.Id);
                    }))
                        continue;

                    if (resultado.Cerrada)
                    {
                        Cobrada = true;
                        timer.Stop();
                        Imprimir(Ticket.Texto(o, resultado.NumeroTicket.Value, DateTime.Now, pagos), "Se cobró");
                        orden = null;
                        Close();
                        return;
                    }

                    var pagados = f.Platillos.Select(p =>
                    {
                        var l = o.Lineas.First(x => x.Id == p.LineaId);
                        return new LineaOrden { Nombre = l.Nombre, Opciones = l.Opciones, Cantidad = p.Cantidad, PrecioUnitario = l.PrecioUnitario };
                    });
                    Imprimir(Ticket.Comprobante(o, f.Pago, pagados, resultado.Saldo), "Se registró el pago");
                }
            }
        }

        // si la impresora falla el pago ya quedo guardado, se puede reimprimir el ticket desde ventas del dia
        void Imprimir(string texto, string queSeHizo)
        {
            try
            {
                Ticket.Imprimir(texto, this);
            }
            catch (Exception ex)
            {
                Log.Error("Imprimir ticket", ex);
                MessageBox.Show(this, queSeHizo + ", pero no se pudo imprimir. Puedes reimprimirlo desde Ventas del día.",
                    "Ticket", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // a una mesa libre se mueve, a una ocupada se junta y esta mesa queda libre
        void MoverOJuntar()
        {
            if (orden == null || !mesaId.HasValue) return;
            using (var f = new FrmMover(orden))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                if (f.Juntada)
                {
                    timer.Stop();
                    orden = null;
                    Cobrada = true;
                    Close();
                    return;
                }
                mesaId = f.MesaDestinoId;
                mesaNumero = f.MesaDestinoNumero;
            }
            Recargar();
        }

        void AlCerrar(object sender, FormClosingEventArgs e)
        {
            if (orden == null || Cobrada) return;

            if (orden.SinEnviar > 0)
            {
                var r = MessageBox.Show(this,
                    "Hay " + orden.SinEnviar + " platillo(s) sin mandar a cocina.\n¿Mandarlos ahora?",
                    TituloActual, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
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
