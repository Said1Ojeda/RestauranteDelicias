using System;
using System.Collections.Generic;
using System.Linq;

namespace RestauranteDelicias.Modelos
{
    public class Orden
    {
        public const decimal PORCENTAJE_PROPINA = 0.10m;
        public const decimal IVA = 0.12m;

        public int Mesa { get; private set; }
        public DateTime Apertura { get; private set; }
        public List<LineaOrden> Lineas { get; private set; }
        public bool ConPropina { get; set; } = true;

        public Orden(int mesa)
        {
            Mesa = mesa;
            Apertura = DateTime.Now;
            Lineas = new List<LineaOrden>();
        }

        public bool Vacia
        {
            get { return Lineas.Count == 0; }
        }

        public decimal Subtotal
        {
            get { return Lineas.Sum(l => l.Subtotal); }
        }

        public decimal Propina
        {
            get { return ConPropina ? Math.Round(Subtotal * PORCENTAJE_PROPINA, 2) : 0m; }
        }

        public decimal Total
        {
            get { return Subtotal + Propina; }
        }

        // los precios del menu ya traen iva, aca solo se desglosa para el ticket
        // la propina no lleva iva
        public decimal IvaIncluido
        {
            get { return Math.Round(Subtotal - Subtotal / (1 + IVA), 2); }
        }

        public int Articulos
        {
            get { return Lineas.Sum(l => l.Cantidad); }
        }

        public void Agregar(Platillo platillo, int cantidad, string nota)
        {
            nota = (nota ?? "").Trim();

            // si ya esta el mismo platillo con la misma nota solo se suma
            var existente = Lineas.FirstOrDefault(l => l.Platillo.Id == platillo.Id
                && string.Equals(l.Nota, nota, StringComparison.OrdinalIgnoreCase));

            if (existente != null)
            {
                existente.Cantidad += cantidad;
                return;
            }

            Lineas.Add(new LineaOrden
            {
                Platillo = platillo,
                Cantidad = cantidad,
                Nota = nota,
                PrecioUnitario = platillo.Precio
            });
        }

        public void CambiarCantidad(LineaOrden linea, int diferencia)
        {
            linea.Cantidad += diferencia;
            if (linea.Cantidad <= 0)
                Lineas.Remove(linea);
        }

        public void Quitar(LineaOrden linea)
        {
            Lineas.Remove(linea);
        }
    }
}
