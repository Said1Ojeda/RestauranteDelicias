namespace RestauranteDelicias.Modelos
{
    public class LineaOrden
    {
        public Platillo Platillo { get; set; }
        public int Cantidad { get; set; }
        public string Nota { get; set; } = "";

        // el precio se copia al agregar, si cambian el menu a media orden no le cambia al cliente
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal
        {
            get { return PrecioUnitario * Cantidad; }
        }
    }
}
