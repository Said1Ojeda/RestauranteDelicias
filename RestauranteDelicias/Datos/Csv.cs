using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RestauranteDelicias.Datos
{
    // csv a mano para no meter librerias, aguanta comas y comillas dentro de los textos
    public static class Csv
    {
        public static string Linea(params object[] campos)
        {
            return string.Join(",", campos.Select(c => Escapar(c == null ? "" : c.ToString())));
        }

        static string Escapar(string valor)
        {
            if (valor.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return valor;
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

        public static List<string> Separar(string linea)
        {
            var campos = new List<string>();
            var actual = new StringBuilder();
            bool enComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];
                if (enComillas)
                {
                    if (c == '"' && i + 1 < linea.Length && linea[i + 1] == '"')
                    {
                        actual.Append('"');
                        i++;
                    }
                    else if (c == '"')
                        enComillas = false;
                    else
                        actual.Append(c);
                }
                else if (c == '"')
                    enComillas = true;
                else if (c == ',')
                {
                    campos.Add(actual.ToString());
                    actual.Clear();
                }
                else
                    actual.Append(c);
            }
            campos.Add(actual.ToString());
            return campos;
        }
    }
}
