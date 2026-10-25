using System;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Pantallas;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
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
            if (Instalador.Revisar(Conexion.Cadena, out error) != Instalador.Estado.Lista)
            {
                if (error != null) Log.Info("Sin conexion al iniciar: " + error);
                using (var f = new FrmConexion())
                    if (f.ShowDialog() != DialogResult.OK) return;
            }

            try
            {
                Configuracion.Cargar();
            }
            catch (Exception ex)
            {
                Log.Error("Cargar configuracion", ex);
                MessageBox.Show(Log.Amigable(ex), "Restaurante Delicias", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Application.Run(new FrmPrincipal());
        }
    }
}
