namespace RestauranteDelicias.Modelos
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
        public string Estacion { get; set; } = "Cocina";
        public string Color { get; set; }
        public bool Activa { get; set; } = true;

        public override string ToString()
        {
            return Nombre;
        }
    }

    public class Platillo
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }

        // disponible es para el dia, activo es si sigue en el menu
        public bool Disponible { get; set; } = true;
        public bool Activo { get; set; } = true;

        public override string ToString()
        {
            return Nombre;
        }
    }
}
