﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RestauranteDelicias.Datos;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Pantallas;

namespace RestauranteDelicias.Servicios
{
    // el ticket se arma como renglones con una marca al inicio que dice como se dibuja
    // # titulo grande     ^ centrado     - linea     * fila grande     ~ texto chico
    // los renglones con tabulador llevan el texto a la izquierda y el monto a la derecha
    public static class Ticket
    {
        // lo que se puede imprimir en un rollo de 80mm, en centesimas de pulgada
        const float ANCHO_ROLLO = 280f;

        public static string Texto(Orden orden, int numeroTicket, DateTime fecha, List<Pago> pagos)
        {
            var t = new StringBuilder();
            t.AppendLine("#" + Configuracion.Nombre);
            Opcional(t, Configuracion.Texto("Restaurante.Lema"));
            Opcional(t, Configuracion.Texto("Restaurante.Direccion"));
            Opcional(t, Configuracion.Texto("Restaurante.Telefono"), "Tel. ");
            Opcional(t, Configuracion.Texto("Restaurante.Nit"), "NIT ");
            t.AppendLine("-");
            t.AppendLine("Ticket\tNo. " + numeroTicket.ToString("00000"));
            t.AppendLine("Fecha\t" + fecha.ToString("dd/MM/yyyy HH:mm"));
            Encabezado(t, orden);

            // las lineas iguales se juntan, en el ticket no importa si se pidieron en dos tandas
            var grupos = orden.Activas
                .GroupBy(l => new { l.Nombre, l.PrecioUnitario, Nota = l.Nota ?? "" })
                .Select(g => new { g.Key.Nombre, g.Key.Nota, Cantidad = g.Sum(x => x.Cantidad), Subtotal = g.Sum(x => x.Subtotal) });

            foreach (var l in grupos)
            {
                t.AppendLine(l.Cantidad + "  " + Limpio(l.Nombre) + "\t" + Q(l.Subtotal));
                if (!string.IsNullOrEmpty(l.Nota))
                    t.AppendLine("~" + Limpio(l.Nota));
            }

            t.AppendLine("-");
            t.AppendLine("Subtotal\t" + Q(orden.Subtotal));
            if (orden.Descuento > 0)
                t.AppendLine("Descuento\t-" + Q(orden.Descuento));
            if (orden.Propina > 0)
                t.AppendLine("Propina " + orden.PorcentajePropina.ToString("0.##") + "%\t" + Q(orden.Propina));
            t.AppendLine("*TOTAL\t" + Q(orden.Total));
            t.AppendLine("~IVA incluido " + Q(orden.IvaIncluido(Configuracion.Iva)));
            t.AppendLine("-");

            foreach (var p in pagos) LineaDePago(t, p);

            t.AppendLine();
            t.AppendLine("^" + Configuracion.Texto("Ticket.Pie", "Gracias por su visita"));
            return t.ToString();
        }

