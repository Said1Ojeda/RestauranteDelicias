using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using RestauranteDelicias.Datos;

namespace RestauranteDelicias.Pantallas
{
    // los datos que salen en el ticket y los porcentajes
    public class FrmConfiguracion : Form
    {
        readonly Dictionary<string, TextBox> campos = new Dictionary<string, TextBox>();

        static readonly string[,] CLAVES =
        {
            { "Restaurante.Nombre", "Nombre del restaurante" },
            { "Restaurante.Lema", "Lema" },
            { "Restaurante.Direccion", "Dirección" },
            { "Restaurante.Telefono", "Teléfono" },
            { "Restaurante.Nit", "NIT" },
            { "Propina.Porcentaje", "Propina sugerida (%)" },
            { "Iva.Porcentaje", "IVA (%)" },
            { "Ticket.Pie", "Pie del ticket" }
        };

        public FrmConfiguracion()
        {
            Estilo.Formulario(this, "Datos del restaurante", 560, 470);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < CLAVES.GetLength(0); i++)
            {
                var txt = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 6, 3, 6), MaxLength = 300, Text = Configuracion.Texto(CLAVES[i, 0]) };
                campos[CLAVES[i, 0]] = txt;
                t.Controls.Add(new Label { Text = CLAVES[i, 1], AutoSize = true, Margin = new Padding(3, 10, 3, 3) }, 0, i);
                t.Controls.Add(txt, 1, i);
            }

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(10), FlowDirection = FlowDirection.RightToLeft };
            var ok = Estilo.Boton("Guardar", Estilo.Vino);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) => Guardar();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Datos del restaurante"));
        }

        void Guardar()
        {
            if (campos["Restaurante.Nombre"].Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "El nombre no puede quedar vacío.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (var clave in new[] { "Propina.Porcentaje", "Iva.Porcentaje" })
            {
                decimal n;
                var texto = campos[clave].Text.Trim().Replace(',', '.');
                if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out n) || n < 0 || n > 100)
                {
                    MessageBox.Show(this, "Los porcentajes tienen que ser un número entre 0 y 100.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                campos[clave].Text = n.ToString(CultureInfo.InvariantCulture);
            }

            var valores = new Dictionary<string, string>();
            foreach (var par in campos) valores[par.Key] = par.Value.Text.Trim();
            if (Ui.Seguro(this, () => Configuracion.Guardar(valores)))
                DialogResult = DialogResult.OK;
        }
    }
}
