using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Pantallas;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // asi windows no estira la ventana como imagen y las letras salen nitidas con el escalado al 125 o 150
            try { if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware(); }
            catch (Exception) { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // si algo truena que salga el mensaje y quede en el log, sin cerrar todo con la mesa a medias
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                Log.Error("Sin atrapar", e.Exception);
                MessageBox.Show(Log.Amigable(e.Exception), "Algo salió mal", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log.Error("Fatal", e.ExceptionObject as Exception ?? new Exception("desconocido"));

            Log.Info("Inicio en " + Conexion.Equipo);

            string error;
            var estado = Instalador.Revisar(Conexion.Cadena, out error);

            // si la base es de una version anterior se actualiza al tiro, sin borrar nada
            if (estado == Instalador.Estado.Desactualizada)
            {
                var r = MessageBox.Show("Esta versión del sistema necesita actualizar la base de datos. Los datos se quedan igual.\n\n¿Actualizar ahora?",
                    "Restaurante Delicias", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
                try
                {
                    Instalador.Actualizar(Conexion.Cadena);
                    estado = Instalador.Estado.Lista;
                }
                catch (Exception ex)
                {
                    Log.Error("Actualizar base", ex);
                    MessageBox.Show(Log.Amigable(ex), "No se pudo actualizar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (estado != Instalador.Estado.Lista)
            {
                if (error != null) Log.Info("Sin conexion al iniciar: " + error);
                using (var f = new FrmConexion())
                    if (f.ShowDialog() != DialogResult.OK) return;
            }

            try
            {
                Configuracion.Cargar();

                // la primera vez no hay nadie, se crea el admin antes de pedir login
                if (!UsuarioRepo.HayAdmin())
                    using (var f = new FrmUsuario(null, true))
                        if (f.ShowDialog() != DialogResult.OK) return;
            }
            catch (Exception ex)
            {
                Log.Error("Arranque", ex);
                MessageBox.Show(Log.Amigable(ex), "Restaurante Delicias", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!FrmLogin.IniciarSesion(null)) return;

            Application.Run(new FrmPrincipal());
        }
    }
}
