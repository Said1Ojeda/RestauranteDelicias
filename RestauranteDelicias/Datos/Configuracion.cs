using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dapper;

namespace RestauranteDelicias.Datos
{
    // nombre del restaurante, nit, porcentajes, se lee una vez y queda en memoria
    public static class Configuracion
    {
        static Dictionary<string, string> valores = new Dictionary<string, string>();

        public static void Cargar()
        {
            using (var c = Conexion.Abrir())
                valores = c.Query("SELECT Clave, Valor FROM dbo.Configuracion")
                    .ToDictionary(r => (string)r.Clave, r => (string)r.Valor);
        }

        public static string Texto(string clave, string porDefecto = "")
        {
            string v;
            return valores.TryGetValue(clave, out v) ? v : porDefecto;
        }

        public static decimal Numero(string clave, decimal porDefecto)
        {
            decimal d;
            return decimal.TryParse(Texto(clave), NumberStyles.Number, CultureInfo.InvariantCulture, out d) ? d : porDefecto;
        }

        public static string Nombre { get { return Texto("Restaurante.Nombre", "Restaurante Delicias"); } }
        public static decimal Propina { get { return Numero("Propina.Porcentaje", 10m); } }
        public static decimal Iva { get { return Numero("Iva.Porcentaje", 12m); } }
    }
}
