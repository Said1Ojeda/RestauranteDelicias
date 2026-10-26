using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Datos
{
    public static class VentaRepo
    {
        // cajero null trae todas las ventas, si viene solo las que cobro ese usuario
        public static List<Venta> DelDia(DateTime dia, int? cajero)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Venta>(
                    @"SELECT o.Id, o.NumeroTicket, o.Cierre, m.Numero AS MesaNumero, o.Tipo,
                             (SELECT ISNULL(SUM(d.Cantidad), 0) FROM dbo.OrdenDetalle d
                              WHERE d.OrdenId = o.Id AND d.Anulado = 0) AS Articulos,
                             o.Subtotal, o.Propina, o.Total,
                             STUFF((SELECT N', ' + p.Forma FROM dbo.Pago p WHERE p.OrdenId = o.Id
                                    FOR XML PATH(''), TYPE).value('.', 'nvarchar(200)'), 1, 2, N'') AS Formas,
                             me.Nombre AS Mesero,
                             (SELECT TOP (1) u.Nombre FROM dbo.Pago p JOIN dbo.Usuario u ON u.Id = p.UsuarioId
                              WHERE p.OrdenId = o.Id ORDER BY p.Id) AS Cajero
                      FROM dbo.Orden o
                      LEFT JOIN dbo.Mesa m ON m.Id = o.MesaId
                      LEFT JOIN dbo.Usuario me ON me.Id = o.MeseroId
                      WHERE o.Estado = N'Cobrada' AND o.Cierre >= @desde AND o.Cierre < @hasta
                        AND (@cajero IS NULL OR EXISTS (SELECT 1 FROM dbo.Pago p WHERE p.OrdenId = o.Id AND p.UsuarioId = @cajero))
                      ORDER BY o.NumeroTicket",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1), cajero }).ToList();
        }

        // lo que entro por cada forma de pago, con la propina incluida
        public static List<TotalForma> PorForma(DateTime dia, int? cajero)
        {
            using (var c = Conexion.Abrir())
                return c.Query<TotalForma>(
                    @"SELECT p.Forma, SUM(p.Monto) AS Total, COUNT(*) AS Cantidad
                      FROM dbo.Pago p
                      JOIN dbo.Orden o ON o.Id = p.OrdenId
                      WHERE o.Estado = N'Cobrada' AND o.Cierre >= @desde AND o.Cierre < @hasta
                        AND (@cajero IS NULL OR p.UsuarioId = @cajero)
                      GROUP BY p.Forma
                      ORDER BY p.Forma",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1), cajero }).ToList();
        }

        public static int Anuladas(DateTime dia, int? cajero)
        {
            using (var c = Conexion.Abrir())
                return c.ExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM dbo.OrdenDetalle d JOIN dbo.Orden o ON o.Id = d.OrdenId
                      WHERE d.Anulado = 1 AND o.Apertura >= @desde AND o.Apertura < @hasta
                        AND (@cajero IS NULL OR d.AnuladoPor = @cajero)",
                    new { desde = dia.Date, hasta = dia.Date.AddDays(1), cajero });
        }
    
        public static List<Usuario> Cajeros()
        {
            using (var c = Conexion.Abrir())
                return c.Query<Usuario>(
                    @"SELECT Id, Nombre, Usuario AS Login, Rol, Activo FROM dbo.Usuario
                      WHERE Rol IN (N'Admin', N'Cajero') ORDER BY Activo DESC, Nombre").ToList();
        }
    }
}
