using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using RestauranteDelicias.Modelos;
using RestauranteDelicias.Servicios;

namespace RestauranteDelicias.Datos
{
    public static class UsuarioRepo
    {
        const int MAX_FALLOS = 5;
        const int ESPERA_BASE_SEGUNDOS = 60;
        const int ESPERA_MAXIMA_SEGUNDOS = 900;
        const string LLAVE_PIN = "#pin";

        const string CAMPOS = @"Id, Nombre, Usuario AS Login, Rol, Activo,
                                CAST(CASE WHEN PinHash IS NULL THEN 0 ELSE 1 END AS BIT) AS TienePin, UltimoAcceso";

        static readonly Regex LoginValido = new Regex(@"^[a-z0-9._-]{3,40}$");

        public class Credencial
        {
            public int Id { get; set; }
            public byte[] ClaveHash { get; set; }
            public byte[] ClaveSal { get; set; }
            public byte[] PinHash { get; set; }
            public byte[] PinSal { get; set; }
        }

        public class Intento
        {
            public int Fallos { get; set; }
            public int Segundos { get; set; }
        }

        public static bool HayAdmin()
        {
            using (var c = Conexion.Abrir())
                return HayAdmin(c, null);
        }

        static bool HayAdmin(IDbConnection c, IDbTransaction tx)
        {
            return c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Usuario WHERE Activo = 1 AND Rol = N'Admin'", transaction: tx) > 0;
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
            var llave = Llave(login);
            RevisarBloqueo(llave);

            Credencial cred;
            using (var c = Conexion.Abrir())
                cred = c.QuerySingleOrDefault<Credencial>(
                    "SELECT Id, ClaveHash, ClaveSal FROM dbo.Usuario WHERE Usuario = @login AND Activo = 1", new { login });

            // aunque el usuario no exista se calcula un hash, asi tarda lo mismo y no se sabe cuales existen
            bool correcta;
            if (cred == null)
            {
                Seguridad.Verificar(clave, new byte[32], new byte[16]);
                correcta = false;
            }
            else
                correcta = Seguridad.Verificar(clave, cred.ClaveHash, cred.ClaveSal);

            if (!correcta)
            {
                Fallo(llave);
                return null;
            }
            return Acceso(llave, cred.Id);
        }

        // cada pin se revisa en paralelo, con varios usuarios la compu no se queda pensando
        public static Usuario EntrarConPin(string pin)
        {
            var llave = Llave(LLAVE_PIN);
            RevisarBloqueo(llave);

            List<Credencial> conPin;
            using (var c = Conexion.Abrir())
                conPin = c.Query<Credencial>("SELECT Id, PinHash, PinSal FROM dbo.Usuario WHERE PinHash IS NOT NULL AND Activo = 1").ToList();

            var cred = conPin.AsParallel().FirstOrDefault(x => Seguridad.Verificar(pin, x.PinHash, x.PinSal));
            if (cred == null)
            {
                Fallo(llave);
                return null;
            }
            return Acceso(llave, cred.Id);
        }

        static string Llave(string login)
        {
            return Conexion.Equipo + "|" + login;
        }

        static Usuario Acceso(string llave, int id)
        {
            using (var c = Conexion.Abrir())
                c.Execute(
                    @"DELETE FROM dbo.IntentoAcceso WHERE Clave = @llave;
                      UPDATE dbo.Usuario SET UltimoAcceso = SYSDATETIME() WHERE Id = @id;",
                    new { llave, id });
            return Obtener(id);
        }

        // despues de 5 fallos se espera un minuto, y cada fallo de mas dobla la espera hasta 15 minutos
        static void RevisarBloqueo(string llave)
        {
            Intento intento;
            using (var c = Conexion.Abrir())
                intento = c.QuerySingleOrDefault<Intento>(
                    "SELECT Fallos, DATEDIFF(SECOND, Ultimo, SYSDATETIME()) AS Segundos FROM dbo.IntentoAcceso WHERE Clave = @llave",
                    new { llave });
            if (intento == null || intento.Fallos < MAX_FALLOS) return;

            var espera = Math.Min(ESPERA_MAXIMA_SEGUNDOS, ESPERA_BASE_SEGUNDOS << Math.Min(10, intento.Fallos - MAX_FALLOS));
            var falta = espera - intento.Segundos;
            if (falta > 0)
                throw new AvisoException(falta > 90
                    ? "Demasiados intentos. Espera " + (int)Math.Ceiling(falta / 60.0) + " minutos."
                    : "Demasiados intentos. Espera " + falta + " segundos.");
        }

