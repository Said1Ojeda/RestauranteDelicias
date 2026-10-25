using System;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace RestauranteDelicias.Servicios
{
    // un archivo por dia en AppData\Local\RestauranteDelicias\logs, para que nada falle en silencio
    public static class Log
    {
        static readonly object candado = new object();
        static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestauranteDelicias", "logs");

        public static void Error(string donde, Exception ex)
        {
            Escribir("ERROR", donde + Environment.NewLine + ex);
        }

        public static void Info(string texto)
        {
            Escribir("INFO", texto);
        }

        static void Escribir(string tipo, string texto)
        {
            try
            {
                lock (candado)
                {
                    Directory.CreateDirectory(Carpeta);
                    var archivo = Path.Combine(Carpeta, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                    File.AppendAllText(archivo,
                        DateTime.Now.ToString("HH:mm:ss") + " [" + tipo + "] " + texto + Environment.NewLine + Environment.NewLine,
                        Encoding.UTF8);
                }
            }
            catch
            {
                // si ni el log se puede escribir no hay mucho que hacer
            }
        }

        // mensaje entendible para el que esta en caja, el detalle tecnico se va al log
        public static string Amigable(Exception ex)
        {
            if (ex is AvisoException) return ex.Message;

            var sql = ex as SqlException ?? ex.InnerException as SqlException;
            if (sql != null)
            {
                switch (sql.Number)
                {
                    case -2: return "La base de datos tardó demasiado en responder. Intenta de nuevo.";
                    case -1:
                    case 2:
                    case 53:
                    case 4060:
                        return "No se pudo conectar con la base de datos. Revisa que el servidor esté encendido y en la red.";
                    case 18456: return "El usuario o la contraseña de SQL Server no son correctos.";
                    case 1205: return "Otra caja estaba guardando lo mismo. Intenta de nuevo.";
                }
                return "Error de base de datos (" + sql.Number + "). Quedó anotado en el registro.";
            }
            return "Algo salió mal: " + ex.Message;
        }
    }
}
