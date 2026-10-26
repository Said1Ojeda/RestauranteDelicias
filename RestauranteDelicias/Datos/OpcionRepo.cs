using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    // los grupos de opciones y a que platillos se le preguntan
    public static class OpcionRepo
    {
        // lo que se le pregunta a un platillo al pedirlo, en su orden y solo lo activo
        public static List<GrupoOpcion> DePlatillo(int platilloId)
        {
            using (var c = Conexion.Abrir())
                return DePlatillo(c, null, platilloId);
        }

        internal static List<GrupoOpcion> DePlatillo(IDbConnection c, IDbTransaction tx, int platilloId)
        {
            using (var r = c.QueryMultiple(
                @"SELECT g.Id, g.Nombre, g.Minimo, g.Maximo, g.SoloSiOpcionId, g.Activo, pg.Orden
                  FROM dbo.PlatilloGrupo pg
                  JOIN dbo.GrupoOpcion g ON g.Id = pg.GrupoId
                  WHERE pg.PlatilloId = @platilloId AND g.Activo = 1
                  ORDER BY pg.Orden, g.Nombre;
                  SELECT o.Id, o.GrupoId, o.Nombre, o.Precio, o.Orden, o.Activa
                  FROM dbo.Opcion o
                  JOIN dbo.PlatilloGrupo pg ON pg.GrupoId = o.GrupoId
                  WHERE pg.PlatilloId = @platilloId AND o.Activa = 1
                  ORDER BY o.Orden, o.Nombre;",
                new { platilloId }, tx))
            {
                var grupos = r.Read<GrupoOpcion>().ToList();
                var opciones = r.Read<Opcion>().ToList();
                foreach (var g in grupos) g.Opciones = opciones.Where(o => o.GrupoId == g.Id).ToList();
                return grupos.Where(g => g.Opciones.Count > 0).ToList();
            }
        }

        public static List<GrupoOpcion> Todos()
        {
            using (var c = Conexion.Abrir())
            using (var r = c.QueryMultiple(
                @"SELECT Id, Nombre, Minimo, Maximo, SoloSiOpcionId, Activo FROM dbo.GrupoOpcion ORDER BY Activo DESC, Nombre;
                  SELECT Id, GrupoId, Nombre, Precio, Orden, Activa FROM dbo.Opcion ORDER BY Orden, Nombre;"))
            {
                var grupos = r.Read<GrupoOpcion>().ToList();
                var opciones = r.Read<Opcion>().ToList();
                foreach (var g in grupos) g.Opciones = opciones.Where(o => o.GrupoId == g.Id).ToList();
                return grupos;
            }
        }

        public static List<int> GruposDePlatillo(int platilloId)
        {
            using (var c = Conexion.Abrir())
                return c.Query<int>("SELECT GrupoId FROM dbo.PlatilloGrupo WHERE PlatilloId = @platilloId ORDER BY Orden", new { platilloId }).ToList();
        }

        // las opciones que se quitan no se borran, se desactivan, asi las ventas viejas siguen diciendo que se pidio
        public static int Guardar(GrupoOpcion g)
        {
            Sesion.Exigir(Sesion.EsAdmin);
            g.Nombre = (g.Nombre ?? "").Trim();
            if (g.Nombre.Length < 2 || g.Nombre.Length > 60) throw new AvisoException("El nombre del grupo tiene que tener de 2 a 60 letras.");
            if (g.Minimo < 0 || g.Maximo < 1 || g.Minimo > g.Maximo || g.Maximo > 20)
                throw new AvisoException("Revisa cuántas se pueden escoger, el mínimo no puede ser mayor que el máximo.");
            var activas = g.Opciones.Where(o => o.Activa).ToList();
            if (activas.Count == 0) throw new AvisoException("El grupo necesita al menos una opción.");
            foreach (var o in g.Opciones)
            {
                o.Nombre = (o.Nombre ?? "").Trim();
                if (o.Nombre.Length < 1 || o.Nombre.Length > 60) throw new AvisoException("Cada opción necesita un nombre de hasta 60 letras.");
                if (o.Precio < 0 || o.Precio > 10000m) throw new AvisoException("El precio extra de " + o.Nombre + " no es válido.");
            }
            if (g.Opciones.GroupBy(o => o.Nombre.ToLowerInvariant()).Any(x => x.Count() > 1))
                throw new AvisoException("Hay dos opciones con el mismo nombre.");

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                if (g.SoloSiOpcionId.HasValue && g.Opciones.Any(o => o.Id == g.SoloSiOpcionId))
                    throw new AvisoException("Un grupo no puede depender de una de sus propias opciones.");

                if (g.Id == 0)
                    g.Id = c.ExecuteScalar<int>(
                        @"INSERT INTO dbo.GrupoOpcion (Nombre, Minimo, Maximo, SoloSiOpcionId, Activo)
                          OUTPUT INSERTED.Id VALUES (@Nombre, @Minimo, @Maximo, @SoloSiOpcionId, @Activo)", g, tx);
                else
                    c.Execute(
                        @"UPDATE dbo.GrupoOpcion SET Nombre = @Nombre, Minimo = @Minimo, Maximo = @Maximo,
                                 SoloSiOpcionId = @SoloSiOpcionId, Activo = @Activo
                          WHERE Id = @Id", g, tx);

                var orden = 1;
                foreach (var o in g.Opciones)
                {
                    o.GrupoId = g.Id;
                    o.Orden = orden++;
                    if (o.Id == 0)
                        o.Id = c.ExecuteScalar<int>(
                            @"INSERT INTO dbo.Opcion (GrupoId, Nombre, Precio, Orden, Activa)
                              OUTPUT INSERTED.Id VALUES (@GrupoId, @Nombre, @Precio, @Orden, @Activa)", o, tx);
                    else
                        c.Execute(
                            @"UPDATE dbo.Opcion SET Nombre = @Nombre, Precio = @Precio, Orden = @Orden, Activa = @Activa
                              WHERE Id = @Id AND GrupoId = @GrupoId", o, tx);
                }
                Bitacora.Registrar(c, tx, "Opciones", "Grupo " + g.Nombre + " con " + activas.Count + " opciones");
                tx.Commit();
                return g.Id;
            }
        }

        public static void AsignarAPlatillo(int platilloId, IList<int> grupos)
        {
            Sesion.Exigir(Sesion.EsAdmin);
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                c.Execute("DELETE FROM dbo.PlatilloGrupo WHERE PlatilloId = @platilloId", new { platilloId }, tx);
                for (int i = 0; i < grupos.Count; i++)
                    c.Execute("INSERT INTO dbo.PlatilloGrupo (PlatilloId, GrupoId, Orden) VALUES (@platilloId, @grupoId, @orden)",
                        new { platilloId, grupoId = grupos[i], orden = i + 1 }, tx);
                Bitacora.Registrar(c, tx, "Opciones", "Platillo " + platilloId + " con " + grupos.Count + " grupos");
                tx.Commit();
            }
        }
    }
}
