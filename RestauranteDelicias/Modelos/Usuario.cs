using System;

namespace RestauranteDelicias.Modelos
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Cajero = "Cajero";
        public const string Mesero = "Mesero";
        public const string Cocina = "Cocina";

        public static readonly string[] Todos = { Admin, Cajero, Mesero, Cocina };
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        // en la tabla se llama Usuario pero en C# no puede llamarse igual que la clase
        public string Login { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; } = true;
        public bool TienePin { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }

    public class Turno
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Cajero { get; set; }
        public DateTime Apertura { get; set; }
        public DateTime? Cierre { get; set; }
        public decimal MontoInicial { get; set; }
        public decimal? EfectivoContado { get; set; }
        public decimal? Esperado { get; set; }
    }

    // lo que se cobro en un turno, separado por forma de pago
    public class ResumenTurno
    {
        public decimal MontoInicial { get; set; }
        public decimal Efectivo { get; set; }
        public decimal Tarjeta { get; set; }
        public decimal Transferencia { get; set; }
        public decimal Propinas { get; set; }
        public int Ventas { get; set; }

        public decimal EsperadoEnCaja
        {
            get { return MontoInicial + Efectivo; }
        }

        public decimal Total
        {
            get { return Efectivo + Tarjeta + Transferencia; }
        }
    }

    public class EntradaBitacora
    {
        public long Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Equipo { get; set; }
        public string Accion { get; set; }
        public string Detalle { get; set; }
    }
}
