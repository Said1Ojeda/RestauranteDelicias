using System.Data.SqlClient;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    // cada cajero abre su caja con un fondo y al cerrar cuenta el efectivo
    public static class TurnoRepo
    {
        const string CAMPOS = @"t.Id, t.UsuarioId, u.Nombre AS Cajero, t.Apertura, t.Cierre, t.MontoInicial, t.EfectivoContado, t.Esperado";

        public static Turno Abierto(int usuarioId)
        {
            using (var c = Conexion.Abrir())
                return c.QuerySingleOrDefault<Turno>(
                    "SELECT " + CAMPOS + " FROM dbo.Turno t JOIN dbo.Usuario u ON u.Id = t.UsuarioId WHERE t.UsuarioId = @usuarioId AND t.Cierre IS NULL",
                    new { usuarioId });
        }

        public static Turno Obtener(int id)
        {
            using (var c = Conexion.Abrir())
                return c.QuerySingleOrDefault<Turno>(
                    "SELECT " + CAMPOS + " FROM dbo.Turno t JOIN dbo.Usuario u ON u.Id = t.UsuarioId WHERE t.Id = @id", new { id });
        }

        public static int Abrir(int usuarioId, decimal montoInicial)
        {
            try
            {
                using (var c = Conexion.Abrir())
                using (var tx = c.BeginTransaction())
                {
                    var id = c.ExecuteScalar<int>(
                        "INSERT INTO dbo.Turno (UsuarioId, MontoInicial) OUTPUT INSERTED.Id VALUES (@usuarioId, @montoInicial)",
                        new { usuarioId, montoInicial }, tx);
                    Bitacora.Registrar(c, tx, "Apertura de caja", "Fondo inicial " + Ticket.Q(montoInicial));
                    tx.Commit();
                    return id;
                }
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new AvisoException("Ya tienes una caja abierta, quizás desde otra compu.");
            }
        }

        public static ResumenTurno Resumen(int turnoId)
        {
            using (var c = Conexion.Abrir())
                return c.QuerySingle<ResumenTurno>(
                    @"SELECT t.MontoInicial,
                             ISNULL(SUM(CASE WHEN p.Forma = N'Efectivo' THEN p.Monto END), 0) AS Efectivo,
                             ISNULL(SUM(CASE WHEN p.Forma = N'Tarjeta' THEN p.Monto END), 0) AS Tarjeta,
                             ISNULL(SUM(CASE WHEN p.Forma = N'Transferencia' THEN p.Monto END), 0) AS Transferencia,
                             (SELECT ISNULL(SUM(o.Propina), 0) FROM dbo.Orden o
                              WHERE o.Estado = N'Cobrada' AND o.Id IN (SELECT OrdenId FROM dbo.Pago WHERE TurnoId = t.Id)) AS Propinas,
                             COUNT(DISTINCT p.OrdenId) AS Ventas
                      FROM dbo.Turno t
                      LEFT JOIN dbo.Pago p ON p.TurnoId = t.Id
                      WHERE t.Id = @turnoId
                      GROUP BY t.Id, t.MontoInicial",
                    new { turnoId });
        }

        public static void Cerrar(int turnoId, decimal contado, string notas)
        {
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                var esperado = c.ExecuteScalar<decimal>(
                    @"SELECT t.MontoInicial + ISNULL((SELECT SUM(Monto) FROM dbo.Pago WHERE TurnoId = t.Id AND Forma = N'Efectivo'), 0)
                      FROM dbo.Turno t WITH (UPDLOCK) WHERE t.Id = @turnoId AND t.Cierre IS NULL",
                    new { turnoId }, tx);

                var filas = c.Execute(
                    @"UPDATE dbo.Turno
                      SET Cierre = SYSDATETIME(), EfectivoContado = @contado, Esperado = @esperado, Notas = @notas, CerradoPor = @usuario
                      WHERE Id = @turnoId AND Cierre IS NULL",
                    new { turnoId, contado, esperado, notas, usuario = Sesion.Id }, tx);
                if (filas == 0) throw new AvisoException("Esta caja ya estaba cerrada.");

                Bitacora.Registrar(c, tx, "Cierre de caja",
                    "Esperado " + Ticket.Q(esperado) + ", contado " + Ticket.Q(contado) + ", diferencia " + Ticket.Q(contado - esperado));
                tx.Commit();
            }
        }
    }
}
