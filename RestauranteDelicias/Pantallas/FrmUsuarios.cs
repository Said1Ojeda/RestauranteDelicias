using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Pantallas
{
    public class FrmUsuarios : Form
    {
        readonly DataGridView grid = new DataGridView();

        public FrmUsuarios()
        {
            Estilo.Formulario(this, "Usuarios", 820, 500);

            Estilo.Tabla(grid);
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.Columns.Add("nombre", "Nombre");
            grid.Columns.Add("login", "Usuario");
            grid.Columns.Add("rol", "Rol");
            grid.Columns.Add("pin", "PIN");
            grid.Columns.Add("acceso", "Último acceso");
            grid.Columns.Add("estado", "Estado");
            grid.CellDoubleClick += (s, e) => Editar();

            var barra = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(8) };
            var btnNuevo = Estilo.Boton("Nueva persona", Estilo.Libre);
            var btnEditar = Estilo.Boton("Editar", Estilo.Gris);
            var btnClave = Estilo.Boton("Cambiar contraseña", Estilo.Azul);
            var btnPin = Estilo.Boton("Poner PIN", Estilo.Azul);
            var btnQuitarPin = Estilo.Boton("Quitar PIN", Estilo.Gris);
            btnNuevo.Click += (s, e) => { using (var f = new FrmUsuario(null)) if (f.ShowDialog(this) == DialogResult.OK) Cargar(); };
            btnEditar.Click += (s, e) => Editar();
            btnClave.Click += (s, e) => CambiarClave();
            btnPin.Click += (s, e) => CambiarPin(true);
            btnQuitarPin.Click += (s, e) => CambiarPin(false);
            barra.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnClave, btnPin, btnQuitarPin });

            Controls.Add(grid);
            Controls.Add(barra);
            Controls.Add(Estilo.Encabezado("Usuarios"));
            Load += (s, e) => Cargar();
        }

        void Cargar()
        {
            List<Usuario> lista = null;
            if (!Ui.Seguro(this, () => lista = UsuarioRepo.Listar())) return;
            grid.Rows.Clear();
            foreach (var u in lista)
            {
                int i = grid.Rows.Add(u.Nombre, u.Login, u.Rol, u.TienePin ? "Sí" : "-",
                    u.UltimoAcceso.HasValue ? u.UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm") : "Nunca",
                    u.Activo ? "Activo" : "Desactivado");
                grid.Rows[i].Tag = u;
                if (!u.Activo) grid.Rows[i].DefaultCellStyle.ForeColor = Color.Silver;
            }
        }

        Usuario Seleccionado()
        {
            return grid.CurrentRow == null ? null : grid.CurrentRow.Tag as Usuario;
        }

        void Editar()
        {
            var u = Seleccionado();
            if (u == null) return;
            using (var f = new FrmUsuario(u))
                if (f.ShowDialog(this) == DialogResult.OK) Cargar();
        }

        void CambiarClave()
        {
            var u = Seleccionado();
            if (u == null) return;
            var clave = Microsoft.VisualBasic.Interaction.InputBox("Nueva contraseña para " + u.Nombre + " (mínimo 6 caracteres)", "Cambiar contraseña", "");
            if (clave.Length == 0) return;
            var error = Seguridad.ValidarClave(clave);
            if (error != null) { MessageBox.Show(this, error, "Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            if (Ui.Seguro(this, () => UsuarioRepo.CambiarClave(u.Id, clave)))
                MessageBox.Show(this, "Listo, la contraseña de " + u.Nombre + " quedó cambiada.", "Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void CambiarPin(bool poner)
        {
            var u = Seleccionado();
            if (u == null) return;
            string pin = null;
            if (poner)
            {
                pin = Microsoft.VisualBasic.Interaction.InputBox("PIN para " + u.Nombre + " (de 4 a 6 números)", "Poner PIN", "");
                if (pin.Length == 0) return;
                var error = Seguridad.ValidarPin(pin);
                if (error != null) { MessageBox.Show(this, error, "PIN", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            }
            else if (!Ui.Confirmar(this, "¿Quitarle el PIN a " + u.Nombre + "?", "Quitar PIN")) return;

            if (Ui.Seguro(this, () => UsuarioRepo.CambiarPin(u.Id, pin))) Cargar();
        }
    }
}
