using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // pantalla para la compu de la cocina o de la barra, oscura para que no canse la vista
    public class FrmCocina : Form
    {
        public static readonly Color Fondo = Color.FromArgb(22, 22, 27);
        public static readonly Color Superficie = Color.FromArgb(37, 37, 44);
        public static readonly Color Linea = Color.FromArgb(60, 60, 70);
        public static readonly Color Claro = Color.FromArgb(238, 238, 242);
        public static readonly Color Apagado = Color.FromArgb(150, 150, 162);

        const int REFRESCO_MS = 3000;

        readonly bool comoVentana;
        readonly FlowLayoutPanel panel = new FlowLayoutPanel();
        readonly FlowLayoutPanel estaciones = new FlowLayoutPanel();
        readonly Label lblReloj = new Label();
        readonly Label lblCuenta = new Label();
        readonly Label lblVacio = new Label();
        readonly Timer refresco = new Timer();
        readonly Timer reloj = new Timer();
        readonly Stack<List<int>> hechos = new Stack<List<int>>();
        readonly HashSet<string> vistas = new HashSet<string>();
        readonly Boton btnDeshacer;
        string estacion = "Cocina";
        string firma = "";
        bool refrescando;
        bool primera = true;

        // como ventana es cuando el admin la abre desde las mesas, si no es la pantalla de un usuario de cocina
        public FrmCocina(bool comoVentana)
        {
            this.comoVentana = comoVentana;
            Estilo.Formulario(this, "Cocina · " + Configuracion.Nombre, 1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            BackColor = Fondo;
            ForeColor = Claro;

            var cabeza = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Fondo, Padding = new Padding(20, 14, 20, 14) };
            var lblTitulo = new Label { Text = "Cocina", Font = Estilo.Titulo, ForeColor = Claro, AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 6, 18, 0) };

            estaciones.Dock = DockStyle.Left;
            estaciones.AutoSize = true;
            estaciones.BackColor = Fondo;
            estaciones.Padding = new Padding(0, 2, 0, 0);
            foreach (var par in new[] { new[] { "Cocina", "Cocina" }, new[] { "Barra", "Barra" }, new[] { "Todo", "" } })
            {
                var valor = par[1].Length == 0 ? null : par[1];
                var b = Estilo.Boton(par[0], Superficie);
                b.Contorno = false;
                b.Radio = 16;
                b.Font = Estilo.Normal;
                b.Tag = valor;
                b.Click += async (s, e) =>
                {
                    estacion = valor;
                    PintarEstaciones();
                    firma = "";
                    await Refrescar();
                };
                estaciones.Controls.Add(b);
            }

            lblReloj.Font = new Font("Segoe UI Semibold", 20f);
            lblReloj.ForeColor = Claro;
            lblReloj.AutoSize = true;
            lblReloj.Dock = DockStyle.Right;
            lblReloj.Padding = new Padding(14, 0, 0, 0);

            lblCuenta.ForeColor = Apagado;
            lblCuenta.AutoSize = true;
            lblCuenta.Dock = DockStyle.Right;
            lblCuenta.Padding = new Padding(14, 12, 6, 0);

            var acciones = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, BackColor = Fondo, WrapContents = false };
            btnDeshacer = Estilo.Boton("Deshacer", Superficie, Icono.Deshacer);
            btnDeshacer.Contorno = false;
            btnDeshacer.Visible = false;
            btnDeshacer.Click += async (s, e) => await Deshacer();
            var salir = Estilo.Boton(comoVentana ? "Cerrar" : "Cambiar usuario", Superficie, comoVentana ? Icono.Volver : Icono.Cambiar);
            salir.Contorno = false;
            salir.Click += (s, e) => Salir();
            acciones.Controls.AddRange(new Control[] { btnDeshacer, salir });

            cabeza.Controls.Add(estaciones);
            cabeza.Controls.Add(lblTitulo);
            cabeza.Controls.Add(lblCuenta);
            cabeza.Controls.Add(lblReloj);
            cabeza.Controls.Add(acciones);

            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            panel.BackColor = Fondo;
            panel.Padding = new Padding(14, 4, 14, 14);
            panel.Resize += (s, e) => AcomodarTarjetas();

            lblVacio.Dock = DockStyle.Fill;
            lblVacio.TextAlign = ContentAlignment.MiddleCenter;
            lblVacio.Font = Estilo.Grande;
            lblVacio.ForeColor = Apagado;
            lblVacio.Text = "Todo al día, no hay pedidos pendientes";
            lblVacio.Visible = false;

            Controls.Add(panel);
            Controls.Add(lblVacio);
            Controls.Add(cabeza);

            refresco.Interval = REFRESCO_MS;
            refresco.Tick += async (s, e) => await Refrescar();
            reloj.Interval = 1000;
            reloj.Tick += (s, e) => Reloj();
            Load += (s, e) => { PintarEstaciones(); Reloj(); };
            Shown += async (s, e) => { await Refrescar(); refresco.Start(); reloj.Start(); };
            FormClosed += (s, e) => { refresco.Stop(); reloj.Stop(); };
        }

        void PintarEstaciones()
        {
            foreach (var b in estaciones.Controls.OfType<Boton>())
            {
                var esta = (string)b.Tag == estacion;
                b.BackColor = esta ? Estilo.Naranja : Superficie;
                b.ForeColor = esta ? Color.White : Apagado;
            }
        }

        void Reloj()
        {
            lblReloj.Text = DateTime.Now.ToString("HH:mm");
            // el tiempo de cada tarjeta avanza solo
            foreach (Control c in panel.Controls) c.Invalidate();
        }

        async Task Refrescar()
        {
            if (refrescando) return;
            refrescando = true;
            try
            {
                var que = estacion;
                var comandas = await Task.Run(() => CocinaRepo.Pendientes(que));
                if (IsDisposed) return;
                var nueva = string.Join(";", comandas.Select(c => c.Clave + ":" +
                    string.Join(",", c.Lineas.Select(l => l.Id + l.Estado + (l.Anulado ? "x" : "")))));
                if (nueva != firma)
                {
                    firma = nueva;
                    Armar(comandas);
                }

                // suena cuando llega un pedido nuevo, no al abrir la pantalla
                var llegaron = comandas.Where(c => !vistas.Contains(c.Clave)).ToList();
                if (!primera && llegaron.Count > 0)
                {
                    try { SystemSounds.Exclamation.Play(); }
                    catch (Exception) { }
                }
                foreach (var c in llegaron) vistas.Add(c.Clave);
                primera = false;
                lblCuenta.Text = comandas.Count == 1 ? "1 pedido" : comandas.Count + " pedidos";
            }
            catch (Exception ex)
            {
                Log.Error("Refrescar cocina", ex);
                if (IsDisposed) return;
                lblCuenta.Text = "Sin conexión, reintentando...";
            }
            finally
            {
                refrescando = false;
            }
        }

        void Armar(List<Comanda> comandas)
        {
            panel.SuspendLayout();
            Estilo.Vaciar(panel);
            var alerta = (int)Configuracion.Numero("Cocina.MinutosAlerta", 15);
            foreach (var c in comandas)
            {
                var comanda = c;
                var t = new TarjetaComanda(comanda, alerta);
                t.Empezar += async (s, e) => await Accion(() => CocinaRepo.Empezar(comanda.Ids), null);
                t.Listo += async (s, e) => await Accion(() => CocinaRepo.Listo(comanda.Ids), comanda.Ids);
                panel.Controls.Add(t);
            }
            AcomodarTarjetas();
            panel.ResumeLayout();
            lblVacio.Visible = comandas.Count == 0;
            panel.Visible = comandas.Count > 0;
        }

        void AcomodarTarjetas()
        {
            var disponible = panel.ClientSize.Width - panel.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth;
            if (disponible > 0) Estilo.RepartirTarjetas(panel, disponible, Estilo.Px(280), Estilo.Px(380));
        }

        async Task Accion(Action accion, List<int> paraDeshacer)
        {
            if (!Ui.Seguro(this, accion)) return;
            if (paraDeshacer != null)
            {
                hechos.Push(paraDeshacer);
                btnDeshacer.Visible = true;
            }
            await Refrescar();
        }

        async Task Deshacer()
        {
            if (hechos.Count == 0) return;
            var ids = hechos.Pop();
            btnDeshacer.Visible = hechos.Count > 0;
            Ui.Seguro(this, () => CocinaRepo.Deshacer(ids));
            await Refrescar();
        }

        void Salir()
        {
            if (!comoVentana)
            {
                try { Bitacora.Registrar("Salida", Sesion.Nombre); }
                catch (Exception ex) { Log.Error("Registrar salida", ex); }
                Sesion.Cerrar();
                Sesion.OtroUsuario = true;
            }
            Close();
        }
    }

    // una tarjeta por cada envio de una mesa, con lo que hay que preparar y los botones
    public class TarjetaComanda : Control
    {
        readonly Comanda comanda;
        readonly int alerta;
        readonly int altoCabeza = Estilo.Px(66);
        readonly int altoPie = Estilo.Px(62);

        public event EventHandler Empezar;
        public event EventHandler Listo;

        public TarjetaComanda(Comanda comanda, int alerta)
        {
            this.comanda = comanda;
            this.alerta = Math.Max(1, alerta);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = FrmCocina.Superficie;
            Font = Estilo.Normal;
            Margin = new Padding(Estilo.Px(7));

            int alto = altoCabeza + altoPie + Estilo.Px(8);
            foreach (var l in comanda.Lineas)
                alto += Estilo.Px(32) + (string.IsNullOrWhiteSpace(l.Opciones) ? 0 : Estilo.Px(22)) + (string.IsNullOrWhiteSpace(l.Nota) ? 0 : Estilo.Px(22));
            Size = new Size(Estilo.Px(300), alto);

            var pie = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = altoPie,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(Estilo.Px(10), Estilo.Px(8), Estilo.Px(10), Estilo.Px(10)),
                BackColor = FrmCocina.Superficie
            };
            var listo = Estilo.Boton(comanda.TodoAnulado ? "Quitar" : "Listo", comanda.TodoAnulado ? Estilo.Gris : Estilo.Libre, Icono.Listo);
            listo.Contorno = false;
            listo.Font = Estilo.Grande;
            listo.Click += (s, e) => { if (Listo != null) Listo(this, EventArgs.Empty); };
            pie.Controls.Add(listo);
            if (comanda.SinEmpezar && !comanda.TodoAnulado)
            {
                var empezar = Estilo.Boton("Empezar", Estilo.Azul, Icono.Empezar);
                empezar.Font = Estilo.Grande;
                empezar.Click += (s, e) => { if (Empezar != null) Empezar(this, EventArgs.Empty); };
                pie.Controls.Add(empezar);
            }
            Controls.Add(pie);
        }

        Color ColorTiempo
        {
            get
            {
                var m = comanda.Minutos;
                if (m >= alerta) return Estilo.Ocupada;
                if (m >= alerta / 2) return Estilo.Naranja;
                return Estilo.Libre;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(FrmCocina.Fondo);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            var tiempo = ColorTiempo;
            using (var path = Estilo.Redondo(r, Estilo.Px(12)))
            {
                using (var b = new SolidBrush(FrmCocina.Superficie)) g.FillPath(b, path);

                // la franja de arriba se pone naranja y luego roja conforme pasa el tiempo
                var antes = g.Clip;
                g.SetClip(path, CombineMode.Intersect);
                using (var b = new SolidBrush(tiempo)) g.FillRectangle(b, 0, 0, Width, Estilo.Px(6));
                g.Clip = antes;
                antes.Dispose();

                using (var p = new Pen(comanda.Minutos >= alerta ? tiempo : FrmCocina.Linea, comanda.Minutos >= alerta ? 2f : 1f))
                    g.DrawPath(p, path);
            }

            int m = Estilo.Px(16);
            var anchoTitulo = Width - m * 2 - Estilo.Px(90);
            TextRenderer.DrawText(g, comanda.Titulo, Estilo.Titulo, new Rectangle(m, Estilo.Px(16), anchoTitulo, Estilo.Titulo.Height + Estilo.Px(4)), FrmCocina.Claro,
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            var quien = (comanda.Mesero ?? "") + "  ·  " + comanda.Enviado.ToString("HH:mm");
            TextRenderer.DrawText(g, quien, Estilo.Chica, new Point(m, Estilo.Px(44)), FrmCocina.Apagado, TextFormatFlags.NoPadding);

            // los minutos en una pastilla a la derecha
            var minutos = comanda.Minutos + " min";
            var medida = TextRenderer.MeasureText(g, minutos, Estilo.Grande, Size.Empty, TextFormatFlags.NoPadding);
            var pastilla = new Rectangle(Width - m - medida.Width - Estilo.Px(18), Estilo.Px(18), medida.Width + Estilo.Px(18), medida.Height + Estilo.Px(8));
            using (var path = Estilo.Redondo(pastilla, pastilla.Height / 2))
            using (var b = new SolidBrush(Color.FromArgb(50, tiempo)))
                g.FillPath(b, path);
            TextRenderer.DrawText(g, minutos, Estilo.Grande, pastilla, Estilo.Mezclar(tiempo, Color.White, 0.3f),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            using (var p = new Pen(FrmCocina.Linea)) g.DrawLine(p, m, altoCabeza, Width - m, altoCabeza);

            int y = altoCabeza + Estilo.Px(8);
            using (var tachado = new Font(Estilo.Grande, FontStyle.Strikeout))
            using (var nota = new Font(Estilo.Normal, FontStyle.Italic))
            {
                foreach (var l in comanda.Lineas)
                {
                    var color = l.Anulado ? Estilo.Ocupada : FrmCocina.Claro;
                    var letra = l.Anulado ? tachado : Estilo.Grande;
                    TextRenderer.DrawText(g, l.Cantidad.ToString(), Estilo.Grande, new Rectangle(m, y, Estilo.Px(34), Estilo.Px(30)), l.Anulado ? color : Estilo.Naranja,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    var nombre = l.Nombre + (l.Anulado ? "  ANULADO" : "");
                    TextRenderer.DrawText(g, nombre, letra, new Rectangle(m + Estilo.Px(34), y, Width - m * 2 - Estilo.Px(50), Estilo.Px(30)), color,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

                    // un punto azul dice que ya lo estan preparando
                    if (l.Estado == "Preparando" && !l.Anulado)
                        using (var b = new SolidBrush(Estilo.Azul))
                            g.FillEllipse(b, Width - m - Estilo.Px(10), y + Estilo.Px(10), Estilo.Px(10), Estilo.Px(10));
                    y += Estilo.Px(32);

                    // lo que escogieron va resaltado, es lo que mas se equivoca en cocina
                    if (!string.IsNullOrWhiteSpace(l.Opciones))
                    {
                        TextRenderer.DrawText(g, l.Opciones, Estilo.Negrita, new Rectangle(m + Estilo.Px(34), y - Estilo.Px(4), Width - m * 2 - Estilo.Px(34), Estilo.Px(22)),
                            Estilo.Mezclar(Estilo.Azul, Color.White, 0.45f), TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                        y += Estilo.Px(22);
                    }

                    if (!string.IsNullOrWhiteSpace(l.Nota))
                    {
                        TextRenderer.DrawText(g, l.Nota, nota, new Rectangle(m + Estilo.Px(34), y - Estilo.Px(4), Width - m * 2 - Estilo.Px(34), Estilo.Px(22)),
                            Estilo.Mezclar(Estilo.Naranja, Color.White, 0.3f), TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                        y += Estilo.Px(22);
                    }
                }
            }
        }
    }
}
