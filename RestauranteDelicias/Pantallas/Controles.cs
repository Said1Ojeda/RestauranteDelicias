using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RestauranteDelicias.Pantallas
{
    // boton con esquinas redondeadas, cambia de tono al pasar el mouse y puede llevar icono
    public class Boton : Button
    {
        bool encima;
        bool apretado;
        string icono;
        bool contorno;
        bool marcado;
        bool izquierda;
        int radio = 8;

        public Boton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = Estilo.Negrita;
            ForeColor = Color.White;
            Padding = new Padding(12, 6, 12, 6);
        }

        public string Icono
        {
            get { return icono; }
            set { icono = value; Invalidate(); }
        }

        // blanco con borde, el color de fondo se usa como acento
        public bool Contorno
        {
            get { return contorno; }
            set { contorno = value; Invalidate(); }
        }

        // para filtros y opciones, el que esta escogido se pinta lleno
        public bool Marcado
        {
            get { return marcado; }
            set { marcado = value; Invalidate(); }
        }

        // icono y texto pegados a la izquierda, como en la barra lateral
        public bool Izquierda
        {
            get { return izquierda; }
            set { izquierda = value; Invalidate(); }
        }

        public int Radio
        {
            get { return radio; }
            set { radio = value; Invalidate(); }
        }

        // los del teclado del pin no se quedan con el foco, asi el enter no repite el ultimo numero
        public bool Enfocable
        {
            get { return GetStyle(ControlStyles.Selectable); }
            set { SetStyle(ControlStyles.Selectable, value); TabStop = value; }
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }

        bool ConIcono
        {
            get { return !string.IsNullOrEmpty(icono) && Estilo.HayIconos; }
        }

        Font FuenteIcono
        {
            get { return Estilo.Iconos(Font.SizeInPoints + 2f); }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var t = TextRenderer.MeasureText(Text ?? "", Font);
            int ancho = t.Width + Padding.Horizontal;
            int alto = t.Height + Padding.Vertical;
            if (ConIcono)
            {
                var i = TextRenderer.MeasureText(icono, FuenteIcono, Size.Empty, TextFormatFlags.NoPadding);
                ancho += i.Width + (string.IsNullOrEmpty(Text) ? 0 : Estilo.Px(8));
                alto = Math.Max(alto, i.Height + Padding.Vertical);
            }
            return new Size(Math.Max(ancho, MinimumSize.Width), Math.Max(alto, Math.Max(Estilo.Px(34), MinimumSize.Height)));
        }

        protected override void OnMouseEnter(EventArgs e) { encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { encima = false; apretado = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { apretado = true; Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            apretado = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Estilo.FondoDe(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool lleno = !contorno || marcado;
            Color fondo = lleno ? BackColor : Color.White;
            Color letra = lleno ? ForeColor : Estilo.Texto;
            Color colorIcono = lleno ? ForeColor : BackColor;

            if (!Enabled)
            {
                fondo = lleno ? Estilo.Mezclar(BackColor, Color.White, 0.55f) : Color.White;
                letra = colorIcono = lleno ? Color.FromArgb(245, 245, 245) : Color.Silver;
            }
            else if (apretado) fondo = Estilo.Mezclar(fondo, Color.Black, 0.15f);
            else if (encima) fondo = lleno ? Estilo.Mezclar(fondo, Color.White, 0.12f) : Estilo.Mezclar(BackColor, Color.White, 0.92f);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Estilo.Redondo(r, Estilo.Px(radio)))
            {
                using (var b = new SolidBrush(fondo)) g.FillPath(b, path);
                if (!lleno)
                    using (var p = new Pen(encima && Enabled ? BackColor : Estilo.Borde)) g.DrawPath(p, path);
            }

            var texto = Text ?? "";
            var medida = TextRenderer.MeasureText(g, texto, Font, Size.Empty, TextFormatFlags.NoPadding);
            int anchoIcono = 0;
            Size medidaIcono = Size.Empty;
            if (ConIcono)
            {
                medidaIcono = TextRenderer.MeasureText(g, icono, FuenteIcono, Size.Empty, TextFormatFlags.NoPadding);
                anchoIcono = medidaIcono.Width + (texto.Length > 0 ? Estilo.Px(8) : 0);
            }

            int total = anchoIcono + medida.Width;
            int x = izquierda ? Padding.Left : Math.Max(Padding.Left / 2, (Width - total) / 2);
            if (ConIcono)
            {
                TextRenderer.DrawText(g, icono, FuenteIcono, new Point(x, (Height - medidaIcono.Height) / 2), colorIcono, TextFormatFlags.NoPadding);
                x += anchoIcono;
            }
            var area = new Rectangle(x, 0, Math.Max(1, Width - x - Padding.Right / 2), Height);
            TextRenderer.DrawText(g, texto, Font, area, letra,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }

    // tarjeta dibujada a mano, se usa para las mesas, los platillos y las opciones de ajustes
    public class Tarjeta : Control
    {
        bool encima;
        string titulo = "";
        string detalle = "";
        string pie = "";
        string etiqueta = "";
        string icono;
        Color acento = Estilo.Vino;
        bool apagada;

        public Tarjeta()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // cada toque cuenta, asi dos toques rapidos agregan dos platillos
            SetStyle(ControlStyles.StandardDoubleClick, false);
            Cursor = Cursors.Hand;
            Font = Estilo.Normal;
            Margin = new Padding(6);
        }

        public string Titulo { get { return titulo; } set { titulo = value ?? ""; Invalidate(); } }
        public string Detalle { get { return detalle; } set { detalle = value ?? ""; Invalidate(); } }
        public string Pie { get { return pie; } set { pie = value ?? ""; Invalidate(); } }
        public string Etiqueta { get { return etiqueta; } set { etiqueta = value ?? ""; Invalidate(); } }
        public string Icono { get { return icono; } set { icono = value; Invalidate(); } }
        public Color Acento { get { return acento; } set { acento = value; Invalidate(); } }
        public bool Apagada { get { return apagada; } set { apagada = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { encima = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { encima = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Estilo.FondoDe(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int radio = Estilo.Px(10);
            int sombra = Estilo.Px(encima ? 4 : 2);
            var r = new Rectangle(0, 0, Width - 1 - sombra, Height - 1 - sombra);

            using (var path = Estilo.Redondo(new Rectangle(r.X + sombra / 2, r.Y + sombra, r.Width, r.Height), radio))
            using (var b = new SolidBrush(Color.FromArgb(encima ? 28 : 16, 0, 0, 0)))
                g.FillPath(b, path);

            var color = apagada ? Color.Silver : acento;
            using (var path = Estilo.Redondo(r, radio))
            {
                g.FillPath(Brushes.White, path);

                // la franja de color de arriba dice el estado sin tener que leer
                var antes = g.Clip;
                g.SetClip(path, CombineMode.Intersect);
                using (var b = new SolidBrush(color)) g.FillRectangle(b, r.X, r.Y, r.Width + 1, Estilo.Px(5));
                g.Clip = antes;
                antes.Dispose();

                using (var p = new Pen(encima && !apagada ? Estilo.Mezclar(color, Color.White, 0.35f) : Estilo.Borde))
                    g.DrawPath(p, path);
            }

            int m = Estilo.Px(12);
            int y = Estilo.Px(14);
            var colorTexto = apagada ? Color.DarkGray : Estilo.Texto;
            var colorSuave = apagada ? Color.Silver : Estilo.Suave;

            if (!string.IsNullOrEmpty(icono) && Estilo.HayIconos)
            {
                int d = Estilo.Px(36);
                using (var b = new SolidBrush(Estilo.Mezclar(color, Color.White, 0.86f)))
                    g.FillEllipse(b, r.X + m, y, d, d);
                TextRenderer.DrawText(g, icono, Estilo.Iconos(14f), new Rectangle(r.X + m, y, d, d), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                y += d + Estilo.Px(8);
            }

            int anchoEtiqueta = 0;
            if (etiqueta.Length > 0)
            {
                var medida = TextRenderer.MeasureText(g, etiqueta, Estilo.Chica, Size.Empty, TextFormatFlags.NoPadding);
                var pastilla = new Rectangle(r.Right - m - medida.Width - Estilo.Px(14), Estilo.Px(14), medida.Width + Estilo.Px(14), medida.Height + Estilo.Px(6));
                using (var path = Estilo.Redondo(pastilla, pastilla.Height / 2))
                using (var b = new SolidBrush(Estilo.Mezclar(color, Color.White, 0.85f)))
                    g.FillPath(b, path);
                TextRenderer.DrawText(g, etiqueta, Estilo.Chica, pastilla, Estilo.Mezclar(color, Color.Black, 0.25f),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                anchoEtiqueta = pastilla.Width + Estilo.Px(6);
            }

            var flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
            int altoPie = pie.Length > 0 ? Estilo.Grande.Height + Estilo.Px(10) : 0;
            int anchoTexto = r.Width - m * 2;

            // el titulo puede usar dos lineas, los nombres de platillos a veces son largos
            int altoTitulo = Math.Min(Estilo.Grande.Height * 2, r.Bottom - altoPie - y - Estilo.Px(4));
            var areaTitulo = new Rectangle(r.X + m, y, anchoTexto - (icono == null ? anchoEtiqueta : 0), Math.Max(altoTitulo, Estilo.Grande.Height));
            var medidaTitulo = TextRenderer.MeasureText(g, titulo, Estilo.Grande, areaTitulo.Size, flags | TextFormatFlags.WordBreak);
            TextRenderer.DrawText(g, titulo, Estilo.Grande, areaTitulo, colorTexto, flags | TextFormatFlags.WordBreak);
            y += Math.Min(medidaTitulo.Height, areaTitulo.Height) + Estilo.Px(2);

            if (detalle.Length > 0 && y + Estilo.Chica.Height <= r.Bottom - altoPie)
                TextRenderer.DrawText(g, detalle, Estilo.Chica, new Rectangle(r.X + m, y, anchoTexto, Estilo.Chica.Height * 2), colorSuave, flags | TextFormatFlags.WordBreak);

            if (pie.Length > 0)
                TextRenderer.DrawText(g, pie, Estilo.Grande, new Rectangle(r.X + m, r.Bottom - altoPie, anchoTexto, altoPie - Estilo.Px(6)),
                    apagada ? colorSuave : Estilo.Texto, flags | TextFormatFlags.Bottom | TextFormatFlags.SingleLine);
        }
    }
}
