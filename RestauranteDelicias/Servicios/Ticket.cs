using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Servicios
{
    public static class Ticket
    {
        // 40 columnas es lo que cabe en un rollo de 80mm con letra de 9
        const int ANCHO = 40;

        public static string Texto(Orden orden, int numeroTicket, DateTime fecha, List<Pago> pagos)
        {
            var t = new StringBuilder();
            t.AppendLine(Centrar(Configuracion.Nombre.ToUpper()));
            Opcional(t, Configuracion.Texto("Restaurante.Lema"));
            Opcional(t, Configuracion.Texto("Restaurante.Direccion"));
            Opcional(t, Configuracion.Texto("Restaurante.Telefono"), "Tel. ");
            Opcional(t, Configuracion.Texto("Restaurante.Nit"), "NIT ");
            t.AppendLine(new string('-', ANCHO));
            t.AppendLine("Ticket No. " + numeroTicket.ToString("00000"));
            t.AppendLine("Fecha  " + fecha.ToString("dd/MM/yyyy HH:mm"));
            t.AppendLine(orden.MesaNumero.HasValue ? "Mesa   " + orden.MesaNumero + "   Personas " + orden.Personas : "Para llevar");
            t.AppendLine(new string('-', ANCHO));

            // las lineas iguales se juntan, en el ticket no importa si se pidieron en dos tandas
            var grupos = orden.Activas
                .GroupBy(l => new { l.Nombre, l.PrecioUnitario, Nota = l.Nota ?? "" })
                .Select(g => new { g.Key.Nombre, g.Key.Nota, Cantidad = g.Sum(x => x.Cantidad), Subtotal = g.Sum(x => x.Subtotal) });

            foreach (var l in grupos)
            {
                // nombre cortado para que el precio no se pase a la otra linea
                string izq = l.Cantidad + " " + l.Nombre;
                string der = Q(l.Subtotal);
                int espacio = ANCHO - der.Length - 1;
                if (izq.Length > espacio) izq = izq.Substring(0, espacio);
                t.AppendLine(izq.PadRight(espacio) + " " + der);

                if (!string.IsNullOrEmpty(l.Nota))
                    t.AppendLine("   * " + l.Nota);
            }

            t.AppendLine(new string('-', ANCHO));
            t.AppendLine(Fila("Subtotal", Q(orden.Subtotal)));
            if (orden.Descuento > 0)
                t.AppendLine(Fila("Descuento", "-" + Q(orden.Descuento)));
            if (orden.Propina > 0)
                t.AppendLine(Fila("Propina " + orden.PorcentajePropina.ToString("0.##") + "%", Q(orden.Propina)));
            t.AppendLine(Fila("TOTAL", Q(orden.Total)));
            t.AppendLine(Fila("  IVA incluido", Q(orden.IvaIncluido(Configuracion.Iva))));
            t.AppendLine(new string('-', ANCHO));

            foreach (var p in pagos)
            {
                t.AppendLine(Fila(p.Forma, Q(p.Monto)));
                if (p.Forma == "Efectivo" && p.Recibido.HasValue && p.Recibido.Value > p.Monto)
                {
                    t.AppendLine(Fila("  Recibido", Q(p.Recibido.Value)));
                    t.AppendLine(Fila("  Vuelto", Q(p.Recibido.Value - p.Monto)));
                }
                if (!string.IsNullOrEmpty(p.Referencia))
                    t.AppendLine(Fila("  Ref.", p.Referencia));
            }

            t.AppendLine();
            t.AppendLine(Centrar(Configuracion.Texto("Ticket.Pie", "Gracias por su visita")));
            return t.ToString();
        }

        static void Opcional(StringBuilder t, string valor, string prefijo = "")
        {
            if (!string.IsNullOrWhiteSpace(valor)) t.AppendLine(Centrar(prefijo + valor));
        }

        public static void Imprimir(string texto, IWin32Window dueno)
        {
            var renglones = new Queue<string>(texto.Replace("\r", "").Split('\n'));
            var fuente = new Font("Consolas", 9);

            var doc = new PrintDocument();
            doc.DocumentName = "Ticket";
            doc.PrintPage += (s, e) =>
            {
                float y = e.MarginBounds.Top;
                float alto = fuente.GetHeight(e.Graphics);
                while (renglones.Count > 0)
                {
                    if (y + alto > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true;
                        return;
                    }
                    e.Graphics.DrawString(renglones.Dequeue(), fuente, Brushes.Black, e.MarginBounds.Left, y);
                    y += alto;
                }
            };

            // la vista previa dibuja todo una vez, por eso se vuelve a llenar la cola antes de imprimir
            doc.BeginPrint += (s, e) =>
            {
                renglones.Clear();
                foreach (var r in texto.Replace("\r", "").Split('\n')) renglones.Enqueue(r);
            };

            using (var vista = new PrintPreviewDialog())
            {
                vista.Document = doc;
                vista.Width = 520;
                vista.Height = 720;
                vista.ShowDialog(dueno);
            }
            fuente.Dispose();
        }

        public static string Q(decimal monto)
        {
            return "Q " + monto.ToString("#,##0.00");
        }

        static string Fila(string izq, string der)
        {
            if (der.Length > ANCHO - 6) der = der.Substring(0, ANCHO - 6);
            return izq.PadRight(Math.Max(izq.Length + 1, ANCHO - der.Length)) + der;
        }

        static string Centrar(string texto)
        {
            if (texto.Length > ANCHO) texto = texto.Substring(0, ANCHO);
            int izq = Math.Max(0, (ANCHO - texto.Length) / 2);
            return new string(' ', izq) + texto;
        }
    }
}