        // cuando se paga una parte se entrega este comprobante, el ticket con numero sale con el ultimo pago
        public static string Comprobante(Orden orden, Pago pago, IEnumerable<LineaOrden> platillos, decimal saldo)
        {
            var t = new StringBuilder();
            t.AppendLine("#" + Configuracion.Nombre);
            t.AppendLine("^COMPROBANTE DE PAGO");
            t.AppendLine("-");
            t.AppendLine("Fecha\t" + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            Encabezado(t, orden);

            var lista = platillos.ToList();
            if (lista.Count > 0)
            {
                foreach (var l in lista)
                    t.AppendLine(l.Cantidad + "  " + Limpio(l.Nombre) + "\t" + Q(l.PrecioUnitario * l.Cantidad));
                t.AppendLine("~Con su parte de propina y descuento");
                t.AppendLine("-");
            }

            t.AppendLine("*PAGADO\t" + Q(pago.Monto));
            LineaDePago(t, pago);
            t.AppendLine("-");
            t.AppendLine("Total de la cuenta\t" + Q(orden.Total));
            t.AppendLine("*SALDO\t" + Q(saldo));
            t.AppendLine();
            t.AppendLine("^El ticket final sale con el último pago");
            return t.ToString();
        }

        static void Encabezado(StringBuilder t, Orden orden)
        {
            if (orden.MesaNumero.HasValue)
                t.AppendLine("Mesa " + orden.MesaNumero + "\t" + orden.Personas + " personas");
            else
            {
                t.AppendLine((orden.Tipo == "Domicilio" ? "A domicilio" : "Para llevar") + "\t" + Limpio(orden.Cliente));
                if (!string.IsNullOrWhiteSpace(orden.Telefono)) t.AppendLine("Teléfono\t" + Limpio(orden.Telefono));
                if (!string.IsNullOrWhiteSpace(orden.Direccion)) t.AppendLine("~" + Limpio(orden.Direccion));
            }
            if (!string.IsNullOrEmpty(orden.Mesero)) t.AppendLine("Atendió\t" + orden.Mesero);
            t.AppendLine("-");
        }

        static void LineaDePago(StringBuilder t, Pago p)
        {
            t.AppendLine((string.IsNullOrEmpty(p.Parte) ? "" : Limpio(p.Parte) + " · ") + p.Forma + "\t" + Q(p.Monto));
            if (p.Forma == "Efectivo" && p.Recibido.HasValue && p.Recibido.Value > p.Monto)
                t.AppendLine("~Recibido " + Q(p.Recibido.Value) + "   Vuelto " + Q(p.Recibido.Value - p.Monto));
            if (!string.IsNullOrEmpty(p.Referencia))
                t.AppendLine("~Ref. " + Limpio(p.Referencia));
        }

        public static string Reimpresion(string texto)
        {
            return texto + Environment.NewLine + "^** REIMPRESIÓN **";
        }

        // corte de caja al cerrar el turno, para guardarlo junto con el efectivo
        public static string Corte(Turno turno, ResumenTurno r, string notas)
        {
            var t = new StringBuilder();
            t.AppendLine("#" + Configuracion.Nombre);
            t.AppendLine("^CORTE DE CAJA");
            t.AppendLine("-");
            t.AppendLine("Cajero\t" + turno.Cajero);
            t.AppendLine("Abrió\t" + turno.Apertura.ToString("dd/MM/yyyy HH:mm"));
            if (turno.Cierre.HasValue) t.AppendLine("Cerró\t" + turno.Cierre.Value.ToString("dd/MM/yyyy HH:mm"));
            t.AppendLine("-");
            t.AppendLine("Ventas\t" + r.Ventas);
            t.AppendLine("Efectivo\t" + Q(r.Efectivo));
            t.AppendLine("Tarjeta\t" + Q(r.Tarjeta));
            t.AppendLine("Transferencia\t" + Q(r.Transferencia));
            t.AppendLine("*TOTAL COBRADO\t" + Q(r.Total));
            t.AppendLine("~Propinas incluidas " + Q(r.Propinas));
            t.AppendLine("-");
            t.AppendLine("Fondo inicial\t" + Q(r.MontoInicial));
            t.AppendLine("Debe haber\t" + Q(turno.Esperado ?? r.EsperadoEnCaja));
            if (turno.EfectivoContado.HasValue)
            {
                var dif = turno.EfectivoContado.Value - (turno.Esperado ?? r.EsperadoEnCaja);
                t.AppendLine("Contado\t" + Q(turno.EfectivoContado.Value));
                t.AppendLine("*Diferencia\t" + (dif < 0 ? "-" : "") + Q(Math.Abs(dif)));
            }
            if (!string.IsNullOrWhiteSpace(notas))
            {
                t.AppendLine("-");
                t.AppendLine("~Notas " + Limpio(notas));
            }
            t.AppendLine();
            t.AppendLine();
            t.AppendLine("Firma ______________________________");
            return t.ToString();
        }

        static void Opcional(StringBuilder t, string valor, string prefijo = "")
        {
            if (!string.IsNullOrWhiteSpace(valor)) t.AppendLine("^" + prefijo + Limpio(valor));
        }

        // lo que escribe el usuario no puede romper el formato del ticket
        static string Limpio(string texto)
        {
            return (texto ?? "").Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }

        static string[] Renglones(string texto)
        {
            return texto.Replace("\r", "").TrimEnd('\n').Split('\n');
        }

        // dibuja los renglones desde el indice que se le pasa y regresa hasta donde llego
        // sirve igual para la vista en pantalla que para la impresora
        public static int Dibujar(Graphics g, string[] renglones, int desde, RectangleF area, out float alto)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            float y = area.Top;
            using (var izquierda = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            using (var derecha = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap })
            using (var centro = new StringFormat { Alignment = StringAlignment.Center })
            using (var titulo = new Font("Segoe UI Semibold", 13f))
            using (var normal = new Font("Segoe UI", 8.5f))
            using (var fuerte = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var chica = new Font("Segoe UI", 7.5f))
            using (var gris = new SolidBrush(Color.FromArgb(90, 90, 90)))
            using (var linea = new Pen(Color.FromArgb(150, 150, 150), 1f) { DashStyle = DashStyle.Dash })
            {
                int i = desde;
                for (; i < renglones.Length; i++)
                {
                    var r = renglones[i];
                    Font f = normal;
                    if (r.StartsWith("#")) f = titulo;
                    else if (r.StartsWith("*")) f = fuerte;
                    else if (r.StartsWith("~")) f = chica;
                    float h = r == "-" ? 10f : (r.Length == 0 ? f.GetHeight(g) * 0.6f : f.GetHeight(g) + 2f);
                    if (r.StartsWith("#") || r.StartsWith("^"))
                        h = g.MeasureString(r.Substring(1), f, (int)area.Width, centro).Height + 2f;
                    else if (r.StartsWith("~"))
                        h = g.MeasureString(r.Substring(1), f, (int)(area.Width - 12f)).Height + 1f;

                    if (y + h > area.Bottom && i > desde) break;

                    if (r == "-")
                        g.DrawLine(linea, area.Left, y + h / 2, area.Right, y + h / 2);
                    else if (r.StartsWith("#") || r.StartsWith("^"))
                        g.DrawString(r.Substring(1), f, Brushes.Black, new RectangleF(area.Left, y, area.Width, h), centro);
                    else if (r.StartsWith("~"))
                        g.DrawString(r.Substring(1), f, gris, new RectangleF(area.Left + 12f, y, area.Width - 12f, h));
                    else
                    {
                        var texto = r.StartsWith("*") ? r.Substring(1) : r;
                        var partes = texto.Split('\t');
                        if (partes.Length > 1)
                        {
                            var der = partes[1];
                            var anchoDer = g.MeasureString(der, f).Width;
                            g.DrawString(der, f, Brushes.Black, new RectangleF(area.Left, y, area.Width, h), derecha);
                            g.DrawString(partes[0], f, Brushes.Black, new RectangleF(area.Left, y, Math.Max(10f, area.Width - anchoDer - 6f), h), izquierda);
                        }
                        else
                            g.DrawString(texto, f, Brushes.Black, new RectangleF(area.Left, y, area.Width, h), izquierda);
                    }
                    y += h;
                }
                alto = y - area.Top;
                return i;
            }
        }

        public static float Medir(Graphics g, string texto, float ancho)
        {
            float alto;
            Dibujar(g, Renglones(texto), 0, new RectangleF(0, 0, ancho, 100000f), out alto);
            return alto;
        }

        public static PrintDocument Documento(string texto, string nombre)
        {
            var renglones = Renglones(texto);
            int siguiente = 0;
            var doc = new PrintDocument { DocumentName = nombre };
            // margenes chicos, los de una pulgada no caben en un rollo termico
            doc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
            doc.BeginPrint += (s, e) => siguiente = 0;
            doc.PrintPage += (s, e) =>
            {
                var m = e.MarginBounds;
                var area = new RectangleF(m.Left, m.Top, Math.Min(m.Width, ANCHO_ROLLO), m.Height);
                float alto;
                siguiente = Dibujar(e.Graphics, renglones, siguiente, area, out alto);
                e.HasMorePages = siguiente < renglones.Length;
            };
            return doc;
        }

        public static void Imprimir(string texto, IWin32Window dueno)
        {
            using (var f = new FrmTicket(texto))
                f.ShowDialog(dueno);
        }

        public static string Q(decimal monto)
        {
            return "Q " + monto.ToString("#,##0.00");
        }
    }
}
