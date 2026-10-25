using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;

namespace RestauranteDelicias.Datos
{
    // crea la base y las tablas desde el programa, para no tener que abrir SSMS en cada compu
    public static class Instalador
    {
        public enum Estado { SinConexion, SinBase, SinTablas, Lista }

        public static Estado Revisar(string cs, out string error)
        {
            error = null;
            try
            {
                var b = new SqlConnectionStringBuilder(cs);
                var nombre = b.InitialCatalog;
                b.InitialCatalog = "master";
                using (var c = Conexion.Abrir(b.ConnectionString))
                {
                    if (c.ExecuteScalar<int?>("SELECT DB_ID(@nombre)", new { nombre }) == null)
                        return Estado.SinBase;
                }
                using (var c = Conexion.Abrir(cs))
                {
                    var hay = c.ExecuteScalar<int?>("SELECT OBJECT_ID(N'dbo.Orden', N'U')");
                    return hay == null ? Estado.SinTablas : Estado.Lista;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return Estado.SinConexion;
            }
        }

        public static void Instalar(string cs)
        {
            var b = new SqlConnectionStringBuilder(cs);
            var nombre = b.InitialCatalog;

            // el nombre va pegado al CREATE DATABASE, por eso solo se aceptan letras numeros y guion bajo
            if (!Regex.IsMatch(nombre ?? "", @"^[A-Za-z0-9_]{1,100}$"))
                throw new AvisoException("El nombre de la base solo puede tener letras, números y guion bajo.");

            b.InitialCatalog = "master";
            using (var c = Conexion.Abrir(b.ConnectionString))
            {
                if (c.ExecuteScalar<int?>("SELECT DB_ID(@nombre)", new { nombre }) == null)
                {
                    c.Execute("CREATE DATABASE [" + nombre + "]", commandTimeout: 120);
                    c.Execute("ALTER DATABASE [" + nombre + "] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE", commandTimeout: 120);
                    c.Execute("ALTER DATABASE [" + nombre + "] SET ALLOW_SNAPSHOT_ISOLATION ON", commandTimeout: 120);
                }
            }

            using (var c = Conexion.Abrir(cs))
            {
                if (c.ExecuteScalar<int?>("SELECT OBJECT_ID(N'dbo.Orden', N'U')") != null) return;

                // todo en una transaccion, si algo falla la base queda vacia y se puede volver a intentar
                using (var tx = c.BeginTransaction())
                {
                    foreach (var archivo in new[] { "02_esquema.sql", "03_datos_iniciales.sql" })
                        foreach (var lote in Lotes(LeerScript(archivo)))
                            c.Execute(lote, transaction: tx, commandTimeout: 120);
                    tx.Commit();
                }
            }
        }

        static string LeerScript(string archivo)
        {
            var ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BaseDatos", archivo);
            if (!File.Exists(ruta))
                throw new AvisoException("No se encontró " + archivo + " junto al programa. Revisa que la carpeta BaseDatos se haya copiado.");
            return File.ReadAllText(ruta);
        }

        // SSMS separa con GO pero sql server no lo entiende, hay que mandar cada parte por aparte
        static string[] Lotes(string script)
        {
            return Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
        }
    }
}
