using System.Linq;

namespace RestauranteDelicias.Servicios
{
    // NIT de Guatemala, el ultimo digito es de verificacion con modulo 11 y puede ser K
    // tambien se acepta el CUI del DPI, que son 13 numeros, para quien no tiene NIT
    public static class Nit
    {
        public const string CONSUMIDOR_FINAL = "CF";

        // deja el NIT como se guarda, sin guiones ni espacios y con la K en mayuscula
        public static string Limpiar(string nit)
        {
            var limpio = new string((nit ?? "").Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray()).ToUpperInvariant();
            return limpio == "C/F" || limpio == "C.F." ? CONSUMIDOR_FINAL : limpio;
        }

        public static bool EsConsumidorFinal(string nit)
        {
            return Limpiar(nit) == CONSUMIDOR_FINAL;
        }

        public static bool EsCui(string nit)
        {
            var limpio = Limpiar(nit);
            return limpio.Length == 13 && limpio.All(char.IsDigit);
        }

        public static bool Valido(string nit)
        {
            var limpio = Limpiar(nit);
            if (limpio == CONSUMIDOR_FINAL || EsCui(limpio)) return true;
            if (limpio.Length < 2 || limpio.Length > 12) return false;

            var cuerpo = limpio.Substring(0, limpio.Length - 1);
            var verificador = limpio[limpio.Length - 1];
            if (!cuerpo.All(char.IsDigit) || !(char.IsDigit(verificador) || verificador == 'K')) return false;

            int suma = 0;
            for (int i = 0; i < cuerpo.Length; i++)
                suma += (cuerpo[cuerpo.Length - 1 - i] - '0') * (i + 2);
            var esperado = (11 - suma % 11) % 11;
            return esperado == 10 ? verificador == 'K' : verificador == (char)('0' + esperado);
        }
    }
}
