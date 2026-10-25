-- crea la base vacia, correrlo conectado a master
-- si usas el asistente del programa no hace falta, el lo hace solo

IF DB_ID(N'RestauranteDelicias') IS NULL
    CREATE DATABASE RestauranteDelicias;
GO

-- con esto las lecturas no bloquean a los que estan guardando
-- importante con varias cajas y meseros trabajando al mismo tiempo
ALTER DATABASE RestauranteDelicias SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO
ALTER DATABASE RestauranteDelicias SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
