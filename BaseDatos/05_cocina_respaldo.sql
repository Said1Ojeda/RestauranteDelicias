-- fase 3, pantalla de cocina, cierre por inactividad y respaldos
-- el programa lo corre solo si la base viene de la fase 2, no borra nada
-- cada paso revisa si ya existe, asi se puede volver a correr sin que truene

-- lo que quedo enviado de pruebas viejas no tiene que salir en la pantalla de cocina
UPDATE d SET d.Estado = N'Entregado'
FROM dbo.OrdenDetalle d
JOIN dbo.Orden o ON o.Id = d.OrdenId
WHERE d.Estado IN (N'Enviado', N'Preparando', N'Listo') AND o.Estado <> N'Abierta';
GO

-- la pantalla de cocina pregunta cada pocos segundos por lo pendiente
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Detalle_Cocina' AND object_id = OBJECT_ID(N'dbo.OrdenDetalle'))
    CREATE INDEX IX_Detalle_Cocina ON dbo.OrdenDetalle (Estado, Enviado)
        INCLUDE (OrdenId, PlatilloId, Nombre, Cantidad, Nota, Anulado)
        WHERE Estado IN (N'Enviado', N'Preparando', N'Listo');
GO

-- cuando cocina empieza un pedido, sirve para los tiempos de los reportes
IF COL_LENGTH(N'dbo.OrdenDetalle', N'Preparando') IS NULL
    ALTER TABLE dbo.OrdenDetalle ADD Preparando DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.OrdenDetalle', N'Entregado') IS NULL
    ALTER TABLE dbo.OrdenDetalle ADD Entregado DATETIME2(0) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Configuracion WHERE Clave = N'Sesion.MinutosInactivo')
    INSERT INTO dbo.Configuracion (Clave, Valor) VALUES (N'Sesion.MinutosInactivo', N'5');
IF NOT EXISTS (SELECT 1 FROM dbo.Configuracion WHERE Clave = N'Cocina.MinutosAlerta')
    INSERT INTO dbo.Configuracion (Clave, Valor) VALUES (N'Cocina.MinutosAlerta', N'15');
GO
