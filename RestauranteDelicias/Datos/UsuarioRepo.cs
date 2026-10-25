using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    public static class UsuarioRepo
    {
        const int MAX_FALLOS = 5;
        static readonly TimeSpan BLOQUEO = TimeSpan.FromSeconds(60);

        // intentos fallidos en esta compu, para frenar al que se pone a probar claves
        static readonly ConcurrentDictionary<string, Tuple<int, DateTime>> fallos = new ConcurrentDictionary<string, Tuple<int, DateTime>>();

        const string CAMPOS = @"Id, Nombre, Usuario AS Login, Rol, Activo,
                                CAST(CASE WHEN PinHash IS NULL THEN 0 ELSE 1 END AS BIT) AS TienePin, UltimoAcceso";

        public class Credencial
        {
            public int Id { get; set; }
            public byte[] ClaveHash { get; set; }
            public byte[] ClaveSal { get; set; }
            public byte[] PinHash { get; set; }
            public byte[] PinSal { get; set; }
        }

        public static bool HayAdmin()
        {
            using (var c = Conexion.Abrir())
                return c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Usuario WHERE Activo = 1 AND Rol = N'Admin'") > 0;
        }

        public static List<Usuario> Listar()
        {
            using (var c = Conexion.Abrir())
                return c.Query<Usuario>("SELECT " + CAMPOS + " FROM dbo.Usuario ORDER BY Activo DESC, Nombre").ToList();
        }

        public static Usuario Obtener(int id)
        {
            using (var c = Conexion.Abrir())
                return c.QuerySingleOrDefault<Usuario>("SELECT " + CAMPOS + " FROM dbo.Usuario WHERE Id = @id", new { id });
        }

        public static Usuario Entrar(string login, string clave)
        {
            login = (login ?? "").Trim().ToLowerInvariant();
            RevisarBloqueo(login);

            Credencial cred;
            using (var c = Conexion.Abrir())
                cred = c.QuerySingleOrDefault<Credencial>(
                    "SELECT Id, ClaveHash, ClaveSal FROM dbo.Usuario WHERE Usuario = @login AND Activo = 1", new { login });

            // aunque el usuario no exista se calcula un hash, asi tarda lo mismo y no se sabe cuales existen
            if (cred == null)
            {
                Seguridad.Verificar(clave, new byte[32], new byte[16]);
                Fallo(login);
                return null;
            }
            if (!Seguridad.Verificar(clave, cred.ClaveHash, cred.ClaveSal))
            {
                Fallo(login);
                return null;
            }
            return Acceso(login, cred.Id);
        }

        public static Usuario EntrarConPin(string pin)
        {
            const string clave = "#pin";
            RevisarBloqueo(clave);

            List<Credencial> conPin;
            using (var c = Conexion.Abrir())
                conPin = c.Query<Credencial>("SELECT Id, PinHash, PinSal FROM dbo.Usuario WHERE PinHash IS NOT NULL AND Activo = 1").ToList();

            var cred = conPin.FirstOrDefault(x => Seguridad.Verificar(pin, x.PinHash, x.PinSal));
            if (cred == null)
            {
                Fallo(clave);
                return null;
            }
            return Acceso(clave, cred.Id);
        }

        static Usuario Acceso(string clave, int id)
        {
            Tuple<int, DateTime> nada;
            fallos.TryRemove(clave, out nada);
            using (var c = Conexion.Abrir())
                c.Execute("UPDATE dbo.Usuario SET UltimoAcceso = SYSDATETIME() WHERE Id = @id", new { id });
            return Obtener(id);
        }

        static void RevisarBloqueo(string clave)
        {
            Tuple<int, DateTime> f;
            if (fallos.TryGetValue(clave, out f) && f.Item1 >= MAX_FALLOS)
            {
                var falta = f.Item2 + BLOQUEO - DateTime.Now;
                if (falta > TimeSpan.Zero)
                    throw new AvisoException("Demasiados intentos. Espera " + Math.Ceiling(falta.TotalSeconds) + " segundos.");
                fallos.TryRemove(clave, out f);
            }
        }

        static void Fallo(string clave)
        {
            fallos.AddOrUpdate(clave, Tuple.Create(1, DateTime.Now), (k, v) => Tuple.Create(v.Item1 + 1, DateTime.Now));
        }

        public static int Crear(string nombre, string login, string rol, string clave)
        {
            byte[] hash, sal;
            Seguridad.Hashear(clave, out hash, out sal);
            login = login.Trim().ToLowerInvariant();
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Usuario WHERE Usuario = @login", new { login }, tx) > 0)
                    throw new AvisoException("Ya existe alguien con el usuario " + login + ".");
                var id = c.ExecuteScalar<int>(
                    @"INSERT INTO dbo.Usuario (Nombre, Usuario, ClaveHash, ClaveSal, Rol)
                      OUTPUT INSERTED.Id VALUES (@nombre, @login, @hash, @sal, @rol)",
                    new { nombre = nombre.Trim(), login, hash, sal, rol }, tx);
                Bitacora.Registrar(c, tx, "Usuario nuevo", login + " (" + rol + ")");
                tx.Commit();
                return id;
            }
        }

        public static void Actualizar(Usuario u)
        {
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                // que nunca se quede el sistema sin un admin activo
                if (u.Rol != Roles.Admin || !u.Activo)
                {
                    var otros = c.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM dbo.Usuario WHERE Rol = N'Admin' AND Activo = 1 AND Id <> @Id", u, tx);
                    if (otros == 0)
                        throw new AvisoException("Tiene que quedar al menos un administrador activo.");
                }
                c.Execute("UPDATE dbo.Usuario SET Nombre = @Nombre, Rol = @Rol, Activo = @Activo WHERE Id = @Id", u, tx);
                Bitacora.Registrar(c, tx, "Usuario modificado", u.Login + " rol " + u.Rol + (u.Activo ? "" : ", desactivado"));
                tx.Commit();
            }
        }

        public static void CambiarClave(int id, string clave)
        {
            byte[] hash, sal;
            Seguridad.Hashear(clave, out hash, out sal);
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                c.Execute("UPDATE dbo.Usuario SET ClaveHash = @hash, ClaveSal = @sal WHERE Id = @id", new { id, hash, sal }, tx);
                Bitacora.Registrar(c, tx, "Cambio de contraseña", "usuario " + id);
                tx.Commit();
            }
        }

        // el pin se busca entre todos, por eso no pueden repetirse
        public static void CambiarPin(int id, string pin)
        {
            using (var c = Conexion.Abrir())
            {
                if (pin != null)
                {
                    var otros = c.Query<Credencial>("SELECT Id, PinHash, PinSal FROM dbo.Usuario WHERE PinHash IS NOT NULL AND Id <> @id", new { id });
                    if (otros.Any(x => Seguridad.Verificar(pin, x.PinHash, x.PinSal)))
                        throw new AvisoException("Ese PIN ya lo usa otra persona, escoge otro.");
                }
                byte[] hash = null, sal = null;
                if (pin != null) Seguridad.Hashear(pin, out hash, out sal);
                using (var tx = c.BeginTransaction())
                {
                    c.Execute("UPDATE dbo.Usuario SET PinHash = @hash, PinSal = @sal WHERE Id = @id", new { id, hash, sal }, tx);
                    Bitacora.Registrar(c, tx, pin == null ? "PIN quitado" : "PIN cambiado", "usuario " + id);
                    tx.Commit();
                }
            }
        }
    }
}