        // si el ultimo fallo fue hace mas de una hora se empieza a contar de nuevo
        static void Fallo(string llave)
        {
            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction())
            {
                c.Execute(
                    @"UPDATE dbo.IntentoAcceso WITH (UPDLOCK, SERIALIZABLE)
                      SET Fallos = CASE WHEN Ultimo < DATEADD(HOUR, -1, SYSDATETIME()) THEN 1 ELSE Fallos + 1 END,
                          Ultimo = SYSDATETIME()
                      WHERE Clave = @llave;
                      IF @@ROWCOUNT = 0
                          INSERT INTO dbo.IntentoAcceso (Clave, Fallos, Ultimo) VALUES (@llave, 1, SYSDATETIME());",
                    new { llave }, tx);
                tx.Commit();
            }
        }

        // el primer admin se crea sin sesion, despues solo un admin puede crear usuarios
        public static int Crear(string nombre, string login, string rol, string clave, string pin)
        {
            nombre = (nombre ?? "").Trim();
            login = (login ?? "").Trim().ToLowerInvariant();
            if (nombre.Length < 3 || nombre.Length > 100) throw new AvisoException("El nombre tiene que tener de 3 a 100 letras.");
            if (!LoginValido.IsMatch(login)) throw new AvisoException("El usuario lleva de 3 a 40 letras o números, sin espacios.");
            if (!Roles.Todos.Contains(rol)) throw new AvisoException("Escoge un rol.");
            var error = Seguridad.ValidarClave(clave) ?? (string.IsNullOrEmpty(pin) ? null : Seguridad.ValidarPin(pin));
            if (error != null) throw new AvisoException(error);

            byte[] hash, sal, pinHash = null, pinSal = null;
            Seguridad.Hashear(clave, out hash, out sal);
            if (!string.IsNullOrEmpty(pin)) Seguridad.Hashear(pin, out pinHash, out pinSal);

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction(IsolationLevel.Serializable))
            {
                Sesion.Exigir(Sesion.EsAdmin || (!HayAdmin(c, tx) && rol == Roles.Admin));
                if (c.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.Usuario WHERE Usuario = @login", new { login }, tx) > 0)
                    throw new AvisoException("Ya existe alguien con el usuario " + login + ".");
                if (!string.IsNullOrEmpty(pin)) RevisarPinLibre(c, tx, pin, 0);

                var id = c.ExecuteScalar<int>(
                    @"INSERT INTO dbo.Usuario (Nombre, Usuario, ClaveHash, ClaveSal, PinHash, PinSal, Rol)
                      OUTPUT INSERTED.Id VALUES (@nombre, @login, @hash, @sal, @pinHash, @pinSal, @rol)",
                    new { nombre, login, hash, sal, pinHash, pinSal, rol }, tx);
                Bitacora.Registrar(c, tx, "Usuario nuevo", login + ", rol " + rol);
                tx.Commit();
                return id;
            }
        }

        public static void Actualizar(Usuario u)
        {
            Sesion.Exigir(Sesion.EsAdmin);
            u.Nombre = (u.Nombre ?? "").Trim();
            if (u.Nombre.Length < 3 || u.Nombre.Length > 100) throw new AvisoException("El nombre tiene que tener de 3 a 100 letras.");
            if (!Roles.Todos.Contains(u.Rol)) throw new AvisoException("Escoge un rol.");

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction(IsolationLevel.Serializable))
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
            Sesion.Exigir(Sesion.EsAdmin);
            var error = Seguridad.ValidarClave(clave);
            if (error != null) throw new AvisoException(error);

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
            Sesion.Exigir(Sesion.EsAdmin);
            if (pin != null)
            {
                var error = Seguridad.ValidarPin(pin);
                if (error != null) throw new AvisoException(error);
            }

            byte[] hash = null, sal = null;
            if (pin != null) Seguridad.Hashear(pin, out hash, out sal);

            using (var c = Conexion.Abrir())
            using (var tx = c.BeginTransaction(IsolationLevel.Serializable))
            {
                if (pin != null) RevisarPinLibre(c, tx, pin, id);
                c.Execute("UPDATE dbo.Usuario SET PinHash = @hash, PinSal = @sal WHERE Id = @id", new { id, hash, sal }, tx);
                Bitacora.Registrar(c, tx, pin == null ? "PIN quitado" : "PIN cambiado", "usuario " + id);
                tx.Commit();
            }
        }

        static void RevisarPinLibre(IDbConnection c, IDbTransaction tx, string pin, int menosEste)
        {
            var otros = c.Query<Credencial>(
                "SELECT Id, PinHash, PinSal FROM dbo.Usuario WHERE PinHash IS NOT NULL AND Id <> @menosEste",
                new { menosEste }, tx).ToList();
            if (otros.AsParallel().Any(x => Seguridad.Verificar(pin, x.PinHash, x.PinSal)))
                throw new AvisoException("Ese PIN ya lo usa otra persona, escoge otro.");
        }
    }
}
