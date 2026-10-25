using System;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Datos;

namespace RestauranteDelicias.Pantallas
{
    // respaldo de la base con un clic, y el aviso si hace mucho que no se hace
    public class FrmRespaldo : Form
    {
        readonly TextBox txtCarpeta = new TextBox();
        readonly Label lblUltimo = new Label();
        readonly Boton btnRespaldar;

        public FrmRespaldo()
        {
            Estilo.Formulario(this, "Respaldo de la base", 560, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(22, 16, 22, 8), BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            lblUltimo.AutoSize = true;
            lblUltimo.Font = Estilo.Grande;
            lblUltimo.Margin = new Padding(0, 0, 0, 12);
            t.Controls.Add(lblUltimo);

            t.Controls.Add(new Label { Text = "Carpeta en la compu del servidor", AutoSize = true, ForeColor = Estilo.Suave });
            txtCarpeta.Font = Estilo.Normal;
            txtCarpeta.MaxLength = 250;
            var campo = Estilo.Campo(txtCarpeta);
            campo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            t.Controls.Add(campo);

            t.Controls.Add(new Label
            {
                Text = "El respaldo lo guarda SQL Server en su propia compu. Copia esos archivos de vez en cuando a una USB o a la nube, " +
                       "así si se arruina el disco no se pierde nada.",
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                ForeColor = Estilo.Suave,
                Font = Estilo.Chica,
                Margin = new Padding(0, 12, 0, 0)
            });

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(14, 12, 14, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Estilo.Crema };
            btnRespaldar = Estilo.Boton("Respaldar ahora", Estilo.Libre, Icono.Respaldo);
            var cerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            btnRespaldar.Click += (s, e) => Respaldar();
            cerrar.Click += (s, e) => Close();
            botones.Controls.AddRange(new Control[] { btnRespaldar, cerrar });

            Controls.Add(t);
            Controls.Add(botones);
            Controls.Add(Estilo.Encabezado("Respaldo de la base"));
            CancelButton = cerrar;

            Load += (s, e) =>
            {
                txtCarpeta.Text = RespaldoRepo.CarpetaSugerida();
                Mostrar();
            };
        }

        void Mostrar()
        {
            lblUltimo.Text = Resumen();
            lblUltimo.ForeColor = Pendiente ? Estilo.Naranja : Estilo.Libre;
        }

        void Respaldar()
        {
            string archivo = null;
            btnRespaldar.Enabled = false;
            try
            {
                if (!Ui.Seguro(this, () => archivo = RespaldoRepo.Hacer(txtCarpeta.Text))) return;
            }
            finally
            {
                btnRespaldar.Enabled = true;
            }
            Mostrar();
            MessageBox.Show(this, "Listo, el respaldo quedó en\n" + archivo, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // mas de un dia sin respaldo ya es para preocuparse
        public static bool Pendiente
        {
            get
            {
                var u = RespaldoRepo.Ultimo;
                return !u.HasValue || (DateTime.Now - u.Value).TotalHours > 24;
            }
        }

        public static string Resumen()
        {
            var u = RespaldoRepo.Ultimo;
            if (!u.HasValue) return "Todavía no se ha respaldado";
            var horas = (DateTime.Now - u.Value).TotalHours;
            if (horas < 1) return "Último respaldo hace un momento";
            if (horas < 24) return "Último respaldo hace " + (int)horas + (horas < 2 ? " hora" : " horas");
            var dias = (int)(horas / 24);
            return "Último respaldo hace " + dias + (dias == 1 ? " día" : " días");
        }
    }
}
