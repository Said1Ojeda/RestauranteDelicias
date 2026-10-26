-- cuentas divididas, varios pagos por orden, pedidos para llevar y a domicilio
-- cada paso revisa si ya existe, asi se puede volver a correr sin que truene

-- de que parte de la cuenta es cada pago, por ejemplo parte 2 de 3 o por platillos
IF COL_LENGTH(N'dbo.Pago', N'Parte') IS NULL
    ALTER TABLE dbo.Pago ADD Parte NVARCHAR(40) NULL;
GO

-- cuando se divide por platillos cada renglon queda amarrado al pago que lo cubrio
IF COL_LENGTH(N'dbo.OrdenDetalle', N'PagoId') IS NULL
    ALTER TABLE dbo.OrdenDetalle ADD PagoId INT NULL CONSTRAINT FK_Detalle_Pago REFERENCES dbo.Pago (Id);
GO

-- los datos del cliente para llevar o a domicilio, el nombre va en Cliente que ya existia
IF COL_LENGTH(N'dbo.Orden', N'Telefono') IS NULL
    ALTER TABLE dbo.Orden ADD Telefono NVARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.Orden', N'Direccion') IS NULL
    ALTER TABLE dbo.Orden ADD Direccion NVARCHAR(200) NULL;
GO

-- la pantalla de mesas tambien pregunta por los pedidos abiertos que no tienen mesa
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orden_SinMesa' AND object_id = OBJECT_ID(N'dbo.Orden'))
    CREATE INDEX IX_Orden_SinMesa ON dbo.Orden (Apertura)
        INCLUDE (Tipo, Cliente, Telefono)
        WHERE Estado = N'Abierta' AND MesaId IS NULL;
GO
