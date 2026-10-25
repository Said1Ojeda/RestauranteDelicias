using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // vista del ticket como se va a ver en el papel, con el boton para mandarlo a la impresora
    public class FrmTicket : Form
    {
        readonly string texto;
        readonly Panel mesa = new Panel();
        readonly Panel papel = new Panel();

        public FrmTicket(string texto)
        {
            this.texto = texto;
            Estilo.Formulario(this, "Ticket", 460, 700);
            MinimizeBox = false;
            ShowInTaskbar = false;

            mesa.Dock = DockStyle.Fill;
            mesa.AutoScroll = true;
            mesa.BackColor = Color.FromArgb(232, 230, 226);
            mesa.Resize += (s, e) => Acomodar();
            mesa.Paint += (s, e) =>
            {
                // sombra suave debajo del papel
                var r = new Rectangle(papel.Left + 3, papel.Top + 5, papel.Width, papel.Height);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new SolidBrush(Color.FromArgb(30, 0, 0, 0))) e.Graphics.FillRectangle(b, r);
            };

            papel.BackColor = Color.White;
            papel.Paint += (s, e) =>
            {
                int m = Estilo.Px(18);
                float alto;
                Ticket.Dibujar(e.Graphics, Renglones(), 0, new RectangleF(m, m, papel.Width - m * 2, 100000f), out alto);
            };
            mesa.Controls.Add(papel);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(14, 12, 14, 12), FlowDirection = FlowDirection.RightToLeft, BackColor = Color.White };
            var imprimir = Estilo.Boton("Imprimir", Estilo.Vino, Icono.Imprimir);
            var otra = Estilo.Boton("Otra impresora", Estilo.Gris);
            var cerrar = Estilo.Boton("Cerrar", Estilo.Gris);
            otra.Contorno = cerrar.Contorno = true;
            imprimir.Click += (s, e) => Imprimir(false);
            otra.Click += (s, e) => Imprimir(true);
            cerrar.Click += (s, e) => Close();
            botones.Controls.AddRange(new Control[] { imprimir, otra, cerrar });

            Controls.Add(mesa);
            Controls.Add(botones);
            AcceptButton = imprimir;
            CancelButton = cerrar;
            Load += (s, e) => Acomodar();
        }

        string[] Renglones()
        {
            return texto.Replace("\r", "").TrimEnd('\n').Split('\n');
        }

        // el papel tiene el ancho del rollo y el alto de lo que trae, centrado en la ventana
        void Acomodar()
        {
            int ancho = Estilo.Px(310);
            int m = Estilo.Px(18);
            float alto;
            using (var g = papel.CreateGraphics())
                alto = Ticket.Medir(g, texto, ancho - m * 2);
            papel.Size = new Size(ancho, (int)alto + m * 2);
            papel.Location = new Point(Math.Max(Estilo.Px(10), (mesa.ClientSize.Width - ancho) / 2) + mesa.AutoScrollPosition.X,
                                       Estilo.Px(20) + mesa.AutoScrollPosition.Y);
            mesa.AutoScrollMinSize = new Size(ancho + Estilo.Px(20), papel.Height + Estilo.Px(40));
            mesa.Invalidate();
        }

        void Imprimir(bool escoger)
        {
            using (var doc = Ticket.Documento(texto, "Ticket"))
            {
                if (escoger)
                {
                    using (var dlg = new PrintDialog { Document = doc, UseEXDialog = true })
                        if (dlg.ShowDialog(this) != DialogResult.OK) return;
                }
                if (Ui.Seguro(this, doc.Print)) Close();
            }
        }
    }
}
