using System;

namespace RestauranteDelicias.Modelos
{
    public class Venta
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public int Mesa { get; set; }
        public int Articulos { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Propina { get; set; }
        public decimal Total { get; set; }
        public string FormaPago { get; set; }
    }
}
