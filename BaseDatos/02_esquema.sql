-- tablas del sistema, correrlo dentro de la base RestauranteDelicias

CREATE TABLE dbo.Configuracion (
    Clave   NVARCHAR(50)  NOT NULL CONSTRAINT PK_Configuracion PRIMARY KEY,
    Valor   NVARCHAR(300) NOT NULL
);
GO

CREATE TABLE dbo.Categoria (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categoria PRIMARY KEY,
    Nombre      NVARCHAR(60)  NOT NULL,
    Orden       INT           NOT NULL CONSTRAINT DF_Categoria_Orden DEFAULT (0),
    -- a donde se manda la comanda, la cocina o la barra de bebidas
    Estacion    NVARCHAR(10)  NOT NULL CONSTRAINT DF_Categoria_Estacion DEFAULT (N'Cocina'),
    Color       CHAR(7)       NULL,
    Activa      BIT           NOT NULL CONSTRAINT DF_Categoria_Activa DEFAULT (1),
    CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_Categoria_Estacion CHECK (Estacion IN (N'Cocina', N'Barra'))
);
GO

CREATE TABLE dbo.Platillo (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Platillo PRIMARY KEY,
    CategoriaId     INT            NOT NULL CONSTRAINT FK_Platillo_Categoria REFERENCES dbo.Categoria (Id),
    Nombre          NVARCHAR(100)  NOT NULL,
    Descripcion     NVARCHAR(300)  NULL,
    Precio          DECIMAL(10,2)  NOT NULL,
    -- disponible es si hay hoy, activo es si sigue en el menu
    Disponible      BIT            NOT NULL CONSTRAINT DF_Platillo_Disponible DEFAULT (1),
    Activo          BIT            NOT NULL CONSTRAINT DF_Platillo_Activo DEFAULT (1),
    Modificado      DATETIME2(0)   NOT NULL CONSTRAINT DF_Platillo_Modificado DEFAULT (SYSDATETIME()),
    CONSTRAINT CK_Platillo_Precio CHECK (Precio >= 0)
);
GO
CREATE INDEX IX_Platillo_Categoria ON dbo.Platillo (CategoriaId) INCLUDE (Nombre, Precio, Disponible, Activo);
GO

CREATE TABLE dbo.Area (
    Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Area PRIMARY KEY,
    Nombre  NVARCHAR(40) NOT NULL,
    Orden   INT          NOT NULL CONSTRAINT DF_Area_Orden DEFAULT (0),
    Activa  BIT          NOT NULL CONSTRAINT DF_Area_Activa DEFAULT (1),
    CONSTRAINT UQ_Area_Nombre UNIQUE (Nombre)
);
GO

CREATE TABLE dbo.Mesa (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Mesa PRIMARY KEY,
    AreaId      INT          NOT NULL CONSTRAINT FK_Mesa_Area REFERENCES dbo.Area (Id),
    Numero      INT          NOT NULL,
    Capacidad   INT          NOT NULL CONSTRAINT DF_Mesa_Capacidad DEFAULT (4),
    -- posicion en el plano del salon, se usa en la fase de diseno
    PosX        INT          NOT NULL CONSTRAINT DF_Mesa_PosX DEFAULT (0),
    PosY        INT          NOT NULL CONSTRAINT DF_Mesa_PosY DEFAULT (0),
    Forma       NVARCHAR(10) NOT NULL CONSTRAINT DF_Mesa_Forma DEFAULT (N'Cuadrada'),
    Activa      BIT          NOT NULL CONSTRAINT DF_Mesa_Activa DEFAULT (1),
    CONSTRAINT UQ_Mesa_Numero UNIQUE (Numero),
    CONSTRAINT CK_Mesa_Capacidad CHECK (Capacidad BETWEEN 1 AND 30),
    CONSTRAINT CK_Mesa_Forma CHECK (Forma IN (N'Cuadrada', N'Redonda', N'Barra'))
);
GO

