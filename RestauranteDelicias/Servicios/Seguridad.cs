using System;
using System.Security.Cryptography;

namespace RestauranteDelicias.Servicios
{
    // las claves y los pin nunca se guardan tal cual, solo el hash con su sal
    public static class Seguridad
    {
        const int ITERACIONES = 100000;
        const int LARGO_SAL = 16;
        const int LARGO_HASH = 32;

        public static void Hashear(string clave, out byte[] hash, out byte[] sal)
        {
            sal = new byte[LARGO_SAL];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(sal);
            hash = Derivar(clave, sal);
        }

        public static bool Verificar(string clave, byte[] hash, byte[] sal)
        {
            if (hash == null || sal == null || string.IsNullOrEmpty(clave)) return false;
            return IgualesEnTiempoFijo(Derivar(clave, sal), hash);
        }

        static byte[] Derivar(string clave, byte[] sal)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(clave, sal, ITERACIONES, HashAlgorithmName.SHA256))
                return pbkdf2.GetBytes(LARGO_HASH);
        }

        // se compara todo aunque ya se sepa que no son iguales, asi no se puede adivinar midiendo el tiempo
        static bool IgualesEnTiempoFijo(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int dif = 0;
            for (int i = 0; i < a.Length; i++) dif |= a[i] ^ b[i];
            return dif == 0;
        }

        public static string ValidarClave(string clave)
        {
            if (string.IsNullOrEmpty(clave) || clave.Length < 6) return "La contraseña debe tener al menos 6 caracteres.";
            return null;
        }

        public static string ValidarPin(string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6) return "El PIN debe tener de 4 a 6 números.";
            foreach (var c in pin) if (!char.IsDigit(c)) return "El PIN solo lleva números.";
            if (pin == "1234" || pin == "0000" || pin == "123456" || new string(pin[0], pin.Length) == pin)
                return "Ese PIN es muy fácil de adivinar, escoge otro.";
            return null;
        }
    }
}
