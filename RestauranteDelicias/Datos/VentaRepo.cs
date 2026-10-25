using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Datos
{
    public static class VentaRepo
    {
        public static List<Venta> DelDia(DateTime dia)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Venta>(
                    @"SELECT o.Id, o.NumeroTicket, o.Cierre, m.Numero AS MesaNumero, o.Tipo,
                             (SELECT ISNULL(SUM(d.Cantidad), 0) FROM dbo.OrdenDetalle d
                              WHERE d.OrdenId = o.Id AND d.Anulado = 0) AS Articulos,
                             o.Subtotal, o.Propina, o.Total,
                             STUFF((SELECT N', ' + p.Forma FROM dbo.Pago p WHERE p.OrdenId = o.Id
                                    FOR XML PATH(''), TYPE).value('.', 'nvarchar(200)'), 1, 2, N'') AS Formas
                      FROM dbo.Orden o
                      LEFT JOIN dbo.Mesa m ON m.Id = o.MesaId
                      WHERE o.Estado = N'Cobrada' AND o.Cierre >= @desde AND o.Cierre < @hasta
                      ORDER BY o.NumeroTicket",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1) }).ToList();
        }

        // lo que entro por cada forma de pago, con la propina incluida
        public static List<TotalForma> PorForma(DateTime dia)
        {
            using (var c = Conexion.Abrir())
                return c.Query<TotalForma>(
                    @"SELECT p.Forma, SUM(p.Monto) AS Total, COUNT(*) AS Cantidad
                      FROM dbo.Pago p
                      JOIN dbo.Orden o ON o.Id = p.OrdenId
                      WHERE o.Estado = N'Cobrada' AND o.Cierre >= @desde AND o.Cierre < @hasta
                      GROUP BY p.Forma
                      ORDER BY p.Forma",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1) }).ToList();
        }

        public static int Anuladas(DateTime dia)
        {
            using (var c = Conexion.Abrir())
                return c.ExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM dbo.OrdenDetalle d JOIN dbo.Orden o ON o.Id = d.OrdenId
                      WHERE d.Anulado = 1 AND o.Apertura >= @desde AND o.Apertura < @hasta",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1) });
        }
    }
}
