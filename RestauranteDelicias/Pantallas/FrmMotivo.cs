using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // pide el motivo al anular algo que ya se mando a cocina
    public class FrmMotivo : Form
    {
        readonly ComboBox cbo = new ComboBox();

        public string Motivo
        {
            get { return cbo.Text.Trim(); }
        }

        FrmMotivo(string queSeAnula)
        {
            Estilo.Formulario(this, "Anular", 440, 190);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
            t.Controls.Add(new Label { Text = "¿Por qué se anula " + queSeAnula + "?", AutoSize = true, Font = Estilo.Negrita }, 0, 0);

            cbo.DropDownStyle = ComboBoxStyle.DropDown;
            cbo.Dock = DockStyle.Top;
            cbo.MaxLength = 150;
            cbo.Items.AddRange(new object[]
            {
                "El cliente cambió de opinión",
                "Error al tomar la orden",
                "Se tardó demasiado",
                "Salió mal de cocina",
                "Se acabó el producto"
            });
            t.Controls.Add(cbo, 0, 1);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            var ok = Estilo.Boton("Anular", Estilo.Ocupada);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) =>
            {
                if (Motivo.Length < 3)
                {
                    MessageBox.Show(this, "Escribe o escoge un motivo.", "Anular", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
            };
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });
            t.Controls.Add(botones, 0, 2);

            Controls.Add(t);
            AcceptButton = ok;
            CancelButton = cancelar;
        }

        public static string Pedir(IWin32Window dueno, string queSeAnula)
        {
            using (var f = new FrmMotivo(queSeAnula))
                return f.ShowDialog(dueno) == DialogResult.OK ? f.Motivo : null;
        }
    }
}
