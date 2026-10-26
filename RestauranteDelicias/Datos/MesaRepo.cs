using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    public static class MesaRepo
    {
        // todas las mesas con su orden abierta, si tiene, en una sola ida a la base
        // esta consulta corre cada pocos segundos en cada caja, por eso es una sola
        public static List<MesaEstado> Estado()
        {
            using (var c = Conexion.Abrir())
                return c.Query<MesaEstado>(
                    @"SELECT m.Id, m.Numero, m.AreaId, a.Nombre AS AreaNombre, m.Capacidad, m.Forma,
                             o.Id AS OrdenId, o.Apertura, o.Personas,
                             ISNULL(t.Subtotal, 0) AS Subtotal,
                             ISNULL(t.Articulos, 0) AS Articulos,
                             ISNULL(t.Pendientes, 0) AS Pendientes,
                             ISNULL(t.Listos, 0) AS Listos
                      FROM dbo.Mesa m
                      JOIN dbo.Area a ON a.Id = m.AreaId
                      LEFT JOIN dbo.Orden o ON o.MesaId = m.Id AND o.Estado = N'Abierta'
                      OUTER APPLY (
                          SELECT SUM(d.Cantidad * d.PrecioUnitario) AS Subtotal,
                                 SUM(d.Cantidad) AS Articulos,
                                 SUM(CASE WHEN d.Estado = N'Nuevo' THEN 1 ELSE 0 END) AS Pendientes,
                                 SUM(CASE WHEN d.Estado = N'Listo' THEN 1 ELSE 0 END) AS Listos
                          FROM dbo.OrdenDetalle d
                          WHERE d.OrdenId = o.Id AND d.Anulado = 0
                      ) t
                      WHERE m.Activa = 1 AND a.Activa = 1
                      ORDER BY a.Orden, a.Nombre, m.Numero").ToList();
        }
    
        // lo que sigue es para que el admin arme el salon desde el programa
        public static List<Area> Areas()
        {
            using (var c = Conexion.Abrir())
                return c.Query<Area>("SELECT Id, Nombre, Orden, Activa FROM dbo.Area ORDER BY Activa DESC, Orden, Nombre").ToList();
        }

        public static List<Mesa> Todas()
        {
            using (var c = Conexion.Abrir())
                return c.Query<Mesa>(
                    @"SELECT m.Id, m.Numero, m.AreaId, a.Nombre AS AreaNombre, m.Capacidad, m.Forma, m.Activa
                      FROM dbo.Mesa m JOIN dbo.Area a ON a.Id = m.AreaId
                      ORDER BY m.Activa DESC, a.Orden, m.Numero").ToList();
        }

        public static void GuardarArea(Area area)
        {
            Sesion.Exigir(Sesion.EsAdmin);
            area.Nombre = (area.Nombre ?? "").Trim();
            if (area.Nombre.Length < 2 || area.Nombre.Length > 40) throw new AvisoException("El nombre del área tiene que tener de 2 a 40 letras.");

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Area WHERE Nombre = @Nombre AND Id <> @Id", area, tx) > 0)
                    throw new AvisoException("Ya hay un área que se llama " + area.Nombre + ".");
                if (!area.Activa && c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Mesa WHERE AreaId = @Id AND Activa = 1", area, tx) > 0)
                    throw new AvisoException("Para desactivar el área primero desactiva o pasa sus mesas a otra área.");

                if (area.Id == 0)
                    area.Id = c.ExecuteScalar<int>(
                        @"INSERT INTO dbo.Area (Nombre, Orden, Activa)
                          OUTPUT INSERTED.Id VALUES (@Nombre, (SELECT ISNULL(MAX(Orden), 0) + 1 FROM dbo.Area), @Activa)", area, tx);
                else
                    c.Execute("UPDATE dbo.Area SET Nombre = @Nombre, Activa = @Activa WHERE Id = @Id", area, tx);
                Bitacora.Registrar(c, tx, "Área", area.Nombre + (area.Activa ? "" : ", desactivada"));
                tx.Commit();
            }
        }

        // una mesa con cuenta abierta no se desactiva, y el numero no se repite ni con las desactivadas
        public static void GuardarMesa(Mesa mesa)
        {
            Sesion.Exigir(Sesion.EsAdmin);
            if (mesa.Numero < 1 || mesa.Numero > 9999) throw new AvisoException("El número de mesa tiene que ser de 1 a 9999.");
            if (mesa.Capacidad < 1 || mesa.Capacidad > 30) throw new AvisoException("La capacidad tiene que ser de 1 a 30 personas.");
            if (mesa.Forma != "Cuadrada" && mesa.Forma != "Redonda" && mesa.Forma != "Barra") throw new AvisoException("Escoge la forma de la mesa.");

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                var otra = c.QuerySingleOrDefault<Mesa>(
                    "SELECT Id, Activa FROM dbo.Mesa WHERE Numero = @Numero AND Id <> @Id", mesa, tx);
                if (otra != null)
                    throw new AvisoException(otra.Activa
                        ? "Ya hay una mesa con el número " + mesa.Numero + "."
                        : "La mesa " + mesa.Numero + " existe pero está desactivada. Búscala en la lista y actívala.");
                if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Area WHERE Id = @AreaId AND Activa = 1", mesa, tx) == 0)
                    throw new AvisoException("Escoge un área activa.");
                if (!mesa.Activa && mesa.Id != 0 &&
                    c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Orden WHERE MesaId = @Id AND Estado = N'Abierta'", mesa, tx) > 0)
                    throw new AvisoException("La mesa " + mesa.Numero + " tiene una cuenta abierta, cóbrala antes de desactivarla.");

                if (mesa.Id == 0)
                    mesa.Id = c.ExecuteScalar<int>(
                        @"INSERT INTO dbo.Mesa (AreaId, Numero, Capacidad, Forma, Activa)
                          OUTPUT INSERTED.Id VALUES (@AreaId, @Numero, @Capacidad, @Forma, @Activa)", mesa, tx);
                else
                    c.Execute(
                        "UPDATE dbo.Mesa SET AreaId = @AreaId, Numero = @Numero, Capacidad = @Capacidad, Forma = @Forma, Activa = @Activa WHERE Id = @Id",
                        mesa, tx);
                Bitacora.Registrar(c, tx, "Mesa", "Mesa " + mesa.Numero + (mesa.Activa ? "" : ", desactivada"));
                tx.Commit();
            }
        }
    }
}
