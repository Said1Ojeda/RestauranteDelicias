-- los intentos fallidos de entrar se guardan en la base, asi no se pierden al cerrar el programa
-- la clave es la compu y el usuario, o la compu y la palabra pin
IF OBJECT_ID(N'dbo.IntentoAcceso', N'U') IS NULL
    CREATE TABLE dbo.IntentoAcceso (
        Clave   NVARCHAR(120)  NOT NULL CONSTRAINT PK_IntentoAcceso PRIMARY KEY,
        Fallos  INT            NOT NULL,
        Ultimo  DATETIME2(0)   NOT NULL
    );
GO
