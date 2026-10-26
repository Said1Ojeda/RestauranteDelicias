﻿using System;
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
    // cada cambio bloquea primero la fila de la orden, asi nunca se cuela un platillo mientras otra caja esta cobrando
    public static class OrdenRepo
    {
        const string CAMPOS_ORDEN =
            @"o.Id, o.Tipo, o.MesaId, m.Numero AS MesaNumero, o.Estado, o.Personas, o.ConPropina,
              o.PorcentajePropina, o.Descuento, o.DescuentoMotivo, o.Apertura, o.Cierre, o.NumeroTicket, o.Version,
              u.Nombre AS Mesero";

        const string CAMPOS_LINEA =
            @"Id, OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, Estado, Anulado, MotivoAnulacion";

        const string YA_COBRADA = "Esta orden ya fue cobrada o anulada en otra caja.";

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

        // si la mesa no tiene orden se abre en la misma transaccion, asi dos meseros que piden al mismo tiempo caen en la misma cuenta
        // ordenEnPantalla es la que tiene abierta quien pide, si ya no es la de la mesa es que la cobraron en otra caja
        public static int AgregarAMesa(int mesaId, int? ordenEnPantalla, int platilloId, int cantidad, string nota, int personas, bool conPropina)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            if (cantidad < 1 || cantidad > 50) throw new AvisoException("La cantidad tiene que ser de 1 a 50.");
            nota = (nota ?? "").Trim();
            if (nota.Length > 150) nota = nota.Substring(0, 150);

            Func<int> agregar = () => EnTransaccion((c, tx) =>
            {
                var ordenId = c.ExecuteScalar<int?>(
                    "SELECT Id FROM dbo.Orden WITH (UPDLOCK, HOLDLOCK) WHERE MesaId = @mesaId AND Estado = N'Abierta'",
                    new { mesaId }, tx);

                if (ordenEnPantalla.HasValue && ordenId != ordenEnPantalla)
                    throw new AvisoException(YA_COBRADA);

                if (!ordenId.HasValue)
                    ordenId = c.ExecuteScalar<int>(
                        @"INSERT INTO dbo.Orden (Tipo, MesaId, MeseroId, Personas, ConPropina, PorcentajePropina, Equipo)
                          OUTPUT INSERTED.Id
                          VALUES (N'Mesa', @mesaId, @mesero, @personas, @conPropina, @propina, @equipo)",
                        new { mesaId, mesero = Sesion.Id, personas, conPropina, propina = Configuracion.Propina, equipo = Conexion.Equipo }, tx);

                c.Execute(
                    @"IF NOT EXISTS (SELECT 1 FROM dbo.Platillo WHERE Id = @platilloId AND Activo = 1 AND Disponible = 1)
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
                    new { ordenId, platilloId, cantidad, nota, usuario = Sesion.Id }, tx);
                return ordenId.Value;
            });

            try
            {
                return agregar();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // el indice unico atajo a otro que abrio la mesa en el mismo instante, al repetir ya se usa la suya
                return agregar();
            }
        }

        // solo se puede cambiar libremente lo que todavia no se mando a cocina
        public static void CambiarCantidad(int lineaId, int diferencia)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, OrdenDeLinea(c, tx, lineaId));
                c.Execute(
                    @"DELETE FROM dbo.OrdenDetalle
                      WHERE Id = @lineaId AND Estado = N'Nuevo' AND Cantidad + @diferencia <= 0;

                      UPDATE dbo.OrdenDetalle
                      SET Cantidad = Cantidad + @diferencia
                      WHERE Id = @lineaId AND Estado = N'Nuevo' AND Cantidad + @diferencia BETWEEN 1 AND 50;",
                    new { lineaId, diferencia }, tx);
                return 0;
            });
        }

        public static void QuitarNueva(int lineaId)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, OrdenDeLinea(c, tx, lineaId));
                c.Execute("DELETE FROM dbo.OrdenDetalle WHERE Id = @lineaId AND Estado = N'Nuevo'", new { lineaId }, tx);
                return 0;
            });
        }

        // lo que ya esta en cocina no se borra, se anula con motivo y queda en la bitacora
        public static void Anular(int lineaId, int cantidad, string motivo, int autorizo)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            if (cantidad < 1) throw new AvisoException("Escoge cuántos se anulan.");
            motivo = (motivo ?? "").Trim();
            if (motivo.Length < 3) throw new AvisoException("Escribe por qué se anula.");
            if (motivo.Length > 150) motivo = motivo.Substring(0, 150);

            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, OrdenDeLinea(c, tx, lineaId));
                ExigirAdmin(c, tx, autorizo);

                var l = c.QuerySingleOrDefault<LineaOrden>(
                    "SELECT " + CAMPOS_LINEA + " FROM dbo.OrdenDetalle WITH (UPDLOCK) WHERE Id = @lineaId",
                    new { lineaId }, tx);
                if (l == null || l.Anulado) return 0;

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
                    Math.Min(cantidad, l.Cantidad) + " x " + l.Nombre + " de la orden " + l.OrdenId + ". Motivo " + motivo +
                    (autorizo != Sesion.Id ? ". Autorizó el usuario " + autorizo : ""));
                return 0;
            });
        }

        public static int EnviarACocina(int ordenId)
        {
            return EnTransaccion((c, tx) => c.Execute(
                @"UPDATE d SET d.Estado = N'Enviado', d.Enviado = SYSDATETIME()
                  FROM dbo.OrdenDetalle d
                  JOIN dbo.Orden o ON o.Id = d.OrdenId
                  WHERE d.OrdenId = @ordenId AND d.Estado = N'Nuevo' AND d.Anulado = 0 AND o.Estado = N'Abierta'",
                new { ordenId }, tx));
        }

        public static void CambiarDatos(int ordenId, int personas, bool conPropina)
        {
            if (personas < 1 || personas > 100) throw new AvisoException("Las personas tienen que ser de 1 a 100.");
            EnTransaccion((c, tx) => c.Execute(
                @"UPDATE dbo.Orden SET Personas = @personas, ConPropina = @conPropina
                  WHERE Id = @ordenId AND Estado = N'Abierta'",
                new { ordenId, personas, conPropina }, tx));
        }

        // al salir de una mesa que quedo sin nada se borra la orden, si solo tiene anulados se marca anulada
        // primero se toma el candado de la orden, asi no se borra justo cuando otro mesero le esta agregando algo
        public static void CerrarSiVacia(int ordenId)
        {
            EnTransaccion((c, tx) =>
            {
                var abierta = c.ExecuteScalar<int?>(
                    "SELECT Id FROM dbo.Orden WITH (UPDLOCK, ROWLOCK) WHERE Id = @ordenId AND Estado = N'Abierta'",
                    new { ordenId }, tx);
                if (!abierta.HasValue) return 0;

                return c.Execute(
                    @"IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WITH (READCOMMITTEDLOCK) WHERE OrdenId = @ordenId)
                         AND NOT EXISTS (SELECT 1 FROM dbo.Pago WITH (READCOMMITTEDLOCK) WHERE OrdenId = @ordenId)
                      BEGIN
                          DELETE FROM dbo.Orden WHERE Id = @ordenId;
                      END
                      ELSE IF NOT EXISTS (SELECT 1 FROM dbo.OrdenDetalle WITH (READCOMMITTEDLOCK) WHERE OrdenId = @ordenId AND Anulado = 0)
                      BEGIN
                          UPDATE dbo.Orden SET Estado = N'Anulada', Cierre = SYSDATETIME() WHERE Id = @ordenId;
                      END",
                    new { ordenId }, tx);
            });
        }

        // cobrar es todo o nada, el pago y el cierre de la orden van en la misma transaccion
        // si otra caja ya la cobro, si alguien le cambio algo o si el total ya no es el que vio el cajero, no se cobra y se avisa
        public static int Cobrar(int ordenId, byte[] version, Pago pago)
        {
            Sesion.Exigir(Sesion.PuedeCobrar);
            if (!Sesion.TurnoId.HasValue)
                throw new AvisoException("Primero abre tu caja para poder cobrar.");
            var turnoId = Sesion.TurnoId.Value;

            return EnTransaccion((c, tx) =>
            {
                // el candado de la caja hace que no se pueda cerrar mientras se esta cobrando en ella
                var cajaAbierta = c.ExecuteScalar<int?>(
                    "SELECT Id FROM dbo.Turno WITH (UPDLOCK, ROWLOCK) WHERE Id = @turnoId AND Cierre IS NULL",
                    new { turnoId }, tx);
                if (!cajaAbierta.HasValue)
                    throw new AvisoException("Tu caja ya está cerrada. Ábrela de nuevo para cobrar.");

                var o = c.QuerySingleOrDefault<Orden>(
                    @"SELECT Id, Estado, ConPropina, PorcentajePropina, Descuento, Version
                      FROM dbo.Orden WITH (UPDLOCK, ROWLOCK) WHERE Id = @ordenId", new { ordenId }, tx);

                if (o == null || o.Estado != "Abierta")
                    throw new AvisoException(YA_COBRADA);
                if (version != null && !o.Version.SequenceEqual(version))
                    throw new AvisoException("Alguien cambió esta orden mientras cobrabas. Revisa el total y vuelve a cobrar.");

                var subtotal = c.ExecuteScalar<decimal>(
                    "SELECT ISNULL(SUM(Cantidad * PrecioUnitario), 0) FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId AND Anulado = 0",
                    new { ordenId }, tx);
                if (subtotal <= 0)
                    throw new AvisoException("La orden no tiene nada que cobrar.");
                if (o.Descuento > subtotal)
                    throw new AvisoException("El descuento quedó mayor que el consumo. Cámbialo antes de cobrar.");

                var propina = o.ConPropina ? Math.Round(subtotal * o.PorcentajePropina / 100m, 2) : 0m;
                var total = subtotal - o.Descuento + propina;

                // los platillos no cambian el rowversion de la orden, por eso tambien se compara el total
                if (pago.Monto != total)
                    throw new AvisoException("El total cambió a " + Ticket.Q(total) + " mientras cobrabas. Revísalo y vuelve a cobrar.");

                c.Execute(
                    @"INSERT INTO dbo.Pago (OrdenId, Forma, Monto, Recibido, Referencia, UsuarioId, TurnoId)
                      VALUES (@ordenId, @Forma, @monto, @Recibido, @Referencia, @usuario, @turnoId)",
                    new { ordenId, pago.Forma, monto = total, pago.Recibido, pago.Referencia, usuario = Sesion.Id, turnoId }, tx);

                // lo que cocina todavia esta preparando se queda en su pantalla aunque ya se haya pagado
                return c.ExecuteScalar<int>(
                    @"DECLARE @n INT = NEXT VALUE FOR dbo.SecTicket;
                      UPDATE dbo.Orden
                      SET Estado = N'Cobrada', Cierre = SYSDATETIME(), Subtotal = @subtotal, Propina = @propina,
                          Total = @total, NumeroTicket = @n, TurnoId = @turnoId
                      WHERE Id = @ordenId;
                      UPDATE dbo.OrdenDetalle SET Estado = N'Entregado', Entregado = SYSDATETIME()
                      WHERE OrdenId = @ordenId AND Anulado = 0 AND Estado = N'Listo';
                      SELECT @n;",
                    new { ordenId, subtotal, propina, total, turnoId }, tx);
            });
        }

        // el descuento es un monto en quetzales sobre el consumo, siempre con motivo y con quien lo autorizo
        public static void Descontar(int ordenId, decimal monto, string motivo, int autorizo)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            motivo = (motivo ?? "").Trim();
            if (motivo.Length > 150) motivo = motivo.Substring(0, 150);

            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, ordenId);
                ExigirAdmin(c, tx, autorizo);

                var subtotal = c.ExecuteScalar<decimal>(
                    "SELECT ISNULL(SUM(Cantidad * PrecioUnitario), 0) FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId AND Anulado = 0",
                    new { ordenId }, tx);
                if (monto < 0 || monto > subtotal)
                    throw new AvisoException("El descuento no puede ser mayor que el consumo de " + Ticket.Q(subtotal) + ".");

                c.Execute(
                    @"UPDATE dbo.Orden SET Descuento = @monto, DescuentoMotivo = @motivo, DescuentoAutorizo = @autorizo
                      WHERE Id = @ordenId",
                    new { ordenId, monto, motivo = monto == 0 ? null : motivo, autorizo = monto == 0 ? (int?)null : autorizo }, tx);

                Bitacora.Registrar(c, tx, "Descuento",
                    Ticket.Q(monto) + " en la orden " + ordenId + (monto > 0 ? ". Motivo " + motivo : ", se quitó") +
                    (autorizo != Sesion.Id ? ". Autorizó el usuario " + autorizo : ""));
                return 0;
            });
        }

        public static List<Pago> Pagos(int ordenId)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Pago>("SELECT Forma, Monto, Recibido, Referencia FROM dbo.Pago WHERE OrdenId = @ordenId ORDER BY Id",
                    new { ordenId }).ToList();
        }

        static int OrdenDeLinea(IDbConnection c, IDbTransaction tx, int lineaId)
        {
            var ordenId = c.ExecuteScalar<int?>("SELECT OrdenId FROM dbo.OrdenDetalle WHERE Id = @lineaId", new { lineaId }, tx);
            if (!ordenId.HasValue) throw new AvisoException("Ese platillo ya no está en la orden, alguien lo quitó desde otra compu.");
            return ordenId.Value;
        }

        // toma el candado de la orden hasta el final de la transaccion, cobrar toma el mismo y por eso se esperan uno al otro
        static void BloquearOrdenAbierta(IDbConnection c, IDbTransaction tx, int ordenId)
        {
            var estado = c.ExecuteScalar<string>(
                "SELECT Estado FROM dbo.Orden WITH (UPDLOCK, ROWLOCK) WHERE Id = @ordenId", new { ordenId }, tx);
            if (estado != "Abierta") throw new AvisoException(YA_COBRADA);
        }

        // quien autoriza se revisa contra la base, no basta con lo que diga la pantalla
        static void ExigirAdmin(IDbConnection c, IDbTransaction tx, int usuarioId)
        {
            var esAdmin = c.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM dbo.Usuario WHERE Id = @usuarioId AND Rol = N'Admin' AND Activo = 1",
                new { usuarioId }, tx) > 0;
            if (!esAdmin) throw new AvisoException("Solo un administrador activo puede autorizar esto.");
        }

        // abre, corre todo en una transaccion y la confirma, repite si hubo deadlock
        // los THROW 5000x del lado de sql son avisos para el usuario, no errores
        static T EnTransaccion<T>(Func<IDbConnection, IDbTransaction, T> accion)
        {
            try
            {
                return Conexion.ConReintento(() =>
                {
                    using (var c = Conexion.Abrir())
                    using (var tx = c.BeginTransaction())
                    {
                        var resultado = accion(c, tx);
                        tx.Commit();
                        return resultado;
                    }
                });
            }
            catch (SqlException ex) when (ex.Number >= 50000 && ex.Number < 51000)
            {
                throw new AvisoException(ex.Message);
            }
        }
    }
}
