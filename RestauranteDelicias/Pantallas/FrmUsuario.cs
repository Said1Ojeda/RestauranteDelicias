using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    // crear o editar una persona, tambien sirve para el primer admin cuando la base esta recien creada
    public class FrmUsuario : Form
    {
        readonly Usuario existente;
        readonly TextBox txtNombre = new TextBox();
        readonly TextBox txtLogin = new TextBox();
        readonly ComboBox cboRol = new ComboBox();
        readonly CheckBox chkActivo = new CheckBox();
        readonly TextBox txtClave = new TextBox();
        readonly TextBox txtClave2 = new TextBox();
        readonly TextBox txtPin = new TextBox();

        public FrmUsuario(Usuario existente, bool primerAdmin = false)
        {
            this.existente = existente;
            var titulo = primerAdmin ? "Crear el administrador" : existente == null ? "Nueva persona" : "Editar " + existente.Nombre;
            Estilo.Formulario(this, titulo, 460, existente == null ? 470 : 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            if (primerAdmin) StartPosition = FormStartPosition.CenterScreen;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16), AutoScroll = true };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cboRol.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRol.Items.AddRange(Roles.Todos);
            txtClave.UseSystemPasswordChar = txtClave2.UseSystemPasswordChar = txtPin.UseSystemPasswordChar = true;
            txtPin.MaxLength = 6;
            txtNombre.MaxLength = 100;
            txtLogin.MaxLength = 40;
            chkActivo.Text = "Puede entrar al sistema";
            chkActivo.AutoSize = true;

            int fila = 0;
            if (primerAdmin)
            {
                var intro = new Label
                {
                    Text = "Es la primera vez que se usa el sistema. Crea el usuario administrador, con él se crean los demás.",
                    AutoSize = true,
                    MaximumSize = new System.Drawing.Size(400, 0),
                    Margin = new Padding(3, 0, 3, 12)
                };
                t.Controls.Add(intro, 0, fila);
                t.SetColumnSpan(intro, 2);
                fila++;
            }

            Fila(t, "Nombre", txtNombre, fila++);
            Fila(t, "Usuario", txtLogin, fila++);
            Fila(t, "Rol", cboRol, fila++);
            if (existente != null) Fila(t, "", chkActivo, fila++);
            if (existente == null)
            {
                Fila(t, "Contraseña", txtClave, fila++);
                Fila(t, "Repetir", txtClave2, fila++);
                Fila(t, "PIN (opcional)", txtPin, fila++);
                var ayuda = new Label { Text = "El PIN es para entrar rápido con el teclado numérico.", AutoSize = true, ForeColor = Estilo.Gris };
                t.Controls.Add(ayuda, 1, fila++);
            }

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(10), FlowDirection = FlowDirection.RightToLeft };
            var ok = Estilo.Boton("Guardar", Estilo.Vino);
            var cancelar = Estilo.Boton("Cancelar", Estilo.Gris);
            ok.Click += (s, e) => Guardar();
            cancelar.Click += (s, e) => DialogResult = DialogResult.Cancel;
            botones.Controls.AddRange(new Control[] { ok, cancelar });

            Controls.Add(t);
            Controls.Add(botones);
            AcceptButton = ok;
            CancelButton = cancelar;

            if (existente != null)
            {
                txtNombre.Text = existente.Nombre;
                txtLogin.Text = existente.Login;
                txtLogin.ReadOnly = true;
                cboRol.SelectedItem = existente.Rol;
                chkActivo.Checked = existente.Activo;
            }
            else cboRol.SelectedItem = primerAdmin ? Roles.Admin : Roles.Mesero;

            if (primerAdmin)
            {
                cboRol.Enabled = false;
                txtLogin.Text = "admin";
            }
        }

        static void Fila(TableLayoutPanel t, string texto, Control c, int fila)
        {
            c.Dock = DockStyle.Fill;
            c.Margin = new Padding(3, 6, 3, 6);
            t.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 10, 3, 3) }, 0, fila);
            t.Controls.Add(c, 1, fila);
        }

        void Guardar()
        {
            var nombre = txtNombre.Text.Trim();
            var login = txtLogin.Text.Trim();
            var rol = (string)cboRol.SelectedItem;

            if (nombre.Length < 3) { Avisar("Escribe el nombre completo."); return; }
            if (login.Length < 3 || login.Contains(" ")) { Avisar("El usuario debe tener al menos 3 letras y sin espacios."); return; }

            if (existente != null)
            {
                var u = new Usuario { Id = existente.Id, Nombre = nombre, Login = existente.Login, Rol = rol, Activo = chkActivo.Checked };
                if (Ui.Seguro(this, () => UsuarioRepo.Actualizar(u))) DialogResult = DialogResult.OK;
                return;
            }

            var error = Seguridad.ValidarClave(txtClave.Text);
            if (error != null) { Avisar(error); return; }
            if (txtClave.Text != txtClave2.Text) { Avisar("Las contraseñas no son iguales."); return; }
            var pin = txtPin.Text.Trim();
            if (pin.Length > 0)
            {
                error = Seguridad.ValidarPin(pin);
                if (error != null) { Avisar(error); return; }
            }

            if (Ui.Seguro(this, () => UsuarioRepo.Crear(nombre, login, rol, txtClave.Text, pin.Length > 0 ? pin : null)))
                DialogResult = DialogResult.OK;
        }

        void Avisar(string texto)
        {
            MessageBox.Show(this, texto, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
