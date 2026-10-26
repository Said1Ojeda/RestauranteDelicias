using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RestauranteDelicias.Datos;

namespace RestauranteDelicias.Pantallas
{
    // pedido nuevo para llevar o a domicilio, con los datos del cliente
    public class FrmPedido : Form
    {
        readonly FlowLayoutPanel tipos = new FlowLayoutPanel();
        readonly TextBox txtCliente = new TextBox();
        readonly TextBox txtTelefono = new TextBox();
        readonly TextBox txtDireccion = new TextBox();
        readonly Label lblDireccion = new Label();
        readonly Panel campoDireccion;
        string tipo = "Llevar";

        public int OrdenId { get; private set; }

        public FrmPedido()
        {
            Estilo.Formulario(this, "Pedido nuevo", 500, 470);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(22, 16, 22, 6), BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            tipos.AutoSize = true;
            tipos.Margin = new Padding(0, 0, 0, 10);
            foreach (var par in new[] { new[] { "Llevar", "Para llevar" }, new[] { "Domicilio", "A domicilio" } })
            {
                var valor = par[0];
                var b = Estilo.Chip(par[1], Estilo.Vino);
                b.Tag = valor;
                b.Radio = 10;
                b.Padding = new Padding(14, 8, 14, 8);
                b.Marcado = valor == tipo;
                b.Click += (s, e) => EscogerTipo(valor);
                tipos.Controls.Add(b);
            }
            t.Controls.Add(tipos);

            txtCliente.MaxLength = 100;
            txtTelefono.MaxLength = 20;
            txtDireccion.MaxLength = 200;
            foreach (var caja in new[] { txtCliente, txtTelefono, txtDireccion }) caja.Font = Estilo.Normal;

            t.Controls.Add(Etiqueta("Nombre del cliente"));
            t.Controls.Add(Campo(txtCliente));
            t.Controls.Add(Etiqueta("Teléfono"));
            t.Controls.Add(Campo(txtTelefono));
            lblDireccion.Text = "Dirección";
            lblDireccion.AutoSize = true;
            lblDireccion.ForeColor = Estilo.Suave;
            lblDireccion.Margin = new Padding(0, 10, 0, 2);
            t.Controls.Add(lblDireccion);
            campoDireccion = Campo(txtDireccion);
            t.Controls.Add(campoDireccion);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(14, 12, 14, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var ok = Estilo.Boton("Tomar el pedido", Estilo.Vino, Icono.Agregar);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) => Crear();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Pedido nuevo"));
            AcceptButton = ok;
            CancelButton = cancelar;
            EscogerTipo(tipo);
            Shown += (s, e) => txtCliente.Focus();
        }

        static Label Etiqueta(string texto)
        {
            return new Label { Text = texto, AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(0, 10, 0, 2) };
        }

        static Panel Campo(TextBox caja)
        {
            var campo = Estilo.Campo(caja);
            campo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            return campo;
        }

        void EscogerTipo(string valor)
        {
            tipo = valor;
            foreach (var b in tipos.Controls.OfType<Boton>()) b.Marcado = (string)b.Tag == valor;
            lblDireccion.Visible = campoDireccion.Visible = valor == "Domicilio";
        }

        void Crear()
        {
            int id = 0;
            if (!Ui.Seguro(this, () => id = OrdenRepo.CrearSinMesa(tipo, txtCliente.Text, txtTelefono.Text, DireccionEscrita()))) return;
            OrdenId = id;
            DialogResult = DialogResult.OK;
        }

        string DireccionEscrita()
        {
            return tipo == "Domicilio" ? txtDireccion.Text : null;
        }
    }
}
