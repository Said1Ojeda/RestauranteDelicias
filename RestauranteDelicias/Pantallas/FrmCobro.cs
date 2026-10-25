using System;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmCobro : Form
    {
        readonly decimal total;

        public Pago Pago { get; private set; }

        readonly ComboBox cboPago = new ComboBox();
        readonly NumericUpDown numRecibido = new NumericUpDown();
        readonly TextBox txtReferencia = new TextBox();
        readonly Label lblRecibido = new Label();
        readonly Label lblReferencia = new Label();
        readonly FlowLayoutPanel rapidos = new FlowLayoutPanel();
        readonly Label lblVuelto = new Label();
        readonly Button btnAceptar;

        public FrmCobro(decimal total)
        {
            this.total = total;
            Estilo.Formulario(this, "Cobrar", 460, 380);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            var lblTotal = new Label { Text = Ticket.Q(total), AutoSize = true, Font = Estilo.Total, ForeColor = Estilo.Vino };

            cboPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPago.Items.AddRange(new object[] { "Efectivo", "Tarjeta", "Transferencia" });
            cboPago.SelectedIndex = 0;
            cboPago.SelectedIndexChanged += (s, e) => Calcular();

            numRecibido.DecimalPlaces = 2;
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
                var b = new Button { Text = monto == 0 ? "Exacto" : "Q" + monto, AutoSize = true, Margin = new Padding(2) };
                b.Click += (s, e) => numRecibido.Value = Math.Min(numRecibido.Maximum, m);
                if (monto == 0 || monto >= total) rapidos.Controls.Add(b);
            }

            lblVuelto.AutoSize = true;
            lblVuelto.Font = Estilo.Negrita;
            lblRecibido.Text = "Recibido";
            lblReferencia.Text = "Referencia";
            foreach (var l in new[] { lblRecibido, lblReferencia })
            {
                l.AutoSize = true;
                l.Anchor = AnchorStyles.Left;
            }

            t.Controls.Add(new Label { Text = "Total a cobrar", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            t.Controls.Add(lblTotal, 1, 0);
            t.Controls.Add(new Label { Text = "Forma de pago", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            t.Controls.Add(cboPago, 1, 1);
            t.Controls.Add(lblRecibido, 0, 2);
            t.Controls.Add(numRecibido, 1, 2);
            t.Controls.Add(rapidos, 1, 3);
            t.Controls.Add(lblReferencia, 0, 4);
            t.Controls.Add(txtReferencia, 1, 4);
            t.Controls.Add(new Label { Text = "Vuelto", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
            t.Controls.Add(lblVuelto, 1, 5);

            var botones = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, FlowDirection = FlowDirection.RightToLeft };
            btnAceptar = Estilo.Boton("Cobrar e imprimir", Estilo.Vino);
            var btnCancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            btnAceptar.Click += (s, e) => Aceptar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            botones.Controls.AddRange(new Control[] { btnAceptar, btnCancelar });
            t.Controls.Add(botones, 0, 6);
            t.SetColumnSpan(botones, 2);

            Controls.Add(t);
            AcceptButton = btnAceptar;
            CancelButton = btnCancelar;
            Calcular();
        }

        bool EsEfectivo
        {
            get { return (string)cboPago.SelectedItem == "Efectivo"; }
        }

        void Calcular()
        {
            lblRecibido.Visible = numRecibido.Visible = rapidos.Visible = EsEfectivo;
            lblReferencia.Visible = txtReferencia.Visible = !EsEfectivo;
            lblReferencia.Text = (string)cboPago.SelectedItem == "Tarjeta" ? "Últimos 4 dígitos" : "No. de boleta";

            if (!EsEfectivo)
            {
                lblVuelto.Text = "-";
                lblVuelto.ForeColor = Color.Black;
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
            var forma = (string)cboPago.SelectedItem;
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
