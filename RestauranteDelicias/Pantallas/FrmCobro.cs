using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmCobro : Form
    {
        readonly decimal total;

        public Pago Pago { get; private set; }

        readonly FlowLayoutPanel formas = new FlowLayoutPanel();
        string forma = "Efectivo";
        readonly NumericUpDown numRecibido = new NumericUpDown();
        readonly TextBox txtReferencia = new TextBox();
        readonly Label lblRecibido = new Label();
        readonly Label lblReferencia = new Label();
        readonly FlowLayoutPanel rapidos = new FlowLayoutPanel();
        readonly Label lblVuelto = new Label();
        readonly Boton btnAceptar;

        public FrmCobro(decimal total)
        {
            this.total = total;
            Estilo.Formulario(this, "Cobrar", 520, 470);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            BackColor = Color.White;
            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(22, 16, 22, 8), BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));

            var lblTotal = new Label { Text = Ticket.Q(total), AutoSize = true, Font = Estilo.Total, ForeColor = Estilo.Texto };

            // la forma de pago se escoge con un toque, sin abrir listas
            formas.AutoSize = true;
            formas.Margin = new Padding(0, 4, 0, 8);
            foreach (var par in new[] { new[] { "Efectivo", "Q" }, new[] { "Tarjeta", Icono.Tarjeta }, new[] { "Transferencia", Icono.Banco } })
            {
                var nombre = par[0];
                var b = Estilo.Chip(nombre, Estilo.Vino);
                if (par[1] != "Q") b.Icono = par[1];
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

            numRecibido.DecimalPlaces = 2;
            numRecibido.Font = Estilo.Grande;
            numRecibido.BorderStyle = BorderStyle.FixedSingle;
            numRecibido.Maximum = 100000;
            numRecibido.ThousandsSeparator = true;
            numRecibido.Value = Math.Ceiling(total);
            numRecibido.ValueChanged += (s, e) => Calcular();
            numRecibido.Enter += (s, e) => numRecibido.Select(0, numRecibido.Text.Length);

            txtReferencia.MaxLength = 50;
            txtReferencia.Dock = DockStyle.Fill;

            // los billetes mas comunes, el cajero no tiene que teclear
            rapidos.AutoSize = true;
            rapidos.Margin = new Padding(0);
            foreach (var monto in new decimal[] { 0, 50, 100, 200 })
            {
                var m = monto == 0 ? total : monto;
                var b = Estilo.Chip(monto == 0 ? "Exacto" : "Q" + monto, Estilo.Libre);
                b.Click += (s, e) => numRecibido.Value = Math.Min(numRecibido.Maximum, m);
                if (monto == 0 || monto >= total) rapidos.Controls.Add(b);
            }

            lblVuelto.AutoSize = true;
            lblVuelto.Font = Estilo.Grande;
            lblRecibido.Text = "Recibido";
            lblReferencia.Text = "Referencia";
            foreach (var l in new[] { lblRecibido, lblReferencia })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Left;
            }

            t.Controls.Add(new Label { Text = "Total a cobrar", AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left }, 0, 0);
            t.Controls.Add(lblTotal, 1, 0);
            t.Controls.Add(formas, 0, 1);
            t.SetColumnSpan(formas, 2);
            t.Controls.Add(lblRecibido, 0, 2);
            t.Controls.Add(numRecibido, 1, 2);
            t.Controls.Add(rapidos, 1, 3);
            t.Controls.Add(lblReferencia, 0, 4);
            t.Controls.Add(txtReferencia, 1, 4);
            t.Controls.Add(new Label { Text = "Vuelto", AutoSize = true, ForeColor = Estilo.Suave, Anchor = AnchorStyles.Left }, 0, 5);
            t.Controls.Add(lblVuelto, 1, 5);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 66, Padding = new Padding(16, 12, 16, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            btnAceptar = Estilo.Boton("Cobrar e imprimir", Estilo.Vino, Icono.Imprimir);
            var btnCancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            btnCancelar.Contorno = true;
            btnAceptar.Click += (s, e) => Aceptar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            botones.Controls.AddRange(new Control[] { btnAceptar, btnCancelar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Cobrar"));
            AcceptButton = btnAceptar;
            CancelButton = btnCancelar;
            Calcular();
        }

        bool EsEfectivo
        {
            get { return forma == "Efectivo"; }
        }

        void Calcular()
        {
            lblRecibido.Visible = numRecibido.Visible = rapidos.Visible = EsEfectivo;
            lblReferencia.Visible = txtReferencia.Visible = !EsEfectivo;
            lblReferencia.Text = forma == "Tarjeta" ? "Últimos 4 dígitos" : "No. de boleta";

            if (!EsEfectivo)
            {
                lblVuelto.Text = "-";
                lblVuelto.ForeColor = Estilo.Suave;
                btnAceptar.Enabled = true;
                return;
            }

            decimal vuelto = numRecibido.Value - total;
            lblVuelto.Text = vuelto >= 0 ? Ticket.Q(vuelto) : "Faltan " + Ticket.Q(-vuelto);
            lblVuelto.ForeColor = vuelto >= 0 ? Estilo.Libre : Estilo.Ocupada;
            btnAceptar.Enabled = vuelto >= 0;
        }

        void Aceptar()
        {
            Pago = new Pago
            {
                Forma = forma,
                Monto = total,
                Recibido = EsEfectivo ? numRecibido.Value : (decimal?)null,
                Referencia = EsEfectivo || txtReferencia.Text.Trim().Length == 0 ? null : txtReferencia.Text.Trim()
            };
            DialogResult = DialogResult.OK;
        }
    }
}
