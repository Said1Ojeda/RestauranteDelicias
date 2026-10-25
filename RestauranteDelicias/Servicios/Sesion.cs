using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Servicios
{
    // quien esta usando el programa en esta compu y que puede hacer
    public static class Sesion
    {
        public static Usuario Usuario { get; private set; }
        public static int? TurnoId { get; set; }

        public static int? Id
        {
            get { return Usuario == null ? (int?)null : Usuario.Id; }
        }

        public static string Nombre
        {
            get { return Usuario == null ? "" : Usuario.Nombre; }
        }

        public static void Iniciar(Usuario u, int? turnoId)
        {
            Usuario = u;
            TurnoId = turnoId;
        }

        public static void Cerrar()
        {
            Usuario = null;
            TurnoId = null;
        }

        static bool Es(params string[] roles)
        {
            if (Usuario == null) return false;
            foreach (var r in roles) if (Usuario.Rol == r) return true;
            return false;
        }

        public static bool EsAdmin { get { return Es(Roles.Admin); } }
        public static bool PuedeCobrar { get { return Es(Roles.Admin, Roles.Cajero); } }
        public static bool PuedeVerVentas { get { return Es(Roles.Admin, Roles.Cajero); } }
        public static bool PuedeTomarOrdenes { get { return Es(Roles.Admin, Roles.Cajero, Roles.Mesero); } }
    }
}
