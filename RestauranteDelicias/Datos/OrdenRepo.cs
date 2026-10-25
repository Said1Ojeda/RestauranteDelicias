using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    // todo lo de una orden se guarda al momento, asi otra caja ve lo mismo y nada se pierde si se va la luz
    public static class OrdenRepo
    {
        const string CAMPOS_ORDEN =
            @"o.Id, o.Tipo, o.MesaId, m.Numero AS MesaNumero, o.Estado, o.Personas, o.ConPropina,
              o.PorcentajePropina, o.Descuento, o.DescuentoMotivo, o.Apertura, o.Cierre, o.NumeroTicket, o.Version,
              u.Nombre AS Mesero";

        const string CAMPOS_LINEA =
            @"Id, OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, Estado, Anulado, MotivoAnulacion";

        public static Orden Cargar(int ordenId)
        {
            using (var c = Conexion.Abrir())
            using (var r = c.QueryMultiple(
                "SELECT " + CAMPOS_ORDEN + @" FROM dbo.Orden o LEFT JOIN dbo.Mesa m ON m.Id = o.MesaId
                  LEFT JOIN dbo.Usuario u ON u.Id = o.MeseroId WHERE o.Id = @ordenId;
                  SELECT " + CAMPOS_LINEA + " FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId ORDER BY Id;",
                new { ordenId }))
            {
                var o = r.ReadSingleOrDefault<Orden>();
                if (o != null) o.Lineas = r.Read<LineaOrden>().ToList();
                return o;
            }
        }

        public static Orden AbiertaDeMesa(int mesaId)
        {
            int? id;
            using (var c = Conexion.Abrir())
                id = c.ExecuteScalar<int?>("SELECT Id FROM dbo.Orden WHERE MesaId = @mesaId AND Estado = N'Abierta'", new { mesaId });
            return id.HasValue ? Cargar(id.Value) : null;
        }

        // si dos meseros abren la misma mesa al mismo tiempo los dos terminan en la misma orden
        public static int ObtenerOCrear(int mesaId)
        {
            return Conexion.ConReintento(() =>
            {
                try
                {
                    using (var c = Conexion.Abrir())
                    using (var tx = c.BeginTransaction())
                    {
                        var id = c.ExecuteScalar<int?>(
                            "SELECT Id FROM dbo.Orden WITH (UPDLOCK, HOLDLOCK) WHERE MesaId = @mesaId AND Estado = N'Abierta'",
                            new { mesaId }, tx);
                        if (!id.HasValue)
                        {
                            id = c.ExecuteScalar<int>(
                                @"INSERT INTO dbo.Orden (Tipo, MesaId, MeseroId, PorcentajePropina, Equipo)
                                  OUTPUT INSERTED.Id
                                  VALUES (N'Mesa', @mesaId, @mesero, @propina, @equipo)",
                                new { mesaId, mesero = Sesion.Id, propina = Configuracion.Propina, equipo = Conexion.Equipo }, tx);
                        }
                        tx.Commit();
                        return id.Value;
                    }
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    // el indice unico atajo a otro que la abrio primero, se usa esa
                    using (var c = Conexion.Abrir())
                        return c.ExecuteScalar<int>("SELECT Id FROM dbo.Orden WHERE MesaId = @mesaId AND Estado = N'Abierta'", new { mesaId });
                }
            });
        }

        public static void Agregar(int ordenId, int platilloId, int cantidad, string nota)
        {
            nota = (nota ?? "").Trim();
            if (nota.Length > 150) nota = nota.Substring(0, 150);

            Ejecutar(c => c.Execute(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.Orden WHERE Id = @ordenId AND Estado = N'Abierta')
                      THROW 50001, N'Esta orden ya fue cobrada o anulada en otra caja.', 1;

                  IF NOT EXISTS (SELECT 1 FROM dbo.Platillo WHERE Id = @platilloId AND Activo = 1 AND Disponible = 1)
                      THROW 50002, N'Ese platillo ya no está disponible.', 1;

                  UPDATE dbo.OrdenDetalle
                  SET Cantidad = Cantidad + @cantidad
                  WHERE Id = (SELECT TOP (1) Id FROM dbo.OrdenDetalle
                              WHERE OrdenId = @ordenId AND PlatilloId = @platilloId AND Nota = @nota
                                AND Estado = N'Nuevo' AND Anulado = 0
                              ORDER BY Id);

                  IF @@ROWCOUNT = 0
                      INSERT INTO dbo.OrdenDetalle (OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, CreadoPor)
                      SELECT @ordenId, Id, Nombre, @cantidad, Precio, @nota, @usuario
                      FROM dbo.Platillo WHERE Id = @platilloId;",
                new { ordenId, platilloId, cantidad, nota, usuario = Sesion.Id }));
        }

        // solo se puede cambiar libremente lo que todavia no se mando a cocina
        public static void CambiarCantidad(int lineaId, int diferencia)
        {
            Ejecutar(c => c.Execute(
                @"IF EXISTS (SELECT 1 FROM dbo.OrdenDetalle d JOIN dbo.Orden o ON o.Id = d.OrdenId
                             WHERE d.Id = @lineaId AND o.Estado <> N'Abierta')
                      THROW 50001, N'Esta orden ya fue cobrada o anulada en otra caja.', 1;

                  DELETE FROM dbo.OrdenDetalle
                  WHERE Id = @lineaId AND Estado = N'Nuevo' AND Cantidad + @diferencia <= 0;

                  UPDATE dbo.OrdenDetalle
                  SET Cantidad = Cantidad + @diferencia
                  WHERE Id = @lineaId AND Estado = N'Nuevo' AND Cantidad + @diferencia > 0;",
                new { lineaId, diferencia }));
        }

        public static void QuitarNueva(int lineaId)
        {
            Ejecutar(c => c.Execute(
                "DELETE FROM dbo.OrdenDetalle WHERE Id = @lineaId AND Estado = N'Nuevo'", new { lineaId }));
        }

        // lo que ya esta en cocina no se borra, se anula con motivo y queda en la bitacora
        public static void Anular(int lineaId, int cantidad, string motivo, int autorizo)
        {
            Ejecutar(c =>
            {
                using (var tx = c.BeginTransaction())
                {
                    var l = c.QuerySingleOrDefault<LineaOrden>(
                        "SELECT " + CAMPOS_LINEA + " FROM dbo.OrdenDetalle WITH (UPDLOCK) WHERE Id = @lineaId",
                        new { lineaId }, tx);
                    if (l == null || l.Anulado) { tx.Rollback(); return; }

                    if (cantidad >= l.Cantidad)
                    {
                        c.Execute(
                            @"UPDATE dbo.OrdenDetalle SET Anulado = 1, MotivoAnulacion = @motivo, AnuladoPor = @usuario, AutorizadoPor = @autorizo
                              WHERE Id = @lineaId",
                            new { lineaId, motivo, usuario = Sesion.Id, autorizo }, tx);
                    }
                    else
                    {
                        // se parte la linea, una parte sigue y la otra queda anulada para que cuadre el reporte
                        c.Execute("UPDATE dbo.OrdenDetalle SET Cantidad = Cantidad - @cantidad WHERE Id = @lineaId",
                            new { lineaId, cantidad }, tx);
                        c.Execute(
                            @"INSERT INTO dbo.OrdenDetalle (OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, Estado, Enviado,
                                                            Anulado, MotivoAnulacion, CreadoPor, AnuladoPor, AutorizadoPor)
                              SELECT OrdenId, PlatilloId, Nombre, @cantidad, PrecioUnitario, Nota, Estado, Enviado,
                                     1, @motivo, CreadoPor, @usuario, @autorizo
                              FROM dbo.OrdenDetalle WHERE Id = @lineaId",
                            new { lineaId, cantidad, motivo, usuario = Sesion.Id, autorizo }, tx);
                    }
                    Bitacora.Registrar(c, tx, "Anulación",
                        cantidad + " x " + l.Nombre + " de la orden " + l.OrdenId + ". Motivo: " + motivo +
                        (autorizo != Sesion.Id ? ". Autorizó el usuario " + autorizo : ""));
                    tx.Commit();
                }
            });
        }

        public static int EnviarACocina(int ordenId)
        {
            return EjecutarConResultado(c => c.Execute(
                @"UPDATE dbo.OrdenDetalle SET Estado = N'Enviado', Enviado = SYSDATETIME()
                  WHERE OrdenId = @ordenId AND Estado = N'Nuevo' AND Anulado = 0",
                new { ordenId }));
        }

        public static void CambiarDatos(int ordenId, int personas, bool conPropina)
        {
            Ejecutar(c => c.Execute(
                @"UPDATE dbo.Orden SET Personas = @personas, ConPropina = @conPropina
                  WHERE Id = @ordenId AND Estado = N'Abierta'",
                new { ordenId, personas, conPropina }));
        }

        // al salir de una mesa que quedo sin nada se borra la orden, si solo tiene anulados se marca anulada
        public static void CerrarSiVacia(int ordenId)
        {
            Ejecutar(c => c.Execute(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId)
                  BEGIN
                      DELETE FROM dbo.Orden WHERE Id = @ordenId AND Estado = N'Abierta'
                          AND NOT EXISTS (SELECT 1 FROM dbo.Pago WHERE OrdenId = @ordenId);
                  END
                  ELSE IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId AND Anulado = 0)
                  BEGIN
                      UPDATE dbo.Orden SET Estado = N'Anulada', Cierre = SYSDATETIME()
                      WHERE Id = @ordenId AND Estado = N'Abierta';
                  END",
                new { ordenId }));
        }

        // cobrar es todo o nada, el pago y el cierre de la orden van en la misma transaccion
        // si otra caja ya la cobro o alguien agrego algo mientras tanto, no se cobra y se avisa
        public static int Cobrar(int ordenId, byte[] version, Pago pago)
        {
            if (!Sesion.TurnoId.HasValue)
                throw new AvisoException("Primero abre tu caja para poder cobrar.");
            var turnoId = Sesion.TurnoId.Value;

            return Conexion.ConReintento(() =>
            {
                using (var c = Conexion.Abrir())
                using (var tx = c.BeginTransaction())
                {
                    var o = c.QuerySingleOrDefault<Orden>(
                        @"SELECT Id, Estado, ConPropina, PorcentajePropina, Descuento, Version
                          FROM dbo.Orden WITH (UPDLOCK) WHERE Id = @ordenId", new { ordenId }, tx);

                    if (o == null || o.Estado != "Abierta")
                        throw new AvisoException("Esta orden ya fue cobrada o anulada en otra caja.");
                    if (version != null && !o.Version.SequenceEqual(version))
                        throw new AvisoException("Alguien cambió esta orden mientras cobrabas. Revisa el total y vuelve a cobrar.");

                    var subtotal = c.ExecuteScalar<decimal>(
                        "SELECT ISNULL(SUM(Cantidad * PrecioUnitario), 0) FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId AND Anulado = 0",
                        new { ordenId }, tx);
                    if (subtotal <= 0)
                        throw new AvisoException("La orden no tiene nada que cobrar.");

                    var propina = o.ConPropina ? Math.Round(subtotal * o.PorcentajePropina / 100m, 2) : 0m;
                    var total = subtotal - o.Descuento + propina;

                    if (pago.Monto < total)
                        throw new AvisoException("El pago no alcanza para el total de " + total.ToString("Q #,##0.00") + ".");

                    c.Execute(
                        @"INSERT INTO dbo.Pago (OrdenId, Forma, Monto, Recibido, Referencia, UsuarioId, TurnoId)
                          VALUES (@ordenId, @Forma, @monto, @Recibido, @Referencia, @usuario, @turnoId)",
                        new { ordenId, pago.Forma, monto = total, pago.Recibido, pago.Referencia, usuario = Sesion.Id, turnoId }, tx);

                    var ticket = c.ExecuteScalar<int>(
                        @"DECLARE @n INT = NEXT VALUE FOR dbo.SecTicket;
                          UPDATE dbo.Orden
                          SET Estado = N'Cobrada', Cierre = SYSDATETIME(), Subtotal = @subtotal, Propina = @propina,
                              Total = @total, NumeroTicket = @n, TurnoId = @turnoId
                          WHERE Id = @ordenId;
                          UPDATE dbo.OrdenDetalle SET Estado = N'Entregado'
                          WHERE OrdenId = @ordenId AND Anulado = 0 AND Estado <> N'Entregado';
                          SELECT @n;",
                        new { ordenId, subtotal, propina, total, turnoId }, tx);

                    tx.Commit();
                    return ticket;
                }
            });
        }

        // el descuento es un monto en quetzales sobre el consumo, siempre con motivo y con quien lo autorizo
        public static void Descontar(int ordenId, decimal monto, string motivo, int autorizo)
        {
            Ejecutar(c =>
            {
                using (var tx = c.BeginTransaction())
                {
                    var subtotal = c.ExecuteScalar<decimal>(
                        "SELECT ISNULL(SUM(Cantidad * PrecioUnitario), 0) FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId AND Anulado = 0",
                        new { ordenId }, tx);
                    if (monto < 0 || monto > subtotal)
                        throw new AvisoException("El descuento no puede ser mayor que el consumo de " + Ticket.Q(subtotal) + ".");

                    var filas = c.Execute(
                        @"UPDATE dbo.Orden SET Descuento = @monto, DescuentoMotivo = @motivo, DescuentoAutorizo = @autorizo
                          WHERE Id = @ordenId AND Estado = N'Abierta'",
                        new { ordenId, monto, motivo = monto == 0 ? null : motivo, autorizo = monto == 0 ? (int?)null : autorizo }, tx);
                    if (filas == 0) throw new AvisoException("Esta orden ya fue cobrada o anulada en otra caja.");

                    Bitacora.Registrar(c, tx, "Descuento",
                        Ticket.Q(monto) + " en la orden " + ordenId + (monto > 0 ? ". Motivo: " + motivo : " (se quitó)") +
                        (autorizo != Sesion.Id ? ". Autorizó el usuario " + autorizo : ""));
                    tx.Commit();
                }
            });
        }

        public static List<Pago> Pagos(int ordenId)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Pago>("SELECT Forma, Monto, Recibido, Referencia FROM dbo.Pago WHERE OrdenId = @ordenId ORDER BY Id",
                    new { ordenId }).ToList();
        }

        static void Ejecutar(Action<IDbConnection> accion)
        {
            EjecutarConResultado(c => { accion(c); return 0; });
        }

        // los THROW 5000x del lado de sql son avisos para el usuario, no errores
        static T EjecutarConResultado<T>(Func<IDbConnection, T> accion)
        {
            try
            {
                return Conexion.ConReintento(() =>
                {
                    using (var c = Conexion.Abrir())
                        return accion(c);
                });
            }
            catch (SqlException ex) when (ex.Number >= 50000 && ex.Number < 51000)
            {
                throw new AvisoException(ex.Message);
            }
        }
    }
}
