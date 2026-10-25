using System.Drawing;
using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // pregunta corta con una caja de texto, en lugar del inputbox viejo de windows
    public class FrmPregunta : Form
    {
        readonly TextBox txt = new TextBox();

        FrmPregunta(string titulo, string pregunta, string valor)
        {
            Estilo.Formulario(this, titulo, 460, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 18, 20, 8), ColumnCount = 1, BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            t.Controls.Add(new Label { Text = pregunta, AutoSize = true, MaximumSize = new Size(410, 0), Font = Estilo.Negrita, Margin = new Padding(0, 0, 0, 10) });

            txt.Text = valor ?? "";
            txt.MaxLength = 150;
            txt.Font = Estilo.Grande;
            var campo = Estilo.Campo(txt);
            campo.Dock = DockStyle.Top;
            campo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            t.Controls.Add(campo);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(14, 10, 14, 10), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            var ok = Estilo.Boton("Aceptar", Estilo.Vino);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            cancelar.Contorno = true;
            ok.Click += (s, e) => DialogResult = DialogResult.OK;
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(t);
            Controls.Add(botones);
            AcceptButton = ok;
            CancelButton = cancelar;
            Shown += (s, e) => { txt.Focus(); txt.SelectAll(); };
        }

        public static string Texto(IWin32Window dueno, string titulo, string pregunta, string valor)
        {
            using (var f = new FrmPregunta(titulo, pregunta, valor))
                return f.ShowDialog(dueno) == DialogResult.OK ? f.txt.Text : null;
        }
    }
}
