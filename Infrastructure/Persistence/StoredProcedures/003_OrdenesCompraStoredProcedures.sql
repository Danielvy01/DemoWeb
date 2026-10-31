-- ============================================================================
-- Stored procedures de registro (Registrar) y edición (Modificar) de la
-- orden de compra (documento + ítems) usados por OrdenesCompraController.
-- Igual que 002_ComprasStoredProcedures.sql, estos procedimientos se invocan
-- dentro de la misma conexión/transacción EF Core abierta por el controlador
-- (ver Infrastructure/Persistence/Repositories/OrdenCompraSqlRepository.cs),
-- ya que el controlador sigue resolviendo con EF Core las validaciones
-- (proveedor, comprador, comprobante, cuentas contables).
--
-- Ejecutar contra la base ENMIKADOSBD (SQL Server) antes de desplegar el
-- backend. Es idempotente: crea el tipo de tabla solo si no existe y los
-- procedimientos con CREATE OR ALTER.
-- ============================================================================

-- Tipo de tabla usado para pasar los ítems (OrdenCompraDetalle) como parámetro
-- de valores de tabla (TVP) a los stored procedures de OrdenCompra.
IF TYPE_ID(N'dbo.OrdenCompraDetalleTableType') IS NULL
BEGIN
    CREATE TYPE dbo.OrdenCompraDetalleTableType AS TABLE
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProductoId UNIQUEIDENTIFIER NOT NULL,
        Cantidad DECIMAL(12,2) NOT NULL,
        PrecioUnitario DECIMAL(12,2) NOT NULL,
        CantidadRecibida DECIMAL(12,2) NOT NULL,
        Total DECIMAL(12,2) NOT NULL,
        CuentaContableId UNIQUEIDENTIFIER NULL
    );
END
GO

-- ==================== ÓRDENES DE COMPRA ====================
CREATE OR ALTER PROCEDURE dbo.sp_OrdenCompra_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @ProveedorId UNIQUEIDENTIFIER,
    @EmpleadoCompradorId UNIQUEIDENTIFIER = NULL,
    @ComprobanteId UNIQUEIDENTIFIER = NULL,
    @Numero NVARCHAR(20),
    @FechaEmision DATETIME2,
    @TipoEntrega NVARCHAR(20),
    @PlazoEntregaDias INT = NULL,
    @FechaEntregaEsperada DATETIME2 = NULL,
    @Moneda NVARCHAR(5),
    @Total DECIMAL(12,2),
    @Estado NVARCHAR(20),
    @Observaciones NVARCHAR(500) = NULL,
    @Detalles dbo.OrdenCompraDetalleTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO dbo.OrdenesCompra
        (Id, EmpresaId, ProveedorId, EmpleadoCompradorId, ComprobanteId, Numero, FechaEmision,
         TipoEntrega, PlazoEntregaDias, FechaEntregaEsperada, Moneda, Total, Estado, Observaciones,
         CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @ProveedorId, @EmpleadoCompradorId, @ComprobanteId, @Numero, @FechaEmision,
         @TipoEntrega, @PlazoEntregaDias, @FechaEntregaEsperada, @Moneda, @Total, @Estado, @Observaciones,
         @Now, @Now);

    INSERT INTO dbo.OrdenCompraDetalles (Id, OrdenCompraId, ProductoId, Cantidad, PrecioUnitario, CantidadRecibida, Total, CuentaContableId)
    SELECT Id, @Id, ProductoId, Cantidad, PrecioUnitario, CantidadRecibida, Total, CuentaContableId
    FROM @Detalles;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_OrdenCompra_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @ProveedorId UNIQUEIDENTIFIER,
    @EmpleadoCompradorId UNIQUEIDENTIFIER = NULL,
    @ComprobanteId UNIQUEIDENTIFIER = NULL,
    @Numero NVARCHAR(20),
    @FechaEmision DATETIME2,
    @TipoEntrega NVARCHAR(20),
    @PlazoEntregaDias INT = NULL,
    @FechaEntregaEsperada DATETIME2 = NULL,
    @Moneda NVARCHAR(5),
    @Total DECIMAL(12,2),
    @Estado NVARCHAR(20),
    @Observaciones NVARCHAR(500) = NULL,
    @Detalles dbo.OrdenCompraDetalleTableType READONLY,
    @Encontrada BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OrdenesCompra SET
        ProveedorId = @ProveedorId,
        EmpleadoCompradorId = @EmpleadoCompradorId,
        ComprobanteId = @ComprobanteId,
        TipoEntrega = @TipoEntrega,
        PlazoEntregaDias = @PlazoEntregaDias,
        FechaEntregaEsperada = @FechaEntregaEsperada,
        Moneda = @Moneda,
        Total = @Total,
        Estado = @Estado,
        Observaciones = @Observaciones,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    IF @@ROWCOUNT = 0
    BEGIN
        SET @Encontrada = 0;
        RETURN;
    END

    SET @Encontrada = 1;

    DELETE FROM dbo.OrdenCompraDetalles WHERE OrdenCompraId = @Id;

    INSERT INTO dbo.OrdenCompraDetalles (Id, OrdenCompraId, ProductoId, Cantidad, PrecioUnitario, CantidadRecibida, Total, CuentaContableId)
    SELECT Id, @Id, ProductoId, Cantidad, PrecioUnitario, CantidadRecibida, Total, CuentaContableId
    FROM @Detalles;
END
GO
