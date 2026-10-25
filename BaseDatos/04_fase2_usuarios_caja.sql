-- fase 2, usuarios con pin, turnos de caja y descuentos
-- el programa lo corre solo si la base es de la fase 1, no borra nada

ALTER TABLE dbo.Usuario ADD
    PinHash         VARBINARY(64)  NULL,
    PinSal          VARBINARY(32)  NULL,
    UltimoAcceso    DATETIME2(0)   NULL;
GO

ALTER TABLE dbo.Turno ADD
    Esperado        DECIMAL(10,2)  NULL,
    CerradoPor      INT            NULL CONSTRAINT FK_Turno_CerradoPor REFERENCES dbo.Usuario (Id);
GO

-- un cajero no puede tener dos cajas abiertas al mismo tiempo
CREATE UNIQUE INDEX UX_Turno_Abierto ON dbo.Turno (UsuarioId) WHERE Cierre IS NULL;
GO

ALTER TABLE dbo.Orden ADD
    DescuentoMotivo     NVARCHAR(150)  NULL,
    DescuentoAutorizo   INT            NULL CONSTRAINT FK_Orden_DescuentoAutorizo REFERENCES dbo.Usuario (Id);
GO

-- el pago queda en el turno de quien cobro, asi cada caja cuadra lo suyo
ALTER TABLE dbo.Pago ADD
    TurnoId     INT  NULL CONSTRAINT FK_Pago_Turno REFERENCES dbo.Turno (Id);
GO
CREATE INDEX IX_Pago_Turno ON dbo.Pago (TurnoId) INCLUDE (Forma, Monto) WHERE TurnoId IS NOT NULL;
GO

ALTER TABLE dbo.OrdenDetalle ADD
    AnuladoPor      INT  NULL CONSTRAINT FK_Detalle_AnuladoPor REFERENCES dbo.Usuario (Id),
    AutorizadoPor   INT  NULL CONSTRAINT FK_Detalle_AutorizadoPor REFERENCES dbo.Usuario (Id);
GO

CREATE INDEX IX_Bitacora_Usuario ON dbo.Bitacora (UsuarioId, Fecha);
GO
