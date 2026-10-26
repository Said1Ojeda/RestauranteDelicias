using System;
using System.Collections.Generic;
using System.Linq;

namespace RestauranteDelicias.Modelos
{
    // un renglon de lo que cocina tiene que preparar
    public class LineaCocina
    {
        public int Id { get; set; }
        public int OrdenId { get; set; }
        public int? MesaNumero { get; set; }
        public string Tipo { get; set; }
        public string Cliente { get; set; }
        public string Mesero { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public string Nota { get; set; }
        public string Estado { get; set; }
        public DateTime Enviado { get; set; }
        public bool Anulado { get; set; }
        public string Estacion { get; set; }
    }

    // lo que se mando junto de una mesa sale en una sola tarjeta
    public class Comanda
    {
        public int OrdenId { get; set; }
        public int? MesaNumero { get; set; }
        public string Tipo { get; set; }
        public string Cliente { get; set; }
        public string Mesero { get; set; }
        public DateTime Enviado { get; set; }

        public string Titulo
        {
            get
            {
                if (MesaNumero.HasValue) return "Mesa " + MesaNumero;
                var tipo = Tipo == "Domicilio" ? "Domicilio" : "Llevar";
                return string.IsNullOrWhiteSpace(Cliente) ? tipo : tipo + " · " + Cliente;
            }
        }
        public List<LineaCocina> Lineas { get; set; } = new List<LineaCocina>();

        public string Clave
        {
            get { return OrdenId + "|" + Enviado.ToString("yyyyMMddHHmmss"); }
        }

        public List<int> Ids
        {
            get { return Lineas.Select(l => l.Id).ToList(); }
        }

        public bool SinEmpezar
        {
            get { return Lineas.Any(l => !l.Anulado && l.Estado == "Enviado"); }
        }

        public bool TodoAnulado
        {
            get { return Lineas.All(l => l.Anulado); }
        }

        public int Minutos
        {
            get { return Math.Max(0, (int)(DateTime.Now - Enviado).TotalMinutes); }
        }

        public static List<Comanda> Agrupar(IEnumerable<LineaCocina> lineas)
        {
            return lineas
                .GroupBy(l => new { l.OrdenId, l.Enviado })
                .Select(g => new Comanda
                {
                    OrdenId = g.Key.OrdenId,
                    Enviado = g.Key.Enviado,
                    MesaNumero = g.First().MesaNumero,
                    Tipo = g.First().Tipo,
                    Cliente = g.First().Cliente,
                    Mesero = g.First().Mesero,
                    Lineas = g.OrderBy(l => l.Anulado).ThenBy(l => l.Id).ToList()
                })
                .OrderBy(c => c.Enviado)
                .ToList();
        }
    }
}
