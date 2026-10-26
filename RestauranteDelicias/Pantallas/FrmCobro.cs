using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // cobra toda la cuenta o una parte, en partes iguales, por platillos o un monto libre
    // cada vez se cobra un pago, la orden se cierra cuando el saldo queda en cero
    public class FrmCobro : Form
    {
        const string TODO = "Todo";
        const string PARTES = "Partes iguales";
        const string PLATILLOS = "Por platillos";
        const string OTRO = "Otro monto";

        readonly Orden orden;
        readonly int pagosHechos;
        readonly List<LineaOrden> sinPagar;

        readonly FlowLayoutPanel modos = new FlowLayoutPanel();
        readonly Panel zonaModo = new Panel();
        readonly Panel panelPartes = new Panel();
        readonly Panel panelOtro = new Panel();
        readonly DataGridView gridPlatillos = new DataGridView();
        readonly NumericUpDown numPartes = new NumericUpDown();
        readonly Label lblParte = new Label();
        readonly NumericUpDown numOtro = new NumericUpDown();
        readonly Label lblMonto = new Label();

        readonly FlowLayoutPanel formas = new FlowLayoutPanel();
        readonly NumericUpDown numRecibido = new NumericUpDown();
        readonly TextBox txtReferencia = new TextBox();
        readonly Label lblRecibido = new Label();
        readonly Label lblReferencia = new Label();
        readonly FlowLayoutPanel rapidos = new FlowLayoutPanel();
        readonly Label lblVuelto = new Label();
        readonly Boton btnAceptar;

        string modo = TODO;
        string forma = "Efectivo";
        decimal monto;
        decimal montoDeRapidos = -1;

        public Pago Pago { get; private set; }
        public List<LineaPagada> Platillos { get; private set; }
        public decimal SaldoVisto { get; private set; }

        public FrmCobro(Orden orden, int pagosHechos)
        {
            this.orden = orden;
            this.pagosHechos = pagosHechos;
            SaldoVisto = orden.Saldo;
            sinPagar = orden.Activas.Where(l => !l.Pagada).ToList();

            Estilo.Formulario(this, "Cobrar", 640, 740);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(22, 14, 22, 6), BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            t.Controls.Add(Resumen());
            t.Controls.Add(Titulo("¿Cómo pagan?"));
            ArmarModos();
            t.Controls.Add(modos);

            ArmarPartes();
            ArmarPlatillos();
            ArmarOtro();
            zonaModo.Height = 190;
            zonaModo.Dock = DockStyle.Top;
            zonaModo.Margin = new Padding(0, 4, 0, 4);
            zonaModo.Controls.AddRange(new Control[] { panelPartes, gridPlatillos, panelOtro });
            t.Controls.Add(zonaModo);

            lblMonto.AutoSize = true;
            lblMonto.Font = Estilo.Total;
            lblMonto.Margin = new Padding(0, 4, 0, 8);
            t.Controls.Add(Titulo("A cobrar ahora"));
            t.Controls.Add(lblMonto);

            ArmarFormas();
            t.Controls.Add(formas);
            t.Controls.Add(ArmarEfectivo());

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(16, 12, 16, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            btnAceptar = Estilo.Boton("Cobrar", Estilo.Vino, Icono.Imprimir);
            var btnCancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            btnAceptar.Click += (s, e) => Aceptar();
            btnCancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { btnAceptar, btnCancelar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Cobrar · " + orden.Titulo));
            AcceptButton = btnAceptar;
            CancelButton = btnCancelar;
            Load += (s, e) => Calcular();
        }

        static Label Titulo(string texto)
        {
            return new Label { Text = texto, AutoSize = true, ForeColor = Estilo.Suave, Font = Estilo.Negrita, Margin = new Padding(0, 10, 0, 2) };
        }

        Control Resumen()
        {
            var fila = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Top, BackColor = Color.White, Margin = new Padding(0, 0, 0, 4) };
            for (int i = 0; i < 3; i++) fila.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            fila.Controls.Add(Dato("Total de la cuenta", Ticket.Q(orden.Total), Estilo.Texto), 0, 0);
            fila.Controls.Add(Dato("Ya pagaron", Ticket.Q(orden.Pagado), Estilo.Suave), 1, 0);
            fila.Controls.Add(Dato("Saldo", Ticket.Q(orden.Saldo), Estilo.Vino), 2, 0);
            return fila;
        }

        static Control Dato(string titulo, string valor, Color color)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            p.Controls.Add(new Label { Text = titulo, AutoSize = true, ForeColor = Estilo.Suave, Font = Estilo.Chica, Margin = new Padding(0) });
            p.Controls.Add(new Label { Text = valor, AutoSize = true, ForeColor = color, Font = Estilo.Grande, Margin = new Padding(0, 2, 0, 0) });
            return p;
        }

        void ArmarModos()
        {
            modos.AutoSize = true;
            modos.Margin = new Padding(0, 2, 0, 0);
            foreach (var nombre in new[] { TODO, PARTES, PLATILLOS, OTRO })
            {
                var cual = nombre;
                var b = Estilo.Chip(nombre == TODO && pagosHechos > 0 ? "Todo el saldo" : nombre, Estilo.Vino);
                b.Tag = cual;
                b.Marcado = cual == modo;
                b.Enabled = cual != PLATILLOS || sinPagar.Count > 0;
                b.Click += (s, e) =>
                {
                    modo = cual;
                    foreach (var otro in modos.Controls.OfType<Boton>()) otro.Marcado = (string)otro.Tag == cual;
                    Calcular();
                };
                modos.Controls.Add(b);
            }
        }

        void ArmarPartes()
        {
            panelPartes.Dock = DockStyle.Fill;
            var fila = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            numPartes.Minimum = 2;
            numPartes.Maximum = 20;
            numPartes.Value = Math.Max(2, Math.Min(20, orden.Personas));
            numPartes.Width = 70;
            numPartes.Font = Estilo.Grande;
            numPartes.ValueChanged += (s, e) => Calcular();
            fila.Controls.Add(new Label { Text = "Dividir entre", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
            fila.Controls.Add(numPartes);
            fila.Controls.Add(new Label { Text = "personas", AutoSize = true, Margin = new Padding(8, 8, 0, 0) });
            lblParte.Dock = DockStyle.Top;
            lblParte.AutoSize = false;
            lblParte.Height = 40;
            lblParte.ForeColor = Estilo.Suave;
            lblParte.TextAlign = ContentAlignment.MiddleLeft;
            panelPartes.Controls.Add(lblParte);
            panelPartes.Controls.Add(fila);
        }

        // cada renglon sin pagar con cuantos de ese se pagan ahora, un toque marca todos o ninguno
        void ArmarPlatillos()
        {
            Estilo.Tabla(gridPlatillos);
            gridPlatillos.Dock = DockStyle.Fill;
            gridPlatillos.Columns.Add(new DataGridViewTextBoxColumn { Name = "pagar", HeaderText = "Pagan", FillWeight = 45 });
            gridPlatillos.Columns.Add(new DataGridViewTextBoxColumn { Name = "platillo", HeaderText = "Platillo", FillWeight = 160, ReadOnly = true });
            gridPlatillos.Columns.Add(new DataGridViewTextBoxColumn { Name = "precio", HeaderText = "Precio", FillWeight = 60, ReadOnly = true });
            gridPlatillos.Columns["pagar"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            gridPlatillos.Columns["pagar"].DefaultCellStyle.Font = Estilo.Grande;
            gridPlatillos.Columns["precio"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridPlatillos.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            foreach (var l in sinPagar)
            {
                var i = gridPlatillos.Rows.Add(0, l.Cantidad + "  " + l.Nombre, Ticket.Q(l.PrecioUnitario));
                gridPlatillos.Rows[i].Tag = l;
            }

            gridPlatillos.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex == gridPlatillos.Columns["pagar"].Index) return;
                var fila = gridPlatillos.Rows[e.RowIndex];
                var l = (LineaOrden)fila.Tag;
                fila.Cells["pagar"].Value = CuantosPagan(fila) == 0 ? l.Cantidad : 0;
                Calcular();
            };
            gridPlatillos.CellEndEdit += (s, e) =>
            {
                var fila = gridPlatillos.Rows[e.RowIndex];
                fila.Cells["pagar"].Value = CuantosPagan(fila);
                Calcular();
            };
        }

        static int CuantosPagan(DataGridViewRow fila)
        {
            var l = (LineaOrden)fila.Tag;
            int n;
            if (!int.TryParse(Convert.ToString(fila.Cells["pagar"].Value), out n)) n = 0;
            return Math.Max(0, Math.Min(l.Cantidad, n));
        }

        void ArmarOtro()
        {
            panelOtro.Dock = DockStyle.Fill;
            var fila = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
            numOtro.DecimalPlaces = 2;
            numOtro.Minimum = 0.01m;
            numOtro.Maximum = Math.Max(0.01m, orden.Saldo);
            numOtro.Value = Math.Max(0.01m, Math.Min(orden.Saldo, Math.Round(orden.Saldo / 2, 2)));
            numOtro.ThousandsSeparator = true;
            numOtro.Width = 160;
            numOtro.Font = Estilo.Grande;
            numOtro.ValueChanged += (s, e) => Calcular();
            fila.Controls.Add(new Label { Text = "Monto Q", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
            fila.Controls.Add(numOtro);
            panelOtro.Controls.Add(new Label
            {
                Text = "Sirve para pagar con dos formas, por ejemplo una parte en efectivo y el resto con tarjeta.",
                Dock = DockStyle.Top,
                Height = 44,
                ForeColor = Estilo.Suave,
                Font = Estilo.Chica
            });
            panelOtro.Controls.Add(fila);
        }

        void ArmarFormas()
        {
            formas.AutoSize = true;
            formas.Margin = new Padding(0, 4, 0, 6);
            foreach (var par in new[] { new[] { "Efectivo", "" }, new[] { "Tarjeta", Icono.Tarjeta }, new[] { "Transferencia", Icono.Banco } })
            {
                var nombre = par[0];
                var b = Estilo.Chip(nombre, Estilo.Vino);
                if (par[1].Length > 0) b.Icono = par[1];
                b.Radio = 10;
                b.Padding = new Padding(12, 8, 12, 8);
                b.Marcado = nombre == forma;
                b.Click += (s, e) =>
                {
                    forma = nombre;
                    foreach (var otro in formas.Controls.OfType<Boton>()) otro.Marcado = otro == b;
                    Calcular();
                };
                formas.Controls.Add(b);
            }
        }

        Control ArmarEfectivo()
        {
            var t = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Top, BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));

            numRecibido.DecimalPlaces = 2;
            numRecibido.Font = Estilo.Grande;
            numRecibido.Maximum = 100000;
            numRecibido.ThousandsSeparator = true;
            numRecibido.Width = 180;
            numRecibido.ValueChanged += (s, e) => Calcular();
            numRecibido.Enter += (s, e) => numRecibido.Select(0, numRecibido.Text.Length);

            txtReferencia.MaxLength = 50;
            txtReferencia.Width = 220;

            rapidos.AutoSize = true;
            rapidos.Margin = new Padding(0);

            lblVuelto.AutoSize = true;
            lblVuelto.Font = Estilo.Grande;
            lblRecibido.Text = "Recibido";
            foreach (var l in new[] { lblRecibido, lblReferencia })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Left;
                l.ForeColor = Estilo.Suave;
            }

            t.Controls.Add(lblRecibido, 0, 0);
            t.Controls.Add(numRecibido, 1, 0);
            t.Controls.Add(rapidos, 1, 1);
            t.Controls.Add(lblReferencia, 0, 2);
            t.Controls.Add(txtReferencia, 1, 2);
            t.Controls.Add(new Label { Text = "Vuelto", AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left }, 0, 3);
            t.Controls.Add(lblVuelto, 1, 3);
            return t;
        }

        bool EsEfectivo
        {
            get { return forma == "Efectivo"; }
        }

        decimal SubtotalElegido
        {
            get
            {
                return gridPlatillos.Rows.Cast<DataGridViewRow>()
                    .Sum(f => CuantosPagan(f) * ((LineaOrden)f.Tag).PrecioUnitario);
            }
        }

        bool TodoElegido
        {
            get { return gridPlatillos.Rows.Cast<DataGridViewRow>().All(f => CuantosPagan(f) == ((LineaOrden)f.Tag).Cantidad); }
        }

        int ParteActual
        {
            get { return Math.Min(pagosHechos + 1, (int)numPartes.Value); }
        }

        // el ultimo pago siempre es el saldo exacto, asi los centavos del redondeo no quedan colgando
        decimal CalcularMonto()
        {
            var saldo = orden.Saldo;
            switch (modo)
            {
                case PARTES:
                    var cuota = Math.Round(orden.Total / numPartes.Value, 2);
                    return pagosHechos + 1 >= numPartes.Value || cuota >= saldo ? saldo : cuota;
                case PLATILLOS:
                    var elegido = SubtotalElegido;
                    if (elegido <= 0) return 0m;
                    return TodoElegido ? saldo : Math.Min(saldo, orden.ParteDe(elegido));
                case OTRO:
                    return Math.Min(saldo, numOtro.Value);
                default:
                    return saldo;
            }
        }

        string NombreDeLaParte()
        {
            switch (modo)
            {
                case PARTES: return "Parte " + ParteActual + " de " + numPartes.Value;
                case PLATILLOS: return "Por platillos";
                case OTRO: return "Abono";
                default: return pagosHechos > 0 ? "Saldo" : null;
            }
        }

        void Calcular()
        {
            panelPartes.Visible = modo == PARTES;
            gridPlatillos.Visible = modo == PLATILLOS;
            panelOtro.Visible = modo == OTRO;
            zonaModo.Visible = modo != TODO;

            monto = CalcularMonto();
            lblMonto.Text = Ticket.Q(monto);
            lblMonto.ForeColor = monto > 0 ? Estilo.Texto : Estilo.Suave;
            lblParte.Text = modo == PARTES ? NombreDeLaParte() + ", cada parte es de " + Ticket.Q(Math.Round(orden.Total / numPartes.Value, 2)) : "";

            if (montoDeRapidos != monto)
            {
                montoDeRapidos = monto;
                ArmarRapidos();
                numRecibido.Value = Math.Min(numRecibido.Maximum, Math.Ceiling(monto));
            }

            lblRecibido.Visible = numRecibido.Visible = rapidos.Visible = EsEfectivo;
            lblReferencia.Visible = txtReferencia.Visible = !EsEfectivo;
            lblReferencia.Text = forma == "Tarjeta" ? "Últimos 4 dígitos" : "No. de boleta";
            btnAceptar.Text = monto > 0 ? "Cobrar " + Ticket.Q(monto) : "Cobrar";

            if (!EsEfectivo)
            {
                lblVuelto.Text = "-";
                lblVuelto.ForeColor = Estilo.Suave;
                btnAceptar.Enabled = monto > 0;
                return;
            }

            var vuelto = numRecibido.Value - monto;
            lblVuelto.Text = vuelto >= 0 ? Ticket.Q(vuelto) : "Faltan " + Ticket.Q(-vuelto);
            lblVuelto.ForeColor = vuelto >= 0 ? Estilo.Libre : Estilo.Ocupada;
            btnAceptar.Enabled = monto > 0 && vuelto >= 0;
        }

        // los billetes mas comunes, el cajero no tiene que teclear
        void ArmarRapidos()
        {
            Estilo.Vaciar(rapidos);
            foreach (var billete in new decimal[] { 0, 50, 100, 200 })
            {
                if (billete != 0 && billete < monto) continue;
                var valor = billete == 0 ? monto : billete;
                var b = Estilo.Chip(billete == 0 ? "Exacto" : "Q" + billete, Estilo.Libre);
                b.Click += (s, e) => numRecibido.Value = Math.Min(numRecibido.Maximum, valor);
                rapidos.Controls.Add(b);
            }
        }

        void Aceptar()
        {
            if (monto <= 0) return;
            Pago = new Pago
            {
                Forma = forma,
                Monto = monto,
                Parte = NombreDeLaParte(),
                Recibido = EsEfectivo ? numRecibido.Value : (decimal?)null,
                Referencia = EsEfectivo || txtReferencia.Text.Trim().Length == 0 ? null : txtReferencia.Text.Trim()
            };
            Platillos = modo != PLATILLOS ? new List<LineaPagada>() : gridPlatillos.Rows.Cast<DataGridViewRow>()
                .Where(f => CuantosPagan(f) > 0)
                .Select(f => new LineaPagada { LineaId = ((LineaOrden)f.Tag).Id, Cantidad = CuantosPagan(f) })
                .ToList();
            DialogResult = DialogResult.OK;
        }
    }
}
