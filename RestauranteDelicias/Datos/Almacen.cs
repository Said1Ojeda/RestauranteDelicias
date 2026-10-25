using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Datos
{
    // todo se guarda en csv dentro de AppData\Local\RestauranteDelicias
    public static class Almacen
    {
        // ojo con la cultura, en es-GT el decimal sale con coma y rompe el csv
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static readonly string Carpeta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RestauranteDelicias");

        static string ArchivoMenu { get { return Path.Combine(Carpeta, "menu.csv"); } }
        static string ArchivoVentas { get { return Path.Combine(Carpeta, "ventas.csv"); } }

        const string ENCABEZADO_MENU = "id,nombre,categoria,precio,disponible";
        const string ENCABEZADO_VENTAS = "numero,fecha,mesa,articulos,subtotal,propina,total,forma_pago";

        public static readonly string[] Categorias = { "Entradas", "Platos fuertes", "Bebidas", "Postres" };

        static Almacen()
        {
            Directory.CreateDirectory(Carpeta);
        }

        public static List<Platillo> CargarMenu()
        {
            if (!File.Exists(ArchivoMenu))
            {
                var inicial = MenuInicial();
                GuardarMenu(inicial);
                return inicial;
            }

            var menu = new List<Platillo>();
            foreach (var linea in File.ReadAllLines(ArchivoMenu, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                var c = Csv.Separar(linea);
                if (c.Count < 5) continue; // fila mala, mejor saltarla que tronar al abrir

                menu.Add(new Platillo
                {
                    Id = int.Parse(c[0], Inv),
                    Nombre = c[1],
                    Categoria = c[2],
                    Precio = decimal.Parse(c[3], Inv),
                    Disponible = c[4] == "1"
                });
            }
            return menu;
        }

        public static void GuardarMenu(IEnumerable<Platillo> menu)
        {
            var lineas = new List<string> { ENCABEZADO_MENU };
            lineas.AddRange(menu.Select(p => Csv.Linea(
                p.Id, p.Nombre, p.Categoria, p.Precio.ToString("0.00", Inv), p.Disponible ? "1" : "0")));
            GuardarSeguro(ArchivoMenu, lineas);
        }

        public static Venta RegistrarVenta(Orden orden, string formaPago)
        {
            var venta = new Venta
            {
                Numero = SiguienteNumero(),
                Fecha = DateTime.Now,
                Mesa = orden.Mesa,
                Articulos = orden.Articulos,
                Subtotal = orden.Subtotal,
                Propina = orden.Propina,
                Total = orden.Total,
                FormaPago = formaPago
            };

            if (!File.Exists(ArchivoVentas))
                File.WriteAllText(ArchivoVentas, ENCABEZADO_VENTAS + Environment.NewLine, Encoding.UTF8);

            File.AppendAllText(ArchivoVentas, Csv.Linea(
                venta.Numero,
                venta.Fecha.ToString("yyyy-MM-dd HH:mm:ss", Inv),
                venta.Mesa,
                venta.Articulos,
                venta.Subtotal.ToString("0.00", Inv),
                venta.Propina.ToString("0.00", Inv),
                venta.Total.ToString("0.00", Inv),
                venta.FormaPago) + Environment.NewLine, Encoding.UTF8);

            return venta;
        }

        public static List<Venta> VentasDelDia(DateTime dia)
        {
            return TodasLasVentas().Where(v => v.Fecha.Date == dia.Date).ToList();
        }

        static List<Venta> TodasLasVentas()
        {
            var ventas = new List<Venta>();
            if (!File.Exists(ArchivoVentas)) return ventas;

            foreach (var linea in File.ReadAllLines(ArchivoVentas, Encoding.UTF8).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(linea)) continue;
                var c = Csv.Separar(linea);
                if (c.Count < 8) continue;

                ventas.Add(new Venta
                {
                    Numero = int.Parse(c[0], Inv),
                    Fecha = DateTime.ParseExact(c[1], "yyyy-MM-dd HH:mm:ss", Inv),
                    Mesa = int.Parse(c[2], Inv),
                    Articulos = int.Parse(c[3], Inv),
                    Subtotal = decimal.Parse(c[4], Inv),
                    Propina = decimal.Parse(c[5], Inv),
                    Total = decimal.Parse(c[6], Inv),
                    FormaPago = c[7]
                });
            }
            return ventas;
        }

        static int SiguienteNumero()
        {
            var ventas = TodasLasVentas();
            return ventas.Count == 0 ? 1 : ventas.Max(v => v.Numero) + 1;
        }

        public static void ExportarVentas(IEnumerable<Venta> ventas, string ruta)
        {
            var lineas = new List<string> { ENCABEZADO_VENTAS };
            lineas.AddRange(ventas.Select(v => Csv.Linea(
                v.Numero, v.Fecha.ToString("yyyy-MM-dd HH:mm:ss", Inv), v.Mesa, v.Articulos,
                v.Subtotal.ToString("0.00", Inv), v.Propina.ToString("0.00", Inv),
                v.Total.ToString("0.00", Inv), v.FormaPago)));

            // con bom para que excel agarre bien las tildes
            File.WriteAllLines(ruta, lineas, new UTF8Encoding(true));
        }

        // se escribe en un temporal y luego se reemplaza, si se va la luz a medias no queda el menu vacio
        static void GuardarSeguro(string ruta, IEnumerable<string> lineas)
        {
            var temporal = ruta + ".tmp";
            File.WriteAllLines(temporal, lineas, Encoding.UTF8);
            if (File.Exists(ruta))
                File.Replace(temporal, ruta, null);
            else
                File.Move(temporal, ruta);
        }

        static List<Platillo> MenuInicial()
        {
            int id = 1;
            Func<string, string, decimal, Platillo> p = (nombre, categoria, precio) =>
                new Platillo { Id = id++, Nombre = nombre, Categoria = categoria, Precio = precio };

            return new List<Platillo>
            {
                p("Guacamol con totopos", "Entradas", 35m),
                p("Chiles rellenos", "Entradas", 40m),
                p("Tostadas de frijol", "Entradas", 25m),
                p("Pepián de pollo", "Platos fuertes", 75m),
                p("Hilachas", "Platos fuertes", 70m),
                p("Churrasco con chirmol", "Platos fuertes", 95m),
                p("Pollo en jocón", "Platos fuertes", 72m),
                p("Fresco de rosa de jamaica", "Bebidas", 15m),
                p("Horchata", "Bebidas", 15m),
                p("Café de olla", "Bebidas", 12m),
                p("Agua pura", "Bebidas", 8m),
                p("Rellenitos de plátano", "Postres", 20m),
                p("Mole de plátano", "Postres", 22m),
                p("Champurradas con café", "Postres", 18m)
            };
        }
    }
}
