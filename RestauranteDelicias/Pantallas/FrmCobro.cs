using System;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmCobro : Form
    {
        readonly decimal total;

        public string FormaPago { get; private set; }
        public decimal Recibido { get; private set; }

        readonly ComboBox cboPago = new ComboBox();
        readonly NumericUpDown numRecibido = new NumericUpDown();
        readonly Label lblVuelto = new Label();
        readonly Button btnAceptar;

        public FrmCobro(decimal total)
        {
            this.total = total;
            Estilo.Formulario(this, "Cobrar", 380, 280);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            var lblTotal = new Label { Text = Ticket.Q(total), AutoSize = true, Font = Estilo.Total, ForeColor = Estilo.Vino };

            cboPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPago.Items.AddRange(new object[] { "Efectivo", "Tarjeta" });
            cboPago.SelectedIndex = 0;
            cboPago.SelectedIndexChanged += (s, e) => Calcular();

            numRecibido.DecimalPlaces = 2;
            numRecibido.Maximum = 100000;
            numRecibido.ThousandsSeparator = true;
            numRecibido.Value = Math.Ceiling(total);
            numRecibido.ValueChanged += (s, e) => Calcular();
            numRecibido.Enter += (s, e) => numRecibido.Select(0, numRecibido.Text.Length);

            lblVuelto.AutoSize = true;
            lblVuelto.Font = Estilo.Negrita;

            t.Controls.Add(new Label { Text = "Total a cobrar", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            t.Controls.Add(lblTotal, 1, 0);
            t.Controls.Add(new Label { Text = "Forma de pago", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            t.Controls.Add(cboPago, 1, 1);
            t.Controls.Add(new Label { Text = "Recibido", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            t.Controls.Add(numRecibido, 1, 2);
            t.Controls.Add(new Label { Text = "Vuelto", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            t.Controls.Add(lblVuelto, 1, 3);

            var botones = new FlowLayoutPanel { AutoSize = true, Anchor = AnchorStyles.Right, FlowDirection = FlowDirection.RightToLeft };
            btnAceptar = Estilo.Boton("Cobrar e imprimir", Estilo.Vino);
            var btnCancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            btnAceptar.Click += (s, e) => Aceptar();
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; };
            botones.Controls.AddRange(new Control[] { btnAceptar, btnCancelar });
            t.Controls.Add(botones, 0, 4);
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
            numRecibido.Enabled = EsEfectivo;
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
            FormaPago = (string)cboPago.SelectedItem;
            Recibido = EsEfectivo ? numRecibido.Value : total;
            DialogResult = DialogResult.OK;
        }
    }
}
