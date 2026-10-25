namespace RestauranteDelicias.Modelos
{
    public class Platillo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public decimal Precio { get; set; }

        // en vez de borrar se desactiva, asi no se pierden las ventas viejas que lo usan
        public bool Disponible { get; set; } = true;

        public override string ToString()
        {
            return Nombre;
        }
    }
}
