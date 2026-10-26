using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    public static class ClienteRepo
    {
        public static Cliente Buscar(string nit)
        {
            nit = Nit.Limpiar(nit);
            if (nit.Length == 0 || Nit.EsConsumidorFinal(nit)) return null;
            using (var c = Conexion.Abrir())
                return c.QuerySingleOrDefault<Cliente>("SELECT Nit, Nombre, Direccion FROM dbo.Cliente WHERE Nit = @nit", new { nit });
        }
    }
}
