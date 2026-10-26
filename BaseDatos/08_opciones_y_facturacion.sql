-- opciones de los platillos, datos de factura en cada pago y numero de los pedidos para llevar
-- cada paso revisa si ya existe, asi se puede volver a correr sin que truene

-- un grupo es una pregunta que se hace al pedir, como el estilo de los huevos o los extras
-- minimo 1 es obligatorio, maximo 1 es escoger solo uno
-- si tiene SoloSiOpcionId el grupo solo se pregunta cuando escogieron esa opcion, como el termino de los huevos estrellados
IF OBJECT_ID(N'dbo.GrupoOpcion', N'U') IS NULL
    CREATE TABLE dbo.GrupoOpcion (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GrupoOpcion PRIMARY KEY,
        Nombre          NVARCHAR(60)   NOT NULL,
        Minimo          INT            NOT NULL CONSTRAINT DF_GrupoOpcion_Minimo DEFAULT (0),
        Maximo          INT            NOT NULL CONSTRAINT DF_GrupoOpcion_Maximo DEFAULT (1),
        SoloSiOpcionId  INT            NULL,
        Activo          BIT            NOT NULL CONSTRAINT DF_GrupoOpcion_Activo DEFAULT (1),
        CONSTRAINT UQ_GrupoOpcion_Nombre UNIQUE (Nombre),
        CONSTRAINT CK_GrupoOpcion_Rango CHECK (Minimo >= 0 AND Maximo >= 1 AND Minimo <= Maximo AND Maximo <= 20)
    );
GO

IF OBJECT_ID(N'dbo.Opcion', N'U') IS NULL
    CREATE TABLE dbo.Opcion (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Opcion PRIMARY KEY,
        GrupoId     INT            NOT NULL CONSTRAINT FK_Opcion_Grupo REFERENCES dbo.GrupoOpcion (Id),
        Nombre      NVARCHAR(60)   NOT NULL,
        Precio      DECIMAL(10,2)  NOT NULL CONSTRAINT DF_Opcion_Precio DEFAULT (0),
        Orden       INT            NOT NULL CONSTRAINT DF_Opcion_Orden DEFAULT (0),
        Activa      BIT            NOT NULL CONSTRAINT DF_Opcion_Activa DEFAULT (1),
        CONSTRAINT UQ_Opcion_Nombre UNIQUE (GrupoId, Nombre),
        CONSTRAINT CK_Opcion_Precio CHECK (Precio >= 0)
    );
GO

IF OBJECT_ID(N'dbo.FK_GrupoOpcion_SoloSi', N'F') IS NULL
    ALTER TABLE dbo.GrupoOpcion ADD CONSTRAINT FK_GrupoOpcion_SoloSi FOREIGN KEY (SoloSiOpcionId) REFERENCES dbo.Opcion (Id);
GO

IF OBJECT_ID(N'dbo.PlatilloGrupo', N'U') IS NULL
    CREATE TABLE dbo.PlatilloGrupo (
        PlatilloId  INT  NOT NULL CONSTRAINT FK_PlatilloGrupo_Platillo REFERENCES dbo.Platillo (Id),
        GrupoId     INT  NOT NULL CONSTRAINT FK_PlatilloGrupo_Grupo REFERENCES dbo.GrupoOpcion (Id),
        Orden       INT  NOT NULL CONSTRAINT DF_PlatilloGrupo_Orden DEFAULT (0),
        CONSTRAINT PK_PlatilloGrupo PRIMARY KEY (PlatilloId, GrupoId)
    );
GO

-- lo que se escogio queda copiado en el renglon, asi cocina y el ticket lo leen directo
-- y queda aparte con su precio para los reportes
IF COL_LENGTH(N'dbo.OrdenDetalle', N'Opciones') IS NULL
    ALTER TABLE dbo.OrdenDetalle ADD Opciones NVARCHAR(300) NOT NULL CONSTRAINT DF_Detalle_Opciones DEFAULT (N'');
GO

IF OBJECT_ID(N'dbo.OrdenDetalleOpcion', N'U') IS NULL
    CREATE TABLE dbo.OrdenDetalleOpcion (
        DetalleId   INT            NOT NULL CONSTRAINT FK_DetalleOpcion_Detalle REFERENCES dbo.OrdenDetalle (Id) ON DELETE CASCADE,
        OpcionId    INT            NOT NULL CONSTRAINT FK_DetalleOpcion_Opcion REFERENCES dbo.Opcion (Id),
        Nombre      NVARCHAR(60)   NOT NULL,
        Precio      DECIMAL(10,2)  NOT NULL,
        CONSTRAINT PK_OrdenDetalleOpcion PRIMARY KEY (DetalleId, OpcionId)
    );
GO

