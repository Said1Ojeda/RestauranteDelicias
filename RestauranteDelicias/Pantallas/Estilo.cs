using System.Drawing;
using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // colores y botones en un solo lugar para que todas las pantallas se vean igual
    public static class Estilo
    {
        public static readonly Color Vino = Color.FromArgb(128, 28, 40);
        public static readonly Color Crema = Color.FromArgb(250, 245, 236);
        public static readonly Color Libre = Color.FromArgb(46, 139, 87);
        public static readonly Color Ocupada = Color.FromArgb(196, 69, 54);
        public static readonly Color Gris = Color.FromArgb(90, 90, 90);
        public static readonly Color Naranja = Color.FromArgb(214, 128, 30);
        public static readonly Color Azul = Color.FromArgb(40, 104, 170);

        public static readonly Font Normal = new Font("Segoe UI", 10f);
        public static readonly Font Negrita = new Font("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font Titulo = new Font("Segoe UI", 16f, FontStyle.Bold);
        public static readonly Font Total = new Font("Segoe UI", 18f, FontStyle.Bold);

        public static void Formulario(Form f, string titulo, int ancho, int alto)
        {
            f.Text = titulo;
            f.Font = Normal;
            f.BackColor = Crema;
            f.ClientSize = new Size(ancho, alto);
            f.StartPosition = FormStartPosition.CenterParent;
            f.MinimumSize = f.Size;
        }

        public static Button Boton(string texto, Color fondo)
        {
            var b = new Button
            {
                Text = texto,
                BackColor = fondo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Negrita,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(4),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static Panel Encabezado(string texto)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Vino };
            panel.Controls.Add(new Label
            {
                Text = texto,
                ForeColor = Color.White,
                Font = Titulo,
                AutoSize = true,
                Location = new Point(16, 12)
            });
            return panel;
        }

        public static void Tabla(DataGridView g)
        {
            g.AllowUserToAddRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.MultiSelect = false;
            g.BackgroundColor = Color.White;
            g.BorderStyle = BorderStyle.None;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersDefaultCellStyle.BackColor = Vino;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Negrita;
            g.ColumnHeadersHeight = 32;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.RowTemplate.Height = 28;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(240, 214, 160);
            g.DefaultCellStyle.SelectionForeColor = Color.Black;
        }
    }
}
