using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    // lo que ve y marca la pantalla de cocina, cada compu de cocina pregunta cada pocos segundos
    public static class CocinaRepo
    {
        // estacion null trae cocina y barra juntas
        // solo lo de las ultimas 12 horas, asi un pedido olvidado no se queda para siempre en pantalla
        public static List<Comanda> Pendientes(string estacion)
        {
            List<LineaCocina> lineas;
            using (var c = Conexion.Abrir())
                lineas = c.Query<LineaCocina>(
                    @"SELECT d.Id, d.OrdenId, m.Numero AS MesaNumero, u.Nombre AS Mesero, d.Nombre, d.Cantidad, d.Nota,
                             d.Estado, d.Enviado, d.Anulado, cat.Estacion
                      FROM dbo.OrdenDetalle d
                      JOIN dbo.Orden o ON o.Id = d.OrdenId
                      JOIN dbo.Platillo p ON p.Id = d.PlatilloId
                      JOIN dbo.Categoria cat ON cat.Id = p.CategoriaId
                      LEFT JOIN dbo.Mesa m ON m.Id = o.MesaId
                      LEFT JOIN dbo.Usuario u ON u.Id = o.MeseroId
                      WHERE d.Estado IN (N'Enviado', N'Preparando')
                        AND d.Enviado IS NOT NULL
                        AND d.Enviado >= DATEADD(HOUR, -12, SYSDATETIME())
                        AND (@estacion IS NULL OR cat.Estacion = @estacion)
                      ORDER BY d.Enviado, d.OrdenId, d.Id", new { estacion }).ToList();
            return Comanda.Agrupar(lineas);
        }

        static void ExigirCocina()
        {
            Sesion.Exigir(Sesion.EsCocina || Sesion.EsAdmin);
        }

        public static void Empezar(IEnumerable<int> ids)
        {
            ExigirCocina();
            Conexion.ConReintento(() =>
            {
                using (var c = Conexion.Abrir())
                    c.Execute(
                        @"UPDATE dbo.OrdenDetalle SET Estado = N'Preparando', Preparando = SYSDATETIME()
                          WHERE Id IN @ids AND Estado = N'Enviado'", new { ids = ids.ToList() });
            });
        }

        // si otra pantalla ya lo marco no pasa nada, solo se cambia lo que sigue pendiente
        public static void Listo(IEnumerable<int> ids)
        {
            ExigirCocina();
            Conexion.ConReintento(() =>
            {
                using (var c = Conexion.Abrir())
                    c.Execute(
                        @"UPDATE dbo.OrdenDetalle SET Estado = N'Listo', Listo = SYSDATETIME()
                          WHERE Id IN @ids AND Estado IN (N'Enviado', N'Preparando')", new { ids = ids.ToList() });
            });
        }

        // por si se marco listo por error, regresa a preparando
        public static void Deshacer(IEnumerable<int> ids)
        {
            ExigirCocina();
            Conexion.ConReintento(() =>
            {
                using (var c = Conexion.Abrir())
                    c.Execute(
                        @"UPDATE dbo.OrdenDetalle SET Estado = N'Preparando', Listo = NULL
                          WHERE Id IN @ids AND Estado = N'Listo'", new { ids = ids.ToList() });
            });
        }

        // el mesero ya llevo a la mesa lo que estaba listo
        public static int Entregar(int ordenId)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            return Conexion.ConReintento(() =>
            {
                using (var c = Conexion.Abrir())
                    return c.Execute(
                        @"UPDATE dbo.OrdenDetalle SET Estado = N'Entregado', Entregado = SYSDATETIME()
                          WHERE OrdenId = @ordenId AND Estado = N'Listo' AND Anulado = 0", new { ordenId });
            });
        }
    }
}