CREATE TABLE dbo.Usuario (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
    Nombre      NVARCHAR(100)  NOT NULL,
    Usuario     NVARCHAR(40)   NOT NULL,
    ClaveHash   VARBINARY(64)  NULL,
    ClaveSal    VARBINARY(32)  NULL,
    Rol         NVARCHAR(15)   NOT NULL,
    Activo      BIT            NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT (1),
    Creado      DATETIME2(0)   NOT NULL CONSTRAINT DF_Usuario_Creado DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_Usuario_Usuario UNIQUE (Usuario),
    CONSTRAINT CK_Usuario_Rol CHECK (Rol IN (N'Admin', N'Cajero', N'Mesero', N'Cocina'))
);
GO

CREATE TABLE dbo.Turno (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Turno PRIMARY KEY,
    UsuarioId       INT            NOT NULL CONSTRAINT FK_Turno_Usuario REFERENCES dbo.Usuario (Id),
    Apertura        DATETIME2(0)   NOT NULL CONSTRAINT DF_Turno_Apertura DEFAULT (SYSDATETIME()),
    Cierre          DATETIME2(0)   NULL,
    MontoInicial    DECIMAL(10,2)  NOT NULL CONSTRAINT DF_Turno_MontoInicial DEFAULT (0),
    EfectivoContado DECIMAL(10,2)  NULL,
    Notas           NVARCHAR(300)  NULL
);
GO

CREATE TABLE dbo.Orden (
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orden PRIMARY KEY,
    Tipo                NVARCHAR(10)   NOT NULL CONSTRAINT DF_Orden_Tipo DEFAULT (N'Mesa'),
    MesaId              INT            NULL CONSTRAINT FK_Orden_Mesa REFERENCES dbo.Mesa (Id),
    MeseroId            INT            NULL CONSTRAINT FK_Orden_Mesero REFERENCES dbo.Usuario (Id),
    TurnoId             INT            NULL CONSTRAINT FK_Orden_Turno REFERENCES dbo.Turno (Id),
    Estado              NVARCHAR(10)   NOT NULL CONSTRAINT DF_Orden_Estado DEFAULT (N'Abierta'),
    Personas            INT            NOT NULL CONSTRAINT DF_Orden_Personas DEFAULT (1),
    ConPropina          BIT            NOT NULL CONSTRAINT DF_Orden_ConPropina DEFAULT (1),
    PorcentajePropina   DECIMAL(5,2)   NOT NULL CONSTRAINT DF_Orden_PorcPropina DEFAULT (10),
    Descuento           DECIMAL(10,2)  NOT NULL CONSTRAINT DF_Orden_Descuento DEFAULT (0),
    Cliente             NVARCHAR(100)  NULL,
    Nota                NVARCHAR(300)  NULL,
    Apertura            DATETIME2(0)   NOT NULL CONSTRAINT DF_Orden_Apertura DEFAULT (SYSDATETIME()),
    Cierre              DATETIME2(0)   NULL,
    -- los totales se congelan al cobrar, asi los reportes no cambian si despues cambia un precio
    Subtotal            DECIMAL(10,2)  NULL,
    Propina             DECIMAL(10,2)  NULL,
    Total               DECIMAL(10,2)  NULL,
    NumeroTicket        INT            NULL,
    Equipo              NVARCHAR(60)   NULL,
    Version             ROWVERSION,
    CONSTRAINT CK_Orden_Tipo CHECK (Tipo IN (N'Mesa', N'Llevar', N'Domicilio')),
    CONSTRAINT CK_Orden_Estado CHECK (Estado IN (N'Abierta', N'Cobrada', N'Anulada')),
    CONSTRAINT CK_Orden_Personas CHECK (Personas BETWEEN 1 AND 100),
    CONSTRAINT CK_Orden_Descuento CHECK (Descuento >= 0)
);
GO
-- una mesa solo puede tener una orden abierta, aunque dos meseros la abran al mismo tiempo
CREATE UNIQUE INDEX UX_Orden_MesaAbierta ON dbo.Orden (MesaId) WHERE Estado = N'Abierta' AND MesaId IS NOT NULL;
GO
CREATE UNIQUE INDEX UX_Orden_Ticket ON dbo.Orden (NumeroTicket) WHERE NumeroTicket IS NOT NULL;
GO
CREATE INDEX IX_Orden_Estado ON dbo.Orden (Estado) INCLUDE (MesaId, Apertura);
GO
CREATE INDEX IX_Orden_Cierre ON dbo.Orden (Cierre) INCLUDE (Estado, Total, Propina) WHERE Cierre IS NOT NULL;
GO

