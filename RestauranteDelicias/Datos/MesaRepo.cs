using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Datos
{
    public static class MesaRepo
    {
        // todas las mesas con su orden abierta, si tiene, en una sola ida a la base
        // esta consulta corre cada pocos segundos en cada caja, por eso es una sola
        public static List<MesaEstado> Estado()
        {
            using (var c = Conexion.Abrir())
                return c.Query<MesaEstado>(
                    @"SELECT m.Id, m.Numero, m.AreaId, a.Nombre AS AreaNombre, m.Capacidad, m.Forma,
                             o.Id AS OrdenId, o.Apertura, o.Personas,
                             ISNULL(t.Subtotal, 0) AS Subtotal,
                             ISNULL(t.Articulos, 0) AS Articulos,
                             ISNULL(t.Pendientes, 0) AS Pendientes
                      FROM dbo.Mesa m
                      JOIN dbo.Area a ON a.Id = m.AreaId
                      LEFT JOIN dbo.Orden o ON o.MesaId = m.Id AND o.Estado = N'Abierta'
                      OUTER APPLY (
                          SELECT SUM(d.Cantidad * d.PrecioUnitario) AS Subtotal,
                                 SUM(d.Cantidad) AS Articulos,
                                 SUM(CASE WHEN d.Estado = N'Nuevo' THEN 1 ELSE 0 END) AS Pendientes
                          FROM dbo.OrdenDetalle d
                          WHERE d.OrdenId = o.Id AND d.Anulado = 0
                      ) t
                      WHERE m.Activa = 1 AND a.Activa = 1
                      ORDER BY a.Orden, a.Nombre, m.Numero").ToList();
        }
    }
}
