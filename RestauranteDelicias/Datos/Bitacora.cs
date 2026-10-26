using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    public static class Bitacora
    {
        public static void Registrar(IDbConnection c, IDbTransaction tx, string accion, string detalle)
        {
            if (detalle != null && detalle.Length > 400) detalle = detalle.Substring(0, 400);
            c.Execute(
                @"INSERT INTO dbo.Bitacora (UsuarioId, Equipo, Accion, Detalle) VALUES (@usuario, @equipo, @accion, @detalle)",
                new { usuario = Sesion.Id, equipo = Conexion.Equipo, accion, detalle }, tx);
        }

        public static void Registrar(string accion, string detalle)
        {
            using (var c = Conexion.Abrir())
                Registrar(c, null, accion, detalle);
        }

        // un % o un _ escrito en el buscador se busca tal cual, no como comodin
        static string ComoTextoLiteral(string texto)
        {
            return texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
        }

        public static List<EntradaBitacora> Listar(DateTime desde, DateTime hasta, string texto)
        {
            texto = (texto ?? "").Trim();
            using (var c = Conexion.Abrir())
                return c.Query<EntradaBitacora>(
                    @"SELECT TOP (1000) b.Id, b.Fecha, ISNULL(u.Nombre, N'-') AS Usuario, b.Equipo, b.Accion, b.Detalle
                      FROM dbo.Bitacora b
                      LEFT JOIN dbo.Usuario u ON u.Id = b.UsuarioId
                      WHERE b.Fecha >= @desde AND b.Fecha < @hasta
                        AND (@texto = N'' OR b.Accion LIKE @patron ESCAPE N'\' OR b.Detalle LIKE @patron ESCAPE N'\' OR u.Nombre LIKE @patron ESCAPE N'\')
                      ORDER BY b.Fecha DESC, b.Id DESC",
                    new { desde = desde.Date, hasta = hasta.Date.AddDays(1), texto, patron = "%" + ComoTextoLiteral(texto) + "%" }).ToList();
        }
    }
}
