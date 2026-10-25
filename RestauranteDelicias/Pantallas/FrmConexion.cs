using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Datos;

namespace RestauranteDelicias.Pantallas
{
    // sale al arrancar si no hay conexion, y desde el boton de conexion en la pantalla principal
    public class FrmConexion : Form
    {
        readonly TextBox txtServidor = new TextBox();
        readonly TextBox txtBase = new TextBox();
        readonly ComboBox cboAuth = new ComboBox();
        readonly TextBox txtUsuario = new TextBox();
        readonly TextBox txtClave = new TextBox();
        readonly Label lblEstado = new Label();
        readonly Button btnInstalar;
        readonly Button btnEntrar;

        public FrmConexion()
        {
            Estilo.Formulario(this, "Conexión a la base de datos", 520, 430);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(18) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cboAuth.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAuth.Items.AddRange(new object[] { "Windows", "Usuario de SQL Server" });
            cboAuth.SelectedIndexChanged += (s, e) => { txtUsuario.Enabled = txtClave.Enabled = cboAuth.SelectedIndex == 1; };
            txtClave.UseSystemPasswordChar = true;
            foreach (var c in new Control[] { txtServidor, txtBase, cboAuth, txtUsuario, txtClave })
            {
                c.Dock = DockStyle.Fill;
                c.Margin = new Padding(3, 6, 3, 6);
            }

            Fila(t, "Servidor", txtServidor, 0);
            Fila(t, "Base de datos", txtBase, 1);
            Fila(t, "Autenticación", cboAuth, 2);
            Fila(t, "Usuario", txtUsuario, 3);
            Fila(t, "Contraseña", txtClave, 4);

            var ayuda = new Label
            {
                Text = "En la compu donde está SQL Server Express normalmente es .\\SQLEXPRESS\n" +
                       "Desde otra compu de la red es NOMBRE-PC\\SQLEXPRESS",
                AutoSize = true,
                ForeColor = Estilo.Gris,
                Margin = new Padding(3, 4, 3, 10)
            };
            t.Controls.Add(ayuda, 1, 5);

            lblEstado.AutoSize = false;
            lblEstado.Dock = DockStyle.Fill;
            lblEstado.Height = 60;
            lblEstado.Font = Estilo.Negrita;
            t.Controls.Add(lblEstado, 0, 6);
            t.SetColumnSpan(lblEstado, 2);

            var botones = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
            btnEntrar = Estilo.Boton("Guardar y entrar", Estilo.Vino);
            btnInstalar = Estilo.Boton("Crear base de datos", Estilo.Azul);
            var btnProbar = Estilo.Boton("Probar", Estilo.Gris);
            btnEntrar.Click += (s, e) => Entrar();
            btnInstalar.Click += (s, e) => InstalarBase();
            btnProbar.Click += (s, e) => Probar();
            botones.Controls.AddRange(new Control[] { btnEntrar, btnInstalar, btnProbar });

            Controls.Add(t);
            Controls.Add(botones);

            CargarActual();
            Load += (s, e) => Probar();
        }

        static void Fila(TableLayoutPanel t, string texto, Control c, int fila)
        {
            t.Controls.Add(new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 10, 3, 3) }, 0, fila);
            t.Controls.Add(c, 1, fila);
        }

        void CargarActual()
        {
            var b = new SqlConnectionStringBuilder(Conexion.Cadena);
            txtServidor.Text = string.IsNullOrEmpty(b.DataSource) ? @".\SQLEXPRESS" : b.DataSource;
            txtBase.Text = string.IsNullOrEmpty(b.InitialCatalog) ? "RestauranteDelicias" : b.InitialCatalog;
            cboAuth.SelectedIndex = b.IntegratedSecurity ? 0 : 1;
            txtUsuario.Text = b.UserID;
            txtClave.Text = b.Password;
        }

        string Armar()
        {
            var b = new SqlConnectionStringBuilder
            {
                DataSource = txtServidor.Text.Trim(),
                InitialCatalog = txtBase.Text.Trim(),
                ConnectTimeout = 5,
                ApplicationName = "RestauranteDelicias"
            };
            if (cboAuth.SelectedIndex == 0) b.IntegratedSecurity = true;
            else
            {
                b.UserID = txtUsuario.Text.Trim();
                b.Password = txtClave.Text;
            }
            return b.ConnectionString;
        }

        Instalador.Estado Probar()
        {
            string error = null;
            var estado = Instalador.Estado.SinConexion;
            Cursor = Cursors.WaitCursor;
            try { estado = Instalador.Revisar(Armar(), out error); }
            catch (Exception ex) { error = ex.Message; }
            finally { Cursor = Cursors.Default; }

            switch (estado)
            {
                case Instalador.Estado.Lista:
                    Mostrar("Conectado. La base está lista.", Estilo.Libre);
                    break;
                case Instalador.Estado.SinBase:
                    Mostrar("Se conectó al servidor pero la base no existe todavía. Dale a \"Crear base de datos\".", Estilo.Naranja);
                    break;
                case Instalador.Estado.Desactualizada:
                    Mostrar("La base es de una versión anterior del sistema. Dale a \"Actualizar base de datos\", no se borra nada.", Estilo.Naranja);
                    break;
                case Instalador.Estado.SinTablas:
                    Mostrar("La base existe pero está vacía. Dale a \"Crear base de datos\" para crear las tablas.", Estilo.Naranja);
                    break;
                default:
                    Mostrar("No se pudo conectar. " + error, Estilo.Ocupada);
                    break;
            }
            btnInstalar.Enabled = estado == Instalador.Estado.SinBase || estado == Instalador.Estado.SinTablas || estado == Instalador.Estado.Desactualizada;
            btnInstalar.Text = estado == Instalador.Estado.Desactualizada ? "Actualizar base de datos" : "Crear base de datos";
            btnEntrar.Enabled = estado == Instalador.Estado.Lista;
            return estado;
        }

        void InstalarBase()
        {
            var texto = btnInstalar.Text.StartsWith("Actualizar")
                ? "Se van a agregar a la base las tablas y columnas nuevas. Los datos que ya tiene se quedan igual.\n\n¿Seguir?"
                : "Se va a crear la base " + txtBase.Text.Trim() + " con las tablas y un menú de ejemplo.\n\n¿Seguir?";
            if (!Ui.Confirmar(this, texto, btnInstalar.Text))
                return;
            if (Ui.Seguro(this, () => Instalador.Instalar(Armar())))
                Probar();
        }

        void Entrar()
        {
            if (Probar() != Instalador.Estado.Lista) return;
            Ui.Seguro(this, () =>
            {
                Conexion.Guardar(Armar());
                DialogResult = DialogResult.OK;
            });
        }

        void Mostrar(string texto, Color color)
        {
            lblEstado.Text = texto;
            lblEstado.ForeColor = color;
        }
    }
}