CREATE SEQUENCE dbo.SecTicket AS INT START WITH 1 INCREMENT BY 1;
GO

CREATE TABLE dbo.OrdenDetalle (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrdenDetalle PRIMARY KEY,
    OrdenId         INT            NOT NULL CONSTRAINT FK_Detalle_Orden REFERENCES dbo.Orden (Id) ON DELETE CASCADE,
    PlatilloId      INT            NOT NULL CONSTRAINT FK_Detalle_Platillo REFERENCES dbo.Platillo (Id),
    -- copia del nombre y precio del momento, si cambian el menu la orden no se mueve
    Nombre          NVARCHAR(100)  NOT NULL,
    Cantidad        INT            NOT NULL,
    PrecioUnitario  DECIMAL(10,2)  NOT NULL,
    Nota            NVARCHAR(150)  NOT NULL CONSTRAINT DF_Detalle_Nota DEFAULT (N''),
    -- Nuevo es que todavia no se manda a cocina
    Estado          NVARCHAR(12)   NOT NULL CONSTRAINT DF_Detalle_Estado DEFAULT (N'Nuevo'),
    Enviado         DATETIME2(0)   NULL,
    Listo           DATETIME2(0)   NULL,
    Anulado         BIT            NOT NULL CONSTRAINT DF_Detalle_Anulado DEFAULT (0),
    MotivoAnulacion NVARCHAR(150)  NULL,
    Creado          DATETIME2(0)   NOT NULL CONSTRAINT DF_Detalle_Creado DEFAULT (SYSDATETIME()),
    CreadoPor       INT            NULL CONSTRAINT FK_Detalle_Usuario REFERENCES dbo.Usuario (Id),
    CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalle_Precio CHECK (PrecioUnitario >= 0),
    CONSTRAINT CK_Detalle_Estado CHECK (Estado IN (N'Nuevo', N'Enviado', N'Preparando', N'Listo', N'Entregado'))
);
GO
CREATE INDEX IX_Detalle_Orden ON dbo.OrdenDetalle (OrdenId) INCLUDE (Cantidad, PrecioUnitario, Anulado);
GO
CREATE INDEX IX_Detalle_Cocina ON dbo.OrdenDetalle (Estado, Enviado) WHERE Anulado = 0;
GO

CREATE TABLE dbo.Pago (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
    OrdenId     INT            NOT NULL CONSTRAINT FK_Pago_Orden REFERENCES dbo.Orden (Id),
    Forma       NVARCHAR(15)   NOT NULL,
    Monto       DECIMAL(10,2)  NOT NULL,
    Recibido    DECIMAL(10,2)  NULL,
    Referencia  NVARCHAR(50)   NULL,
    Fecha       DATETIME2(0)   NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId   INT            NULL CONSTRAINT FK_Pago_Usuario REFERENCES dbo.Usuario (Id),
    CONSTRAINT CK_Pago_Forma CHECK (Forma IN (N'Efectivo', N'Tarjeta', N'Transferencia')),
    CONSTRAINT CK_Pago_Monto CHECK (Monto > 0)
);
GO
CREATE INDEX IX_Pago_Orden ON dbo.Pago (OrdenId);
GO

CREATE TABLE dbo.Bitacora (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
    Fecha       DATETIME2(0)   NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT (SYSDATETIME()),
    UsuarioId   INT            NULL CONSTRAINT FK_Bitacora_Usuario REFERENCES dbo.Usuario (Id),
    Equipo      NVARCHAR(60)   NULL,
    Accion      NVARCHAR(50)   NOT NULL,
    Detalle     NVARCHAR(400)  NULL
);
GO
CREATE INDEX IX_Bitacora_Fecha ON dbo.Bitacora (Fecha);
GO
