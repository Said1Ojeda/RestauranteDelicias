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
              o.Cliente, o.Telefono, o.Direccion, u.Nombre AS Mesero";

        const string CAMPOS_LINEA =
            @"Id, OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, Estado, Anulado, MotivoAnulacion, PagoId";

        const string YA_COBRADA = "Esta orden ya fue cobrada o anulada en otra caja.";

        public static Orden Cargar(int ordenId)
        {
            using (var c = Conexion.Abrir())
            using (var r = c.QueryMultiple(
                "SELECT " + CAMPOS_ORDEN + @" FROM dbo.Orden o LEFT JOIN dbo.Mesa m ON m.Id = o.MesaId
                  LEFT JOIN dbo.Usuario u ON u.Id = o.MeseroId WHERE o.Id = @ordenId;
                  SELECT " + CAMPOS_LINEA + @" FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId ORDER BY Id;
                  SELECT ISNULL(SUM(Monto), 0) FROM dbo.Pago WHERE OrdenId = @ordenId;",
                new { ordenId }))
            {
                var o = r.ReadSingleOrDefault<Orden>();
                var lineas = r.Read<LineaOrden>().ToList();
                var pagado = r.ReadSingle<decimal>();
                if (o == null) return null;
                o.Lineas = lineas;
                o.Pagado = pagado;
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

                InsertarLinea(c, tx, ordenId.Value, platilloId, cantidad, nota);
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

        // los pedidos para llevar no tienen mesa, se agrega directo a su orden
        public static void AgregarAOrden(int ordenId, int platilloId, int cantidad, string nota)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            if (cantidad < 1 || cantidad > 50) throw new AvisoException("La cantidad tiene que ser de 1 a 50.");
            nota = (nota ?? "").Trim();
            if (nota.Length > 150) nota = nota.Substring(0, 150);

            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, ordenId);
                InsertarLinea(c, tx, ordenId, platilloId, cantidad, nota);
                return 0;
            });
        }

        // si ya hay un renglon igual sin mandar a cocina y sin pagar se suma ahi, si no se crea otro
        static void InsertarLinea(IDbConnection c, IDbTransaction tx, int ordenId, int platilloId, int cantidad, string nota)
        {
            c.Execute(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.Platillo WHERE Id = @platilloId AND Activo = 1 AND Disponible = 1)
                      THROW 50002, N'Ese platillo ya no está disponible.', 1;

                  UPDATE dbo.OrdenDetalle
                  SET Cantidad = Cantidad + @cantidad
                  WHERE Id = (SELECT TOP (1) Id FROM dbo.OrdenDetalle
                              WHERE OrdenId = @ordenId AND PlatilloId = @platilloId AND Nota = @nota
                                AND Estado = N'Nuevo' AND Anulado = 0 AND PagoId IS NULL AND Cantidad + @cantidad <= 50
                              ORDER BY Id);

                  IF @@ROWCOUNT = 0
                      INSERT INTO dbo.OrdenDetalle (OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, CreadoPor)
                      SELECT @ordenId, Id, Nombre, @cantidad, Precio, @nota, @usuario
                      FROM dbo.Platillo WHERE Id = @platilloId;",
                new { ordenId, platilloId, cantidad, nota, usuario = Sesion.Id }, tx);
        }

        public static int CrearSinMesa(string tipo, string cliente, string telefono, string direccion)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            if (tipo != "Llevar" && tipo != "Domicilio") throw new AvisoException("Escoge si es para llevar o a domicilio.");
            cliente = Recortar(cliente, 100);
            telefono = Recortar(telefono, 20);
            direccion = Recortar(direccion, 200);
            if (cliente == null) throw new AvisoException("Escribe el nombre del cliente.");
            if (tipo == "Domicilio" && (telefono == null || direccion == null))
                throw new AvisoException("A domicilio se necesita el teléfono y la dirección.");

            return EnTransaccion((c, tx) => c.ExecuteScalar<int>(
                @"INSERT INTO dbo.Orden (Tipo, MeseroId, Cliente, Telefono, Direccion, ConPropina, PorcentajePropina, Equipo)
                  OUTPUT INSERTED.Id
                  VALUES (@tipo, @mesero, @cliente, @telefono, @direccion, 0, @propina, @equipo)",
                new { tipo, mesero = Sesion.Id, cliente, telefono, direccion, propina = Configuracion.Propina, equipo = Conexion.Equipo }, tx));
        }

        public static List<PedidoSinMesa> SinMesa()
        {
            using (var c = Conexion.Abrir())
                return c.Query<PedidoSinMesa>(
                    @"SELECT o.Id AS OrdenId, o.Tipo, o.Cliente, o.Telefono, o.Apertura,
                             ISNULL(t.Subtotal, 0) AS Subtotal, ISNULL(t.Pendientes, 0) AS Pendientes, ISNULL(t.Listos, 0) AS Listos
                      FROM dbo.Orden o
                      OUTER APPLY (
                          SELECT SUM(d.Cantidad * d.PrecioUnitario) AS Subtotal,
                                 SUM(CASE WHEN d.Estado = N'Nuevo' THEN 1 ELSE 0 END) AS Pendientes,
                                 SUM(CASE WHEN d.Estado = N'Listo' THEN 1 ELSE 0 END) AS Listos
                          FROM dbo.OrdenDetalle d
                          WHERE d.OrdenId = o.Id AND d.Anulado = 0
                      ) t
                      WHERE o.Estado = N'Abierta' AND o.MesaId IS NULL
                      ORDER BY o.Apertura").ToList();
        }

        // pasa la cuenta a una mesa libre, si la otra ya esta ocupada hay que juntarlas
        public static void Mover(int ordenId, int mesaDestinoId)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, ordenId);
                var ocupada = c.ExecuteScalar<int?>(
                    "SELECT Id FROM dbo.Orden WITH (UPDLOCK, HOLDLOCK) WHERE MesaId = @mesaDestinoId AND Estado = N'Abierta'",
                    new { mesaDestinoId }, tx);
                if (ocupada.HasValue) throw new AvisoException("Esa mesa ya está ocupada. Para pasar todo ahí usa juntar.");

                var antes = c.ExecuteScalar<int?>("SELECT m.Numero FROM dbo.Orden o JOIN dbo.Mesa m ON m.Id = o.MesaId WHERE o.Id = @ordenId", new { ordenId }, tx);
                var despues = c.ExecuteScalar<int?>("SELECT Numero FROM dbo.Mesa WHERE Id = @mesaDestinoId AND Activa = 1", new { mesaDestinoId }, tx);
                if (!despues.HasValue) throw new AvisoException("Esa mesa ya no está activa.");
                c.Execute("UPDATE dbo.Orden SET MesaId = @mesaDestinoId WHERE Id = @ordenId", new { ordenId, mesaDestinoId }, tx);
                Bitacora.Registrar(c, tx, "Cambio de mesa", "Orden " + ordenId + " de la mesa " + antes + " a la " + despues);
                return 0;
            });
        }

        // todo lo de la orden de origen pasa a la de destino, sus platillos, sus pagos y sus personas
        // los candados se toman en orden de id, asi dos juntadas cruzadas no se trancan entre si
        public static void Juntar(int ordenOrigenId, int ordenDestinoId)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            if (ordenOrigenId == ordenDestinoId) throw new AvisoException("Escoge otra mesa.");

            EnTransaccion((c, tx) =>
            {
                BloquearOrdenAbierta(c, tx, Math.Min(ordenOrigenId, ordenDestinoId));
                BloquearOrdenAbierta(c, tx, Math.Max(ordenOrigenId, ordenDestinoId));

                var descuento = c.ExecuteScalar<decimal>("SELECT Descuento FROM dbo.Orden WHERE Id = @ordenOrigenId", new { ordenOrigenId }, tx);
                if (descuento > 0) throw new AvisoException("Esta cuenta tiene descuento. Quítalo antes de juntarla con otra.");

                c.Execute(
                    @"UPDATE destino SET destino.Personas = CASE WHEN destino.Personas + origen.Personas > 100 THEN 100 ELSE destino.Personas + origen.Personas END
                      FROM dbo.Orden destino CROSS JOIN dbo.Orden origen
                      WHERE destino.Id = @ordenDestinoId AND origen.Id = @ordenOrigenId;
                      UPDATE dbo.OrdenDetalle SET OrdenId = @ordenDestinoId WHERE OrdenId = @ordenOrigenId;
                      UPDATE dbo.Pago SET OrdenId = @ordenDestinoId WHERE OrdenId = @ordenOrigenId;
                      DELETE FROM dbo.Orden WHERE Id = @ordenOrigenId;",
                    new { ordenOrigenId, ordenDestinoId }, tx);
                Bitacora.Registrar(c, tx, "Juntar cuentas", "La orden " + ordenOrigenId + " pasó a la orden " + ordenDestinoId);
                return 0;
            });
        }

        // solo se puede cambiar libremente lo que todavia no se mando a cocina
        public static void CambiarCantidad(int lineaId, int diferencia)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            EnTransaccion((c, tx) =>
            {
                var ordenId = OrdenDeLinea(c, tx, lineaId);
                BloquearOrdenAbierta(c, tx, ordenId);
                c.Execute(
                    @"DELETE FROM dbo.OrdenDetalle
                      WHERE Id = @lineaId AND Estado = N'Nuevo' AND PagoId IS NULL AND Cantidad + @diferencia <= 0;

                      UPDATE dbo.OrdenDetalle
                      SET Cantidad = Cantidad + @diferencia
                      WHERE Id = @lineaId AND Estado = N'Nuevo' AND PagoId IS NULL AND Cantidad + @diferencia BETWEEN 1 AND 50;",
                    new { lineaId, diferencia }, tx);
                ExigirQueCubraLoPagado(c, tx, ordenId);
                return 0;
            });
        }

        public static void QuitarNueva(int lineaId)
        {
            Sesion.Exigir(Sesion.PuedeTomarOrdenes);
            EnTransaccion((c, tx) =>
            {
                var ordenId = OrdenDeLinea(c, tx, lineaId);
                BloquearOrdenAbierta(c, tx, ordenId);
                c.Execute("DELETE FROM dbo.OrdenDetalle WHERE Id = @lineaId AND Estado = N'Nuevo' AND PagoId IS NULL", new { lineaId }, tx);
                ExigirQueCubraLoPagado(c, tx, ordenId);
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
                if (l.Pagada) throw new AvisoException("Ese platillo ya está pagado, no se puede anular.");

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
                ExigirQueCubraLoPagado(c, tx, l.OrdenId);
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
            EnTransaccion((c, tx) =>
            {
                var filas = c.Execute(
                    @"UPDATE dbo.Orden SET Personas = @personas, ConPropina = @conPropina
                      WHERE Id = @ordenId AND Estado = N'Abierta'",
                    new { ordenId, personas, conPropina }, tx);
                ExigirQueCubraLoPagado(c, tx, ordenId);
                return filas;
            });
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

        // cada pago va en su propia transaccion, la orden se cierra con el pago que deja el saldo en cero
        // saldoVisto es lo que tenia el cajero en pantalla, si alguien cambio la cuenta mientras tanto no se cobra y se avisa
        // con platillos el pago cubre esos renglones, si se paga solo parte de un renglon este se parte en dos
        public static ResultadoPago RegistrarPago(int ordenId, decimal saldoVisto, Pago pago, IList<LineaPagada> platillos)
        {
            Sesion.Exigir(Sesion.PuedeCobrar);
            if (!Sesion.TurnoId.HasValue)
                throw new AvisoException("Primero abre tu caja para poder cobrar.");
            if (pago.Monto <= 0) throw new AvisoException("El monto tiene que ser mayor que cero.");
            var turnoId = Sesion.TurnoId.Value;
            var parte = Recortar(pago.Parte, 40);
            platillos = platillos ?? new List<LineaPagada>();

            return EnTransaccion((c, tx) =>
            {
                // el candado de la caja hace que no se pueda cerrar mientras se esta cobrando en ella
                var cajaAbierta = c.ExecuteScalar<int?>(
                    "SELECT Id FROM dbo.Turno WITH (UPDLOCK, ROWLOCK) WHERE Id = @turnoId AND Cierre IS NULL",
                    new { turnoId }, tx);
                if (!cajaAbierta.HasValue)
                    throw new AvisoException("Tu caja ya está cerrada. Ábrela de nuevo para cobrar.");

                BloquearOrdenAbierta(c, tx, ordenId);
                var cuenta = Cuenta(c, tx, ordenId);
                if (cuenta.Subtotal <= 0)
                    throw new AvisoException("La orden no tiene nada que cobrar.");
                if (cuenta.Descuento > cuenta.Subtotal)
                    throw new AvisoException("El descuento quedó mayor que el consumo. Cámbialo antes de cobrar.");

                var saldo = cuenta.Total - cuenta.Pagado;
                if (saldo != saldoVisto)
                    throw new AvisoException("La cuenta cambió mientras cobrabas, ahora el saldo es " + Ticket.Q(saldo) + ". Revísala y vuelve a cobrar.");

                var lineas = platillos.Count == 0 ? new List<LineaOrden>() : c.Query<LineaOrden>(
                    "SELECT " + CAMPOS_LINEA + " FROM dbo.OrdenDetalle WITH (UPDLOCK) WHERE OrdenId = @ordenId AND Id IN @ids",
                    new { ordenId, ids = platillos.Select(x => x.LineaId).ToList() }, tx).ToList();

                if (platillos.Count > 0)
                {
                    var subtotalPlatillos = 0m;
                    foreach (var p in platillos)
                    {
                        var l = lineas.FirstOrDefault(x => x.Id == p.LineaId);
                        if (l == null || l.Anulado || l.Pagada || p.Cantidad < 1 || p.Cantidad > l.Cantidad)
                            throw new AvisoException("Alguien cambió los platillos de esta cuenta. Revísala y vuelve a cobrar.");
                        subtotalPlatillos += p.Cantidad * l.PrecioUnitario;
                    }
                    var quedanSinPagar = c.ExecuteScalar<decimal>(
                        @"SELECT ISNULL(SUM(Cantidad * PrecioUnitario), 0) FROM dbo.OrdenDetalle
                          WHERE OrdenId = @ordenId AND Anulado = 0 AND PagoId IS NULL", new { ordenId }, tx);
                    var esperado = subtotalPlatillos == quedanSinPagar ? saldo : Math.Min(saldo, cuenta.ParteDe(subtotalPlatillos));
                    if (pago.Monto != esperado)
                        throw new AvisoException("El monto de esos platillos es " + Ticket.Q(esperado) + ". Revísalo y vuelve a cobrar.");
                }

                if (pago.Monto > saldo)
                    throw new AvisoException("El pago es mayor que el saldo de " + Ticket.Q(saldo) + ".");

                var pagoId = c.ExecuteScalar<int>(
                    @"INSERT INTO dbo.Pago (OrdenId, Forma, Monto, Recibido, Referencia, UsuarioId, TurnoId, Parte)
                      OUTPUT INSERTED.Id
                      VALUES (@ordenId, @Forma, @Monto, @Recibido, @Referencia, @usuario, @turnoId, @parte)",
                    new { ordenId, pago.Forma, pago.Monto, pago.Recibido, pago.Referencia, usuario = Sesion.Id, turnoId, parte }, tx);

                foreach (var p in platillos)
                {
                    var l = lineas.First(x => x.Id == p.LineaId);
                    if (p.Cantidad == l.Cantidad)
                    {
                        c.Execute("UPDATE dbo.OrdenDetalle SET PagoId = @pagoId WHERE Id = @Id", new { pagoId, l.Id }, tx);
                        continue;
                    }
                    // se parte el renglon, lo que se paga queda en uno nuevo con el mismo estado de cocina
                    c.Execute(
                        @"UPDATE dbo.OrdenDetalle SET Cantidad = Cantidad - @cantidad WHERE Id = @lineaId;
                          INSERT INTO dbo.OrdenDetalle (OrdenId, PlatilloId, Nombre, Cantidad, PrecioUnitario, Nota, Estado, Enviado,
                                                        Preparando, Listo, Entregado, Anulado, CreadoPor, PagoId)
                          SELECT OrdenId, PlatilloId, Nombre, @cantidad, PrecioUnitario, Nota, Estado, Enviado,
                                 Preparando, Listo, Entregado, 0, CreadoPor, @pagoId
                          FROM dbo.OrdenDetalle WHERE Id = @lineaId;",
                        new { lineaId = l.Id, cantidad = p.Cantidad, pagoId }, tx);
                }

                var resultado = new ResultadoPago { PagoId = pagoId, Saldo = saldo - pago.Monto };
                if (resultado.Saldo > 0) return resultado;

                // el ultimo pago cierra la cuenta, congela los totales y le da su numero de ticket
                // lo que cocina todavia esta preparando se queda en su pantalla aunque ya se haya pagado
                resultado.Cerrada = true;
                resultado.NumeroTicket = c.ExecuteScalar<int>(
                    @"DECLARE @n INT = NEXT VALUE FOR dbo.SecTicket;
                      UPDATE dbo.Orden
                      SET Estado = N'Cobrada', Cierre = SYSDATETIME(), Subtotal = @Subtotal, Propina = @Propina,
                          Total = @Total, NumeroTicket = @n, TurnoId = @turnoId
                      WHERE Id = @ordenId;
                      UPDATE dbo.OrdenDetalle SET Estado = N'Entregado', Entregado = SYSDATETIME()
                      WHERE OrdenId = @ordenId AND Anulado = 0 AND Estado = N'Listo';
                      SELECT @n;",
                    new { ordenId, cuenta.Subtotal, cuenta.Propina, cuenta.Total, turnoId }, tx);
                return resultado;
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
                ExigirQueCubraLoPagado(c, tx, ordenId);

                Bitacora.Registrar(c, tx, "Descuento",
                    Ticket.Q(monto) + " en la orden " + ordenId + (monto > 0 ? ". Motivo " + motivo : ", se quitó") +
                    (autorizo != Sesion.Id ? ". Autorizó el usuario " + autorizo : ""));
                return 0;
            });
        }

        public static List<Pago> Pagos(int ordenId)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Pago>("SELECT Id, Parte, Forma, Monto, Recibido, Referencia FROM dbo.Pago WHERE OrdenId = @ordenId ORDER BY Id",
                    new { ordenId }).ToList();
        }

        // la cuenta como esta en la base en este momento, con lo que ya se pago
        static Orden Cuenta(IDbConnection c, IDbTransaction tx, int ordenId)
        {
            var o = c.QuerySingle<Orden>(
                @"SELECT Id, Estado, ConPropina, PorcentajePropina, Descuento,
                         (SELECT ISNULL(SUM(Monto), 0) FROM dbo.Pago WHERE OrdenId = @ordenId) AS Pagado
                  FROM dbo.Orden WHERE Id = @ordenId", new { ordenId }, tx);
            o.Lineas = c.Query<LineaOrden>(
                "SELECT " + CAMPOS_LINEA + " FROM dbo.OrdenDetalle WHERE OrdenId = @ordenId", new { ordenId }, tx).ToList();
            return o;
        }

        // ningun cambio puede dejar la cuenta en menos de lo que ya pagaron
        static void ExigirQueCubraLoPagado(IDbConnection c, IDbTransaction tx, int ordenId)
        {
            var cuenta = Cuenta(c, tx, ordenId);
            if (cuenta.Pagado > 0 && cuenta.Total < cuenta.Pagado)
                throw new AvisoException("Ya pagaron " + Ticket.Q(cuenta.Pagado) + " de esta cuenta, no puede quedar en menos.");
        }

        static string Recortar(string texto, int largo)
        {
            texto = (texto ?? "").Trim();
            if (texto.Length == 0) return null;
            return texto.Length > largo ? texto.Substring(0, largo) : texto;
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
