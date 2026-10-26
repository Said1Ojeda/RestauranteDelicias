using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // colores, letras y medidas en un solo lugar para que todas las pantallas se vean igual
    public static class Estilo
    {
        // color de la marca
        public static readonly Color Vino = Color.FromArgb(155, 28, 49);
        public static readonly Color VinoSuave = Color.FromArgb(252, 236, 239);

        // fondos y textos
        public static readonly Color Crema = Color.FromArgb(246, 245, 242);
        public static readonly Color Borde = Color.FromArgb(228, 226, 221);
        public static readonly Color Texto = Color.FromArgb(30, 33, 38);
        public static readonly Color Suave = Color.FromArgb(108, 113, 121);
        public static readonly Color Lateral = Color.FromArgb(27, 27, 33);
        public static readonly Color LateralTexto = Color.FromArgb(206, 206, 214);

        // estados de las mesas y de los botones
        public static readonly Color Libre = Color.FromArgb(22, 150, 72);
        public static readonly Color Ocupada = Color.FromArgb(214, 48, 49);
        public static readonly Color Naranja = Color.FromArgb(230, 135, 14);
        public static readonly Color Azul = Color.FromArgb(37, 99, 235);
        public static readonly Color Gris = Color.FromArgb(84, 90, 100);

        public static readonly Font Normal = new Font("Segoe UI", 10f);
        public static readonly Font Chica = new Font("Segoe UI", 9f);
        public static readonly Font Negrita = new Font("Segoe UI Semibold", 10f);
        public static readonly Font Grande = new Font("Segoe UI Semibold", 13f);
        public static readonly Font Titulo = new Font("Segoe UI Semibold", 15f);
        public static readonly Font Total = new Font("Segoe UI", 21f, FontStyle.Bold);

        // cuanto se agranda todo con el escalado de windows, 1 es al 100
        public static readonly float K = LeerEscala();

        static float LeerEscala()
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    return Math.Max(1f, g.DpiX / 96f);
            }
            catch
            {
                return 1f;
            }
        }

        public static int Px(int medida)
        {
            return (int)Math.Round(medida * K);
        }

        // la letra de iconos de windows, la de windows 11 o la de windows 10
        static readonly string familiaIconos = BuscarIconos();
        static readonly Dictionary<float, Font> iconos = new Dictionary<float, Font>();

        static string BuscarIconos()
        {
            try
            {
                using (var fuentes = new InstalledFontCollection())
                    foreach (var nombre in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
                        if (fuentes.Families.Any(f => f.Name == nombre)) return nombre;
            }
            catch
            {
            }
            return null;
        }

        public static bool HayIconos
        {
            get { return familiaIconos != null; }
        }

        public static Font Iconos(float tamano)
        {
            if (familiaIconos == null) return null;
            Font f;
            if (!iconos.TryGetValue(tamano, out f))
            {
                f = new Font(familiaIconos, tamano);
                iconos[tamano] = f;
            }
            return f;
        }

        public static void Formulario(Form f, string titulo, int ancho, int alto)
        {
            f.Text = titulo;
            f.Font = Normal;
            f.BackColor = Crema;
            f.ForeColor = Texto;
            f.AutoScaleMode = AutoScaleMode.None;
            f.ClientSize = new Size(ancho, alto);
            f.StartPosition = FormStartPosition.CenterParent;
            f.MinimumSize = f.Size;
            f.Load += (s, e) =>
            {
                Pulir(f);
                Escalar(f);
            };
        }

        // quita el borde hundido de windows viejo a las cajas y listas de todas las pantallas
        static void Pulir(Control padre)
        {
            foreach (Control c in padre.Controls)
            {
                var caja = c as TextBox;
                if (caja != null && caja.BorderStyle == BorderStyle.Fixed3D) caja.BorderStyle = BorderStyle.FixedSingle;
                var numero = c as NumericUpDown;
                if (numero != null && numero.BorderStyle == BorderStyle.Fixed3D) numero.BorderStyle = BorderStyle.FixedSingle;
                var lista = c as ComboBox;
                if (lista != null) lista.FlatStyle = FlatStyle.Flat;
                if (c.HasChildren) Pulir(c);
            }
        }

        // con windows al 125 o 150 se agranda todo parejo en vez de verse borroso o cortado
        static void Escalar(Form f)
        {
            if (K >= 1.05f)
            {
                var minimo = f.MinimumSize;
                f.MinimumSize = Size.Empty;
                f.Scale(new SizeF(K, K));
                f.MinimumSize = new Size(Px(minimo.Width), Px(minimo.Height));
            }
            if (f.WindowState == FormWindowState.Normal) AcomodarEnPantalla(f);
        }

        // en una laptop chica la ventana no puede quedar mas grande que la pantalla, se encoge y se centra
        static void AcomodarEnPantalla(Form f)
        {
            var pantalla = Screen.FromControl(f).WorkingArea;
            var ancho = Math.Min(f.Width, pantalla.Width);
            var alto = Math.Min(f.Height, pantalla.Height);
            f.MinimumSize = new Size(Math.Min(f.MinimumSize.Width, ancho), Math.Min(f.MinimumSize.Height, alto));
            f.Size = new Size(ancho, alto);

            var sobre = f.StartPosition == FormStartPosition.CenterParent && f.Owner != null ? f.Owner.Bounds : pantalla;
            var x = sobre.Left + (sobre.Width - f.Width) / 2;
            var y = sobre.Top + (sobre.Height - f.Height) / 2;
            f.Location = new Point(
                Math.Max(pantalla.Left, Math.Min(x, pantalla.Right - f.Width)),
                Math.Max(pantalla.Top, Math.Min(y, pantalla.Bottom - f.Height)));
        }

        // cuantas tarjetas caben por fila y de que ancho, asi llenan la fila en pantallas grandes y caben en las chicas
        public static int AnchoDeTarjeta(int disponible, int minimo, int maximo, int margen)
        {
            var columnas = Math.Max(1, disponible / (minimo + margen));
            var ancho = disponible / columnas - margen;
            return Math.Max(Math.Min(minimo, disponible - margen), Math.Min(maximo, ancho));
        }

        public static void RepartirTarjetas(FlowLayoutPanel fila, int disponible, int minimo, int maximo)
        {
            var hijos = fila.Controls.Cast<Control>().ToList();
            if (hijos.Count == 0) return;
            var margen = hijos[0].Margin.Horizontal;
            var ancho = AnchoDeTarjeta(disponible, minimo, maximo, margen);
            fila.SuspendLayout();
            foreach (var c in hijos) c.Width = ancho;
            fila.ResumeLayout();
        }

        // los grises son de cancelar o volver, van blancos con borde para que no compitan con el principal
        public static Boton Boton(string texto, Color fondo)
        {
            return new Boton { Text = texto, BackColor = fondo, AutoSize = true, Margin = new Padding(4), Contorno = fondo == Gris };
        }

        public static Boton Boton(string texto, Color fondo, string icono)
        {
            var b = Boton(texto, fondo);
            b.Icono = icono;
            return b;
        }

        // boton blanco con borde, para filtros, montos rapidos y opciones
        public static Boton Chip(string texto, Color acento)
        {
            return new Boton
            {
                Text = texto,
                BackColor = acento,
                Contorno = true,
                Radio = 16,
                Font = Normal,
                AutoSize = true,
                Margin = new Padding(3)
            };
        }

        // caja de texto sin el borde viejo de windows, con un marco suave alrededor
        // se arma en el constructor, el alto se divide entre K porque al cargar se escala todo
        public static Panel Campo(TextBox t)
        {
            var marco = new Panel { Height = (int)(t.PreferredHeight / K) + 16, BackColor = Color.White, Padding = new Padding(10, 8, 10, 6), Margin = new Padding(0, 4, 0, 4) };
            t.BorderStyle = BorderStyle.None;
            t.Dock = DockStyle.Fill;
            t.BackColor = Color.White;
            marco.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = Redondo(new Rectangle(0, 0, marco.Width - 1, marco.Height - 1), Px(6)))
                using (var p = new Pen(t.Focused ? Vino : Borde))
                    e.Graphics.DrawPath(p, path);
            };
            t.GotFocus += (s, e) => marco.Invalidate();
            t.LostFocus += (s, e) => marco.Invalidate();
            marco.Click += (s, e) => t.Focus();
            marco.Controls.Add(t);
            return marco;
        }

        public static Panel Encabezado(string texto)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White, Padding = new Padding(5, 0, 0, 1) };
            panel.Paint += (s, e) =>
            {
                using (var b = new SolidBrush(Vino)) e.Graphics.FillRectangle(b, 0, 0, 5, panel.Height);
                using (var p = new Pen(Borde)) e.Graphics.DrawLine(p, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };
            panel.Controls.Add(new Label
            {
                Text = texto,
                ForeColor = Texto,
                BackColor = Color.White,
                Font = Titulo,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0),
                AutoEllipsis = true
            });
            return panel;
        }

        // tablas limpias, sin cuadricula, con filas altas y separadas por una linea suave
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
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.GridColor = Borde;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            var cabeza = g.ColumnHeadersDefaultCellStyle;
            cabeza.BackColor = Color.White;
            cabeza.ForeColor = Suave;
            cabeza.SelectionBackColor = Color.White;
            cabeza.SelectionForeColor = Suave;
            cabeza.Font = Negrita;
            cabeza.Padding = new Padding(8, 0, 8, 0);
            cabeza.WrapMode = DataGridViewTriState.False;
            g.ColumnHeadersHeight = Negrita.Height * 2 + 8;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            g.DefaultCellStyle.Font = Normal;
            g.DefaultCellStyle.ForeColor = Texto;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            g.DefaultCellStyle.SelectionBackColor = VinoSuave;
            g.DefaultCellStyle.SelectionForeColor = Texto;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 249, 247);
            g.RowTemplate.Height = Normal.Height * 2 + 6;

            // sin esto la tabla parpadea al refrescar
            var prop = typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(g, true, null);
        }

        public static void Vaciar(Control contenedor)
        {
            var hijos = contenedor.Controls.Cast<Control>().ToList();
            contenedor.Controls.Clear();
            foreach (var c in hijos) c.Dispose();
        }

        public static GraphicsPath Redondo(Rectangle r, int radio)
        {
            var p = new GraphicsPath();
            int d = Math.Min(radio * 2, Math.Min(r.Width, r.Height));
            if (d <= 1)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // mezcla dos colores, cuanto va de 0 que es todo a hasta 1 que es todo b
        public static Color Mezclar(Color a, Color b, float cuanto)
        {
            cuanto = Math.Max(0f, Math.Min(1f, cuanto));
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * cuanto),
                (int)(a.G + (b.G - a.G) * cuanto),
                (int)(a.B + (b.B - a.B) * cuanto));
        }

        // el color que se guarda en la base como #RRGGBB, si viene vacio o malo se usa el de respaldo
        public static Color DeTexto(string hex, Color respaldo)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(hex) && hex.Trim().Length == 7 && hex.Trim()[0] == '#')
                    return ColorTranslator.FromHtml(hex.Trim());
            }
            catch
            {
            }
            return respaldo;
        }

        public static Color FondoDe(Control c)
        {
            var p = c.Parent;
            while (p != null && p.BackColor.A < 255) p = p.Parent;
            return p != null ? p.BackColor : Crema;
        }
    }

    // los iconos son letras de la fuente de iconos de windows, asi no se cargan imagenes
    public static class Icono
    {
        public const string Mesas = "";
        public const string Caja = "";
        public const string Ventas = "";
        public const string Menu = "";
        public const string Usuarios = "";
        public const string Usuario = "";
        public const string Bitacora = "";
        public const string Ajustes = "";
        public const string Restaurante = "";
        public const string Conexion = "";
        public const string Respaldo = "";
        public const string Cambiar = "";
        public const string Cocina = "";
        public const string Imprimir = "";
        public const string Buscar = "";
        public const string Agregar = "";
        public const string Quitar = "";
        public const string Enviar = "";
        public const string Descuento = "";
        public const string Tarjeta = "";
        public const string Banco = "";
        public const string Listo = "";
        public const string Reloj = "";
        public const string Candado = "";
        public const string Borrar = "";
        public const string Volver = "";
        public const string Deshacer = "";
        public const string Empezar = "";
        public const string Aviso = "";
    }
}
