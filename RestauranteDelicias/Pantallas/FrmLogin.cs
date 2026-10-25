using System;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Pantallas
{
    // se entra con usuario y contrasena, o con el pin en el teclado grande
    public class FrmLogin : Form
    {
        const int LARGO_PIN = 6;

        readonly TextBox txtUsuario = new TextBox();
        readonly TextBox txtClave = new TextBox();
        readonly Panel puntos = new Panel();
        readonly Label lblError = new Label();
        readonly Panel panelClave = new Panel();
        readonly Panel panelPin = new Panel();
        readonly Boton btnModoPin;
        readonly Boton btnModoClave;
        readonly bool soloAdmin;
        string pin = "";

        public Usuario Usuario { get; private set; }

        public FrmLogin() : this("Entrar", false) { }

        // tambien se usa para pedir la autorizacion de un admin
        public FrmLogin(string titulo, bool soloAdmin)
        {
            this.soloAdmin = soloAdmin;
            Estilo.Formulario(this, titulo, 400, 640);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            KeyPreview = true;
            KeyPress += AlTeclear;

            // arriba el nombre del restaurante en grande sobre el color de la marca
            var marca = new Panel { Dock = DockStyle.Top, Height = 132, BackColor = Estilo.Vino, Padding = new Padding(24, 26, 24, 16) };
            var lblNombre = new Label
            {
                Text = soloAdmin ? titulo : Configuracion.Nombre,
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI Semibold", 18f),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            var lblSub = new Label
            {
                Text = soloAdmin ? "Un administrador tiene que confirmar" : "Bienvenido, ingresa para empezar",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = Estilo.Mezclar(Estilo.Vino, Color.White, 0.75f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            marca.Controls.Add(lblSub);
            marca.Controls.Add(lblNombre);

            var modos = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(70, 14, 0, 0), BackColor = Color.White };
            btnModoPin = Estilo.Chip("Con PIN", Estilo.Vino);
            btnModoClave = Estilo.Chip("Con usuario", Estilo.Vino);
            btnModoPin.Enfocable = btnModoClave.Enfocable = false;
            btnModoPin.Click += (s, e) => Modo(true);
            btnModoClave.Click += (s, e) => Modo(false);
            modos.Controls.AddRange(new Control[] { btnModoPin, btnModoClave });

            ArmarPin();
            ArmarClave();

            lblError.Dock = DockStyle.Bottom;
            lblError.Height = 44;
            lblError.ForeColor = Estilo.Ocupada;
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            lblError.Font = Estilo.Negrita;

            Controls.Add(panelPin);
            Controls.Add(panelClave);
            Controls.Add(lblError);
            Controls.Add(modos);
            Controls.Add(marca);

            Modo(true);
        }

        void ArmarPin()
        {
            panelPin.Dock = DockStyle.Fill;
            panelPin.Padding = new Padding(36, 4, 36, 4);
            panelPin.BackColor = Color.White;

            // un circulo por numero, se llenan conforme se teclea el pin
            puntos.Dock = DockStyle.Top;
            puntos.Height = 52;
            puntos.BackColor = Color.White;
            puntos.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int d = Estilo.Px(16);
                int sep = Estilo.Px(14);
                int x = (puntos.Width - (LARGO_PIN * d + (LARGO_PIN - 1) * sep)) / 2;
                int y = (puntos.Height - d) / 2;
                for (int i = 0; i < LARGO_PIN; i++)
                {
                    var r = new Rectangle(x + i * (d + sep), y, d, d);
                    if (i < pin.Length)
                        using (var b = new SolidBrush(Estilo.Vino)) g.FillEllipse(b, r);
                    else
                        using (var p = new Pen(Estilo.Borde, Estilo.Px(2))) g.DrawEllipse(p, r);
                }
            };

            var teclado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Padding = new Padding(0, 10, 0, 0), BackColor = Color.White };
            for (int i = 0; i < 3; i++) teclado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            for (int i = 0; i < 4; i++) teclado.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var teclas = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "borrar", "0", "ok" };
            for (int i = 0; i < teclas.Length; i++)
            {
                var t = teclas[i];
                var b = new Boton
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(5),
                    Radio = 12,
                    Font = new Font("Segoe UI Semibold", 18f),
                    BackColor = Estilo.Vino,
                    Enfocable = false
                };
                if (t == "ok")
                {
                    b.Icono = Icono.Listo;
                    b.Text = Estilo.HayIconos ? "" : "OK";
                }
                else if (t == "borrar")
                {
                    b.Contorno = true;
                    b.Icono = Icono.Borrar;
                    b.Text = Estilo.HayIconos ? "" : "<";
                }
                else
                {
                    b.Contorno = true;
                    b.Text = t;
                }
                b.Click += (s, e) => Tecla(t);
                teclado.Controls.Add(b, i % 3, i / 3);
            }

            panelPin.Controls.Add(teclado);
            panelPin.Controls.Add(puntos);
        }

        void ArmarClave()
        {
            panelClave.Dock = DockStyle.Fill;
            panelClave.Padding = new Padding(36, 16, 36, 10);
            panelClave.BackColor = Color.White;

            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Color.White };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            txtUsuario.Font = txtClave.Font = new Font("Segoe UI", 12f);
            txtClave.UseSystemPasswordChar = true;
            txtClave.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; EntrarClave(); } };
            txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtClave.Focus(); } };

            var campoUsuario = Estilo.Campo(txtUsuario);
            var campoClave = Estilo.Campo(txtClave);
            campoUsuario.Dock = campoClave.Dock = DockStyle.Top;

            var btn = Estilo.Boton("Entrar", Estilo.Vino, Icono.Candado);
            btn.AutoSize = false;
            btn.Dock = DockStyle.Top;
            btn.Height = 44;
            btn.Margin = new Padding(0, 18, 0, 0);
            btn.Click += (s, e) => EntrarClave();

            t.Controls.Add(new Label { Text = "Usuario", AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(0, 6, 0, 0) });
            t.Controls.Add(campoUsuario);
            t.Controls.Add(new Label { Text = "Contraseña", AutoSize = true, ForeColor = Estilo.Suave, Margin = new Padding(0, 12, 0, 0) });
            t.Controls.Add(campoClave);
            t.Controls.Add(btn);
            foreach (Control c in t.Controls) if (!(c is Label)) c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            panelClave.Controls.Add(t);
        }

        void Modo(bool conPin)
        {
            panelPin.Visible = conPin;
            panelClave.Visible = !conPin;
            btnModoPin.Marcado = conPin;
            btnModoClave.Marcado = !conPin;
            lblError.Text = "";
            pin = "";
            puntos.Invalidate();
            // ActiveControl funciona aunque la ventana todavia no se haya creado, BeginInvoke no
            ActiveControl = conPin ? null : txtUsuario;
        }

        // en modo pin los numeros del teclado de la compu tambien sirven
        void AlTeclear(object sender, KeyPressEventArgs e)
        {
            if (!panelPin.Visible) return;
            if (char.IsDigit(e.KeyChar)) Tecla(e.KeyChar.ToString());
            else if (e.KeyChar == '\b') Tecla("borrar");
            else if (e.KeyChar == '\r') Tecla("ok");
            else return;
            e.Handled = true;
        }

        void Tecla(string t)
        {
            if (t == "ok") { EntrarPin(); return; }
            if (t == "borrar") { if (pin.Length > 0) pin = pin.Substring(0, pin.Length - 1); }
            else if (pin.Length < LARGO_PIN) pin += t;
            lblError.Text = "";
            puntos.Invalidate();
        }

        void EntrarPin()
        {
            var escrito = pin;
            pin = "";
            puntos.Invalidate();
            if (escrito.Length < 4) { lblError.Text = "El PIN tiene de 4 a 6 números."; return; }
            Terminar(() => UsuarioRepo.EntrarConPin(escrito), "PIN incorrecto.");
        }

        void EntrarClave()
        {
            var clave = txtClave.Text;
            txtClave.Clear();
            Terminar(() => UsuarioRepo.Entrar(txtUsuario.Text, clave), "Usuario o contraseña incorrectos.");
        }

        void Terminar(Func<Usuario> entrar, string siFalla)
        {
            Usuario u = null;
            try
            {
                Cursor = Cursors.WaitCursor;
                u = entrar();
            }
            catch (AvisoException ex)
            {
                lblError.Text = ex.Message;
                return;
            }
            catch (Exception ex)
            {
                Servicios.Log.Error("Login", ex);
                lblError.Text = Servicios.Log.Amigable(ex);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            if (u == null) { lblError.Text = siFalla; return; }
            if (soloAdmin && u.Rol != Roles.Admin) { lblError.Text = "Solo un administrador puede autorizar esto."; return; }

            Usuario = u;
            DialogResult = DialogResult.OK;
        }

        // pide entrar hasta que alguien con permiso lo haga o se cancele, y deja lista la sesion con su caja
        public static bool IniciarSesion(IWin32Window dueno)
        {
            while (true)
            {
                Usuario u;
                using (var f = new FrmLogin())
                {
                    if (f.ShowDialog(dueno) != DialogResult.OK) return false;
                    u = f.Usuario;
                }
                if (u.Rol == Roles.Cocina)
                {
                    MessageBox.Show(dueno, "La pantalla de cocina todavía no está lista, llega en la siguiente fase.",
                        "Cocina", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    continue;
                }

                int? turno = null;
                try
                {
                    var t = TurnoRepo.Abierto(u.Id);
                    if (t != null) turno = t.Id;
                }
                catch (Exception ex)
                {
                    Servicios.Log.Error("Turno al entrar", ex);
                }
                Servicios.Sesion.Iniciar(u, turno);
                Servicios.Log.Info("Entró " + u.Login);
                return true;
            }
        }

        // si quien esta usando el sistema ya es admin no se le pregunta nada
        public static int? Autorizar(IWin32Window dueno, string que)
        {
            if (Servicios.Sesion.EsAdmin) return Servicios.Sesion.Id;
            using (var f = new FrmLogin("Autorizar " + que, true))
                return f.ShowDialog(dueno) == DialogResult.OK ? f.Usuario.Id : (int?)null;
        }
    }
}