-- clientes con NIT, se llenan solos al escribir el NIT la siguiente vez
IF OBJECT_ID(N'dbo.Cliente', N'U') IS NULL
    CREATE TABLE dbo.Cliente (
        Nit         NVARCHAR(20)   NOT NULL CONSTRAINT PK_Cliente PRIMARY KEY,
        Nombre      NVARCHAR(150)  NOT NULL,
        Direccion   NVARCHAR(200)  NOT NULL CONSTRAINT DF_Cliente_Direccion DEFAULT (N'Ciudad'),
        Actualizado DATETIME2(0)   NOT NULL CONSTRAINT DF_Cliente_Actualizado DEFAULT (SYSDATETIME())
    );
GO

-- cada pago lleva sus datos de factura, en una cuenta dividida cada quien puede dar su NIT
IF COL_LENGTH(N'dbo.Pago', N'Nit') IS NULL
    ALTER TABLE dbo.Pago ADD
        Nit                 NVARCHAR(20)   NOT NULL CONSTRAINT DF_Pago_Nit DEFAULT (N'CF'),
        NombreFactura       NVARCHAR(150)  NOT NULL CONSTRAINT DF_Pago_NombreFactura DEFAULT (N'Consumidor Final'),
        DireccionFactura    NVARCHAR(200)  NOT NULL CONSTRAINT DF_Pago_DireccionFactura DEFAULT (N'Ciudad');
GO

-- los pedidos para llevar se llaman por numero, empieza en 1 cada dia
IF COL_LENGTH(N'dbo.Orden', N'NumeroLlevar') IS NULL
    ALTER TABLE dbo.Orden ADD NumeroLlevar INT NULL;
GO

-- ejemplos para que se vea como funciona, el admin los cambia en el menu
IF NOT EXISTS (SELECT 1 FROM dbo.GrupoOpcion)
BEGIN
    INSERT INTO dbo.GrupoOpcion (Nombre, Minimo, Maximo) VALUES
        (N'Huevos', 1, 1), (N'Término de la carne', 1, 1), (N'Extras', 0, 5), (N'Quitar', 0, 5);
    DECLARE @huevos INT = (SELECT Id FROM dbo.GrupoOpcion WHERE Nombre = N'Huevos');
    DECLARE @carne INT = (SELECT Id FROM dbo.GrupoOpcion WHERE Nombre = N'Término de la carne');
    DECLARE @extras INT = (SELECT Id FROM dbo.GrupoOpcion WHERE Nombre = N'Extras');
    DECLARE @quitar INT = (SELECT Id FROM dbo.GrupoOpcion WHERE Nombre = N'Quitar');

    INSERT INTO dbo.Opcion (GrupoId, Nombre, Precio, Orden) VALUES
        (@huevos, N'Estrellados', 0, 1), (@huevos, N'Revueltos', 0, 2), (@huevos, N'Duros', 0, 3), (@huevos, N'Rancheros', 5, 4),
        (@carne, N'Término medio', 0, 1), (@carne, N'Tres cuartos', 0, 2), (@carne, N'Bien cocido', 0, 3),
        (@extras, N'Queso', 5, 1), (@extras, N'Aguacate', 8, 2), (@extras, N'Crema', 4, 3), (@extras, N'Huevo extra', 6, 4),
        (@quitar, N'Sin cebolla', 0, 1), (@quitar, N'Sin chile', 0, 2), (@quitar, N'Sin crema', 0, 3);

    DECLARE @estrellados INT = (SELECT Id FROM dbo.Opcion WHERE GrupoId = @huevos AND Nombre = N'Estrellados');
    INSERT INTO dbo.GrupoOpcion (Nombre, Minimo, Maximo, SoloSiOpcionId) VALUES (N'Término del huevo', 1, 1, @estrellados);
    DECLARE @termino INT = (SELECT Id FROM dbo.GrupoOpcion WHERE Nombre = N'Término del huevo');
    INSERT INTO dbo.Opcion (GrupoId, Nombre, Precio, Orden) VALUES
        (@termino, N'Tiernos', 0, 1), (@termino, N'Medios', 0, 2), (@termino, N'Bien cocidos', 0, 3);

    INSERT INTO dbo.PlatilloGrupo (PlatilloId, GrupoId, Orden)
    SELECT p.Id, g.Id, g.Orden
    FROM dbo.Platillo p
    CROSS APPLY (VALUES (@huevos, 1), (@termino, 2), (@extras, 3), (@quitar, 4)) g (Id, Orden)
    WHERE p.Nombre = N'Desayuno chapín';

    INSERT INTO dbo.PlatilloGrupo (PlatilloId, GrupoId, Orden)
    SELECT p.Id, g.Id, g.Orden
    FROM dbo.Platillo p
    CROSS APPLY (VALUES (@carne, 1), (@extras, 2), (@quitar, 3)) g (Id, Orden)
    WHERE p.Nombre = N'Churrasco con chirmol';
END
GO
