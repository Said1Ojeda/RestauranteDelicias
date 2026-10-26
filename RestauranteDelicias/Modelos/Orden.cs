using System;
using System.Collections.Generic;
using System.Linq;

namespace RestauranteDelicias.Modelos
{
    public class Orden
    {
        public int Id { get; set; }
        public string Tipo { get; set; }
        public int? MesaId { get; set; }
        public int? MesaNumero { get; set; }
        public string Estado { get; set; }
        public int Personas { get; set; }
        public bool ConPropina { get; set; }
        public decimal PorcentajePropina { get; set; }
        public decimal Descuento { get; set; }
        public string DescuentoMotivo { get; set; }
        public string Mesero { get; set; }
        public string Cliente { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public DateTime Apertura { get; set; }
        public DateTime? Cierre { get; set; }
        public int? NumeroTicket { get; set; }

        // rowversion de sql server, sirve para saber si alguien mas la cambio mientras cobrabas
        public byte[] Version { get; set; }

        public List<LineaOrden> Lineas { get; set; } = new List<LineaOrden>();

        // lo que ya se pago en partes, la orden se cierra cuando el saldo llega a cero
        public decimal Pagado { get; set; }

        public bool EsMesa
        {
            get { return MesaId.HasValue; }
        }

        public string Titulo
        {
            get
            {
                if (MesaNumero.HasValue) return "Mesa " + MesaNumero;
                var tipo = Tipo == "Domicilio" ? "Domicilio" : "Para llevar";
                return string.IsNullOrWhiteSpace(Cliente) ? tipo : tipo + " · " + Cliente;
            }
        }

        public IEnumerable<LineaOrden> Activas
        {
            get { return Lineas.Where(l => !l.Anulado); }
        }

        public decimal Subtotal
        {
            get { return Activas.Sum(l => l.Subtotal); }
        }

        public decimal Propina
        {
            get { return ConPropina ? Math.Round(Subtotal * PorcentajePropina / 100m, 2) : 0m; }
        }

        public decimal Total
        {
            get { return Subtotal - Descuento + Propina; }
        }

        public decimal Saldo
        {
            get { return Total - Pagado; }
        }

        // cuanto le toca a unos platillos del total, con su parte de propina y descuento
        public decimal ParteDe(decimal subtotalDePlatillos)
        {
            return Subtotal <= 0 ? 0m : Math.Round(subtotalDePlatillos * Total / Subtotal, 2);
        }

        // los precios ya traen iva, aca solo se saca cuanto es para el ticket
        public decimal IvaIncluido(decimal porcentajeIva)
        {
            var neto = Subtotal - Descuento;
            return Math.Round(neto - neto / (1 + porcentajeIva / 100m), 2);
        }

        public int Articulos
        {
            get { return Activas.Sum(l => l.Cantidad); }
        }

        public int SinEnviar
        {
            get { return Activas.Count(l => l.Estado == "Nuevo"); }
        }

        public bool Vacia
        {
            get { return !Activas.Any(); }
        }
    }

    public class LineaOrden
    {
        public int Id { get; set; }
        public int OrdenId { get; set; }
        public int PlatilloId { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public string Nota { get; set; }
        public string Estado { get; set; }
        public bool Anulado { get; set; }
        public string MotivoAnulacion { get; set; }
        public int? PagoId { get; set; }

        public bool Pagada
        {
            get { return PagoId.HasValue; }
        }

        public decimal Subtotal
        {
            get { return Anulado ? 0m : PrecioUnitario * Cantidad; }
        }

        public bool YaEnCocina
        {
            get { return Estado != "Nuevo"; }
        }
    }

    public class Pago
    {
        public int Id { get; set; }
        public string Parte { get; set; }
        public string Forma { get; set; }
        public decimal Monto { get; set; }
        public decimal? Recibido { get; set; }
        public string Referencia { get; set; }
    }

    // una fila del reporte de ventas
    public class Venta
    {
        public int Id { get; set; }
        public int NumeroTicket { get; set; }
        public DateTime Cierre { get; set; }
        public int? MesaNumero { get; set; }
        public string Tipo { get; set; }
        public int Articulos { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Propina { get; set; }
        public decimal Total { get; set; }
        public string Formas { get; set; }
        public string Mesero { get; set; }
        public string Cajero { get; set; }
    }

    public class TotalForma
    {
        public string Forma { get; set; }
        public decimal Total { get; set; }
        public int Cantidad { get; set; }
    }

    // cuantos de un renglon cubre un pago cuando se divide la cuenta por platillos
    public class LineaPagada
    {
        public int LineaId { get; set; }
        public int Cantidad { get; set; }
    }

    public class ResultadoPago
    {
        public int PagoId { get; set; }
        public bool Cerrada { get; set; }
        public int? NumeroTicket { get; set; }
        public decimal Saldo { get; set; }
    }

    // un pedido abierto para llevar o a domicilio, sale en la pantalla de mesas
    public class PedidoSinMesa
    {
        public int OrdenId { get; set; }
        public string Tipo { get; set; }
        public string Cliente { get; set; }
        public string Telefono { get; set; }
        public DateTime Apertura { get; set; }
        public decimal Subtotal { get; set; }
        public int Pendientes { get; set; }
        public int Listos { get; set; }
    }
}
