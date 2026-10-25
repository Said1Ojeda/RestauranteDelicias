using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;

namespace RestauranteDelicias.Datos
{
    public static class MenuRepo
    {
        public static List<Categoria> Categorias(bool soloActivas = true)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Categoria>(
                    @"SELECT Id, Nombre, Orden, Estacion, Color, Activa
                      FROM dbo.Categoria
                      WHERE Activa = 1 OR @todas = 1
                      ORDER BY Orden, Nombre", new { todas = soloActivas ? 0 : 1 }).ToList();
        }

        public static List<Platillo> Platillos(bool soloActivos = true)
        {
            using (var c = Conexion.Abrir())
                return c.Query<Platillo>(
                    @"SELECT p.Id, p.CategoriaId, cat.Nombre AS CategoriaNombre, cat.Color AS CategoriaColor, p.Nombre, p.Descripcion,
                             p.Precio, p.Disponible, p.Activo
                      FROM dbo.Platillo p
                      JOIN dbo.Categoria cat ON cat.Id = p.CategoriaId
                      WHERE (p.Activo = 1 AND cat.Activa = 1) OR @todos = 1
                      ORDER BY cat.Orden, p.Nombre", new { todos = soloActivos ? 0 : 1 }).ToList();
        }

        public static void GuardarPlatillo(Platillo p)
        {
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                if (p.Id == 0)
                {
                    p.Id = c.ExecuteScalar<int>(
                        @"INSERT INTO dbo.Platillo (CategoriaId, Nombre, Descripcion, Precio, Disponible, Activo)
                          OUTPUT INSERTED.Id
                          VALUES (@CategoriaId, @Nombre, @Descripcion, @Precio, @Disponible, @Activo)", p, tx);
                    Bitacora.Registrar(c, tx, "Platillo nuevo", p.Nombre + " Q" + p.Precio.ToString("0.00"));
                }
                else
                {
                    var antes = c.QuerySingleOrDefault<decimal?>("SELECT Precio FROM dbo.Platillo WHERE Id = @Id", p, tx);
                    c.Execute(
                        @"UPDATE dbo.Platillo
                          SET CategoriaId = @CategoriaId, Nombre = @Nombre, Descripcion = @Descripcion,
                              Precio = @Precio, Disponible = @Disponible, Activo = @Activo, Modificado = SYSDATETIME()
                          WHERE Id = @Id", p, tx);
                    // los cambios de precio quedan registrados, es lo primero que preguntan cuando algo no cuadra
                    if (antes.HasValue && antes.Value != p.Precio)
                        Bitacora.Registrar(c, tx, "Cambio de precio", p.Nombre + " de Q" + antes.Value.ToString("0.00") + " a Q" + p.Precio.ToString("0.00"));
                }
                tx.Commit();
            }
        }

        public static int CrearCategoria(string nombre, string estacion)
        {
            using (var c = Conexion.Abrir())
                return c.ExecuteScalar<int>(
                    @"INSERT INTO dbo.Categoria (Nombre, Orden, Estacion)
                      OUTPUT INSERTED.Id
                      VALUES (@nombre, (SELECT ISNULL(MAX(Orden), 0) + 1 FROM dbo.Categoria), @estacion)",
                    new { nombre, estacion });
        }
    }
}
