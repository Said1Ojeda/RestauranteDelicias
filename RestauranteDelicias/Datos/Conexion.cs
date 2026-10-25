using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace RestauranteDelicias.Datos
{
    // la cadena viene del App.config, pero si alguien la cambia desde la pantalla de conexion
    // se guarda cifrada en AppData de esa compu, asi cada caja puede apuntar a su servidor
    public static class Conexion
    {
        static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestauranteDelicias");
        static readonly string ArchivoLocal = Path.Combine(Carpeta, "conexion.dat");

        static string cadena;

        public static string Cadena
        {
            get
            {
                if (cadena == null) cadena = Leer();
                return cadena;
            }
        }

        public static string Equipo
        {
            get { return Environment.MachineName; }
        }

        static string Leer()
        {
            try
            {
                if (File.Exists(ArchivoLocal))
                {
                    var bytes = ProtectedData.Unprotect(File.ReadAllBytes(ArchivoLocal), null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(bytes);
                }
            }
            catch (Exception ex)
            {
                // si el archivo se dano se usa el del App.config
                Servicios.Log.Error("No se pudo leer conexion.dat", ex);
            }

            var cs = ConfigurationManager.ConnectionStrings["Restaurante"];
            return cs != null ? cs.ConnectionString : @"Server=.\SQLEXPRESS;Database=RestauranteDelicias;Integrated Security=True";
        }

        public static void Guardar(string nueva)
        {
            Directory.CreateDirectory(Carpeta);
            var cifrado = ProtectedData.Protect(Encoding.UTF8.GetBytes(nueva), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(ArchivoLocal, cifrado);
            cadena = nueva;
        }

        public static SqlConnection Abrir()
        {
            return Abrir(Cadena);
        }

        public static SqlConnection Abrir(string cs)
        {
            var c = new SqlConnection(cs);
            c.Open();
            return c;
        }

        // 1205 es deadlock, con varias cajas puede pasar de vez en cuando y basta con repetir
        public static T ConReintento<T>(Func<T> accion)
        {
            for (int intento = 1; ; intento++)
            {
                try
                {
                    return accion();
                }
                catch (SqlException ex) when (ex.Number == 1205 && intento < 3)
                {
                    Thread.Sleep(100 * intento);
                }
            }
        }

        public static void ConReintento(Action accion)
        {
            ConReintento<bool>(() => { accion(); return true; });
        }

        public static string Describir(string cs)
        {
            try
            {
                var b = new SqlConnectionStringBuilder(cs);
                return b.DataSource + " / " + b.InitialCatalog;
            }
            catch
            {
                return "";
            }
        }
    }
}
