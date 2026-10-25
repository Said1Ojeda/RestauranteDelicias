using System.Data;
using Dapper;

namespace RestauranteDelicias.Datos
{
    public static class Bitacora
    {
        public static void Registrar(IDbConnection c, IDbTransaction tx, string accion, string detalle)
        {
            if (detalle != null && detalle.Length > 400) detalle = detalle.Substring(0, 400);
            c.Execute(
                @"INSERT INTO dbo.Bitacora (Equipo, Accion, Detalle) VALUES (@equipo, @accion, @detalle)",
                new { equipo = Conexion.Equipo, accion, detalle }, tx);
        }
    }
}
