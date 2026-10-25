using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Servicios
{
    public static class Ticket
    {
        // 40 columnas es lo que cabe en un rollo de 80mm con letra de 9
        const int ANCHO = 40;

        public static string Texto(Orden orden, Venta venta, decimal recibido)
        {
            var t = new StringBuilder();
            t.AppendLine(Centrar("RESTAURANTE DELICIAS"));
            t.AppendLine(Centrar("Comida chapina"));
            t.AppendLine(new string('-', ANCHO));
            t.AppendLine("Ticket No. " + venta.Numero.ToString("00000"));
            t.AppendLine("Fecha  " + venta.Fecha.ToString("dd/MM/yyyy HH:mm"));
            t.AppendLine("Mesa   " + venta.Mesa);
            t.AppendLine(new string('-', ANCHO));

            foreach (var l in orden.Lineas)
            {
                // nombre cortado para que el precio no se pase a la otra linea
                string izq = l.Cantidad + " " + l.Platillo.Nombre;
                string der = Q(l.Subtotal);
                int espacio = ANCHO - der.Length - 1;
                if (izq.Length > espacio) izq = izq.Substring(0, espacio);
                t.AppendLine(izq.PadRight(espacio) + " " + der);

                if (!string.IsNullOrEmpty(l.Nota))
                    t.AppendLine("   * " + l.Nota);
            }

            t.AppendLine(new string('-', ANCHO));
            t.AppendLine(Fila("Subtotal", Q(orden.Subtotal)));
            if (orden.Propina > 0)
                t.AppendLine(Fila("Propina 10%", Q(orden.Propina)));
            t.AppendLine(Fila("TOTAL", Q(orden.Total)));
            t.AppendLine(Fila("  IVA incluido", Q(orden.IvaIncluido)));
            t.AppendLine(new string('-', ANCHO));
            t.AppendLine(Fila("Pago", venta.FormaPago));

            if (venta.FormaPago == "Efectivo")
            {
                t.AppendLine(Fila("Recibido", Q(recibido)));
                t.AppendLine(Fila("Vuelto", Q(recibido - orden.Total)));
            }

            t.AppendLine();
            t.AppendLine(Centrar("Gracias por su visita"));
            return t.ToString();
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
            return izq.PadRight(ANCHO - der.Length) + der;
        }

        static string Centrar(string texto)
        {
            int izq = Math.Max(0, (ANCHO - texto.Length) / 2);
            return new string(' ', izq) + texto;
        }
    }
}
