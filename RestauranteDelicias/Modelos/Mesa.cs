using System;

namespace RestauranteDelicias.Modelos
{
    public class Area
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Orden { get; set; }
        public bool Activa { get; set; } = true;

        public override string ToString()
        {
            return Nombre;
        }
    }

    // una mesa como la ve el admin al configurar el salon
    public class Mesa
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int AreaId { get; set; }
        public string AreaNombre { get; set; }
        public int Capacidad { get; set; } = 4;
        public string Forma { get; set; } = "Cuadrada";
        public bool Activa { get; set; } = true;
    }

    // como esta cada mesa en este momento, sale de una sola consulta para refrescar rapido
    public class MesaEstado
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public int AreaId { get; set; }
        public string AreaNombre { get; set; }
        public int Capacidad { get; set; }
        public string Forma { get; set; }

        public int? OrdenId { get; set; }
        public DateTime? Apertura { get; set; }
        public int? Personas { get; set; }
        public decimal Subtotal { get; set; }
        public int Articulos { get; set; }
        public int Pendientes { get; set; }
        public int Listos { get; set; }

        public bool Ocupada
        {
            get { return OrdenId.HasValue; }
        }
    }
}
