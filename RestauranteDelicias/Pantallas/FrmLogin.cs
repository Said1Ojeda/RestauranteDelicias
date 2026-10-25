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
        readonly TextBox txtUsuario = new TextBox();
        readonly TextBox txtClave = new TextBox();
        readonly TextBox txtPin = new TextBox();
        readonly Label lblError = new Label();
        readonly Panel panelClave = new Panel();
        readonly Panel panelPin = new Panel();
        readonly bool soloAdmin;

        public Usuario Usuario { get; private set; }

        public FrmLogin() : this("Entrar", false) { }

        // tambien se usa para pedir la autorizacion de un admin
        public FrmLogin(string titulo, bool soloAdmin)
        {
            this.soloAdmin = soloAdmin;
            Estilo.Formulario(this, titulo, 420, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var cabeza = Estilo.Encabezado(soloAdmin ? titulo : Configuracion.Nombre);

            var modos = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(14, 10, 14, 0) };
            var btnModoPin = Estilo.Boton("Con PIN", Estilo.Vino);
            var btnModoClave = Estilo.Boton("Con usuario", Estilo.Gris);
            btnModoPin.Click += (s, e) => Modo(true, btnModoPin, btnModoClave);
            btnModoClave.Click += (s, e) => Modo(false, btnModoPin, btnModoClave);
            modos.Controls.AddRange(new Control[] { btnModoPin, btnModoClave });

            ArmarPin();
            ArmarClave();

            lblError.Dock = DockStyle.Bottom;
            lblError.Height = 40;
            lblError.ForeColor = Estilo.Ocupada;
            lblError.TextAlign = ContentAlignment.MiddleCenter;
            lblError.Font = Estilo.Negrita;

            Controls.Add(panelPin);
            Controls.Add(panelClave);
            Controls.Add(lblError);
            Controls.Add(modos);
            Controls.Add(cabeza);

            Modo(true, btnModoPin, btnModoClave);
        }

        void ArmarPin()
        {
            panelPin.Dock = DockStyle.Fill;
            panelPin.Padding = new Padding(40, 10, 40, 10);

            txtPin.UseSystemPasswordChar = true;
            txtPin.MaxLength = 6;
            txtPin.Font = new Font("Segoe UI", 22f, FontStyle.Bold);
            txtPin.TextAlign = HorizontalAlignment.Center;
            txtPin.Dock = DockStyle.Top;
            txtPin.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') e.Handled = true; };
            txtPin.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) EntrarPin(); };

            var teclado = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 4, Padding = new Padding(0, 12, 0, 0) };
            for (int i = 0; i < 3; i++) teclado.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
            for (int i = 0; i < 4; i++) teclado.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var teclas = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "OK" };
            for (int i = 0; i < teclas.Length; i++)
            {
                var t = teclas[i];
                var b = new Button
                {
                    Text = t,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(4),
                    Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = t == "OK" ? Estilo.Vino : Color.White,
                    ForeColor = t == "OK" ? Color.White : Color.Black,
                    TabStop = false
                };
                b.FlatAppearance.BorderColor = Color.Gainsboro;
                b.Click += (s, e) => Tecla(t);
                teclado.Controls.Add(b, i % 3, i / 3);
            }

            panelPin.Controls.Add(teclado);
            panelPin.Controls.Add(txtPin);
        }

        void ArmarClave()
        {
            panelClave.Dock = DockStyle.Fill;
            panelClave.Padding = new Padding(40, 20, 40, 10);

            var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
            txtUsuario.Dock = txtClave.Dock = DockStyle.Fill;
            txtUsuario.Font = txtClave.Font = new Font("Segoe UI", 13f);
            txtClave.UseSystemPasswordChar = true;
            txtClave.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) EntrarClave(); };
            txtUsuario.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) txtClave.Focus(); };

            var btn = Estilo.Boton("Entrar", Estilo.Vino);
            btn.Anchor = AnchorStyles.Right;
            btn.Click += (s, e) => EntrarClave();

            t.Controls.Add(new Label { Text = "Usuario", AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
            t.Controls.Add(txtUsuario);
            t.Controls.Add(new Label { Text = "Contraseña", AutoSize = true, Margin = new Padding(0, 14, 0, 2) });
            t.Controls.Add(txtClave);
            t.Controls.Add(btn);
            panelClave.Controls.Add(t);
        }

        void Modo(bool pin, Button bPin, Button bClave)
        {
            panelPin.Visible = pin;
            panelClave.Visible = !pin;
            bPin.BackColor = pin ? Estilo.Vino : Estilo.Gris;
            bClave.BackColor = pin ? Estilo.Gris : Estilo.Vino;
            lblError.Text = "";
            BeginInvoke(new Action(() => (pin ? (Control)txtPin : txtUsuario).Focus()));
        }

        void Tecla(string t)
        {
            if (t == "OK") EntrarPin();
            else if (t == "⌫") { if (txtPin.Text.Length > 0) txtPin.Text = txtPin.Text.Substring(0, txtPin.Text.Length - 1); }
            else if (txtPin.Text.Length < 6) txtPin.Text += t;
            txtPin.Focus();
            txtPin.SelectionStart = txtPin.Text.Length;
        }

        void EntrarPin()
        {
            var pin = txtPin.Text;
            txtPin.Clear();
            if (pin.Length < 4) { lblError.Text = "El PIN tiene de 4 a 6 números."; return; }
            Terminar(() => UsuarioRepo.EntrarConPin(pin), "PIN incorrecto.");
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
