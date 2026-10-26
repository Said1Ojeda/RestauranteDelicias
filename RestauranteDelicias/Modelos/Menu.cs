using System.Collections.Generic;

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
        public string CategoriaColor { get; set; }
        public bool TieneOpciones { get; set; }
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

    // una pregunta que se hace al pedir un platillo, como el estilo de los huevos o los extras
    public class GrupoOpcion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Minimo { get; set; }
        public int Maximo { get; set; } = 1;
        public int? SoloSiOpcionId { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public List<Opcion> Opciones { get; set; } = new List<Opcion>();

        public bool Obligatorio
        {
            get { return Minimo > 0; }
        }

        public bool EscogeUno
        {
            get { return Maximo == 1; }
        }

        public override string ToString()
        {
            return Nombre;
        }
    }

    public class Opcion
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; } = true;
    }
}
