using System;
using System.Data.SqlClient;
using System.IO;
using Dapper;

namespace RestauranteDelicias.Datos
{
    // respaldo completo de la base con BACKUP DATABASE, sql express no lo hace solo
    public static class RespaldoRepo
    {
        // la carpeta es del servidor donde esta sql server, no de la compu que lo pide
        public static string CarpetaSugerida()
        {
            var guardada = Configuracion.Texto("Respaldo.Carpeta");
            if (!string.IsNullOrWhiteSpace(guardada)) return guardada;
            try
            {
                using (var c = Conexion.Abrir())
                {
                    var porDefecto = c.ExecuteScalar<string>("SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(400))");
                    if (!string.IsNullOrWhiteSpace(porDefecto)) return porDefecto;
                }
            }
            catch (Exception ex)
            {
                Servicios.Log.Error("Carpeta de respaldo", ex);
            }
            return @"C:\Respaldos\RestauranteDelicias";
        }

        public static string Hacer(string carpeta)
        {
            carpeta = (carpeta ?? "").Trim().TrimEnd('\\');
            if (carpeta.Length == 0) throw new AvisoException("Escribe la carpeta donde se va a guardar el respaldo.");

            string archivo;
            using (var c = Conexion.Abrir())
            {
                var nombre = c.ExecuteScalar<string>("SELECT DB_NAME()");
                archivo = Path.Combine(carpeta, nombre + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".bak");

                // si la carpeta no existe se intenta crear, necesita permiso de administrador en sql server
                try { c.Execute("EXEC master.dbo.xp_create_subdir @carpeta", new { carpeta }); }
                catch (SqlException ex) { Servicios.Log.Info("No se pudo crear la carpeta de respaldo " + ex.Message); }

                try
                {
                    c.Execute(
                        @"DECLARE @base SYSNAME = DB_NAME();
                          BACKUP DATABASE @base TO DISK = @archivo WITH INIT, CHECKSUM, NAME = N'Restaurante Delicias'",
                        new { archivo }, commandTimeout: 900);
                }
                catch (SqlException ex) when (ex.Number == 3201 || ex.Number == 3013)
                {
                    throw new AvisoException("SQL Server no pudo escribir en " + carpeta +
                        ". Revisa que la carpeta exista en la compu del servidor y que el servicio de SQL Server tenga permiso de escribir ahí.");
                }
            }

            Configuracion.Guardar(new System.Collections.Generic.Dictionary<string, string>
            {
                { "Respaldo.Carpeta", carpeta },
                { "Respaldo.Ultimo", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                { "Respaldo.Archivo", archivo }
            });
            return archivo;
        }

        public static DateTime? Ultimo
        {
            get
            {
                DateTime d;
                return DateTime.TryParseExact(Configuracion.Texto("Respaldo.Ultimo"), "yyyy-MM-dd HH:mm:ss",
                    System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out d) ? d : (DateTime?)null;
            }
        }
    }
}
