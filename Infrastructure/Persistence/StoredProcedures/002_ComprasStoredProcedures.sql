-- ============================================================================
-- Stored procedures de registro (Registrar) y edición (Modificar) de la
-- compra (documento + ítems) usados por ComprasService/ComprasController.
-- A diferencia de 001_MasterDataStoredProcedures.sql, estos procedimientos se
-- invocan dentro de la misma transacción EF Core abierta por el controlador
-- (ver Infrastructure/Persistence/Repositories/ComprasSqlRepository.cs), ya
-- que el registro de una compra también ajusta stock/Kardex vía EF Core.
--
-- Ejecutar contra la base ENMIKADOSBD (SQL Server) antes de desplegar el
-- backend. Es idempotente: crea el tipo de tabla solo si no existe y los
-- procedimientos con CREATE OR ALTER.
-- ============================================================================

-- Tipo de tabla usado para pasar los ítems (CompraDetalle) como parámetro de
-- valores de tabla (TVP) a los stored procedures de Compra.
-- Si el tipo ya existe de un despliegue previo sin la columna CuentaContableId, se
-- recrea (hay que soltar antes los procedimientos que lo usan como parámetro, ya que
-- SQL Server no permite ALTER TYPE para tipos de tabla).
IF TYPE_ID(N'dbo.CompraDetalleTableType') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.table_types tt
        JOIN sys.columns c ON c.object_id = tt.type_table_object_id
        WHERE tt.name = 'CompraDetalleTableType' AND c.name = 'CuentaContableId')
BEGIN
    DROP PROCEDURE IF EXISTS dbo.sp_Compra_Registrar;
    DROP PROCEDURE IF EXISTS dbo.sp_Compra_Modificar;
    DROP TYPE dbo.CompraDetalleTableType;
END
GO

IF TYPE_ID(N'dbo.CompraDetalleTableType') IS NULL
BEGIN
    CREATE TYPE dbo.CompraDetalleTableType AS TABLE
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        ProductoId UNIQUEIDENTIFIER NOT NULL,
        Cantidad DECIMAL(12,2) NOT NULL,
        PrecioUnitario DECIMAL(12,2) NOT NULL,
        PorcentajeDescuento DECIMAL(5,2) NOT NULL,
        Total DECIMAL(12,2) NOT NULL,
        CuentaContableId UNIQUEIDENTIFIER NULL
    );
END
GO

-- Columnas nuevas para la contabilización automática de la compra (al estilo CONCAR):
-- la cuenta contable por ítem (Existencias/Activo Fijo/Gasto) ya viaja en el TVP de
-- arriba; aquí se agrega el enlace de la cabecera al asiento generado.
IF COL_LENGTH('dbo.Compras', 'AsientoContableId') IS NULL
BEGIN
    ALTER TABLE dbo.Compras ADD AsientoContableId UNIQUEIDENTIFIER NULL;
END
GO
IF COL_LENGTH('dbo.CompraDetalles', 'CuentaContableId') IS NULL
BEGIN
    ALTER TABLE dbo.CompraDetalles ADD CuentaContableId UNIQUEIDENTIFIER NULL;
END
GO

-- Código de operación (al estilo CONCAR) elegido explícitamente al registrar la compra: ver
-- ConfiguracionCuentaOperacion/ContabilidadService.RegistrarAsientoCompraAsync.
IF COL_LENGTH('dbo.Compras', 'ConfiguracionCuentaOperacionId') IS NULL
BEGIN
    ALTER TABLE dbo.Compras ADD ConfiguracionCuentaOperacionId UNIQUEIDENTIFIER NULL;
END
GO

-- ==================== COMPRAS ====================
CREATE OR ALTER PROCEDURE dbo.sp_Compra_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @ProveedorId UNIQUEIDENTIFIER,
    @CentroCostoId UNIQUEIDENTIFIER,
    @AlmacenId UNIQUEIDENTIFIER,
    @OrdenCompraId UNIQUEIDENTIFIER = NULL,
    @EmpleadoCompradorId UNIQUEIDENTIFIER = NULL,
    @Serie NVARCHAR(10) = NULL,
    @Numero NVARCHAR(20) = NULL,
    @TipoDocumento NVARCHAR(30),
    @FechaEmision DATETIME2,
    @NumeroGuiaRemision NVARCHAR(20) = NULL,
    @Referencia NVARCHAR(255) = NULL,
    @LocalProveedor NVARCHAR(255) = NULL,
    @TipoOperacionSunat NVARCHAR(30),
    @Moneda NVARCHAR(5),
    @TasaIgv DECIMAL(5,2),
    @Subtotal DECIMAL(12,2),
    @Igv DECIMAL(12,2),
    @FechaTipoCambio DATETIME2 = NULL,
    @TipoCambio DECIMAL(8,4) = NULL,
    @Total DECIMAL(12,2),
    @GlosaComprobante NVARCHAR(500) = NULL,
    @GlosaMovimiento NVARCHAR(500) = NULL,
    @FechaVencimiento DATETIME2 = NULL,
    @ConfiguracionCuentaOperacionId UNIQUEIDENTIFIER = NULL,
    @Detalles dbo.CompraDetalleTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO dbo.Compras
        (Id, EmpresaId, ProveedorId, CentroCostoId, AlmacenId, OrdenCompraId, EmpleadoCompradorId,
         Serie, Numero, TipoDocumento, FechaEmision, NumeroGuiaRemision, Referencia, LocalProveedor,
         TipoOperacionSunat, Moneda, TasaIgv, Subtotal, Igv, FechaTipoCambio, TipoCambio, Total,
         GlosaComprobante, GlosaMovimiento, FechaVencimiento, ConfiguracionCuentaOperacionId, Pagado, FechaPago, CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @ProveedorId, @CentroCostoId, @AlmacenId, @OrdenCompraId, @EmpleadoCompradorId,
         @Serie, @Numero, @TipoDocumento, @FechaEmision, @NumeroGuiaRemision, @Referencia, @LocalProveedor,
         @TipoOperacionSunat, @Moneda, @TasaIgv, @Subtotal, @Igv, @FechaTipoCambio, @TipoCambio, @Total,
         @GlosaComprobante, @GlosaMovimiento, @FechaVencimiento, @ConfiguracionCuentaOperacionId, 0, NULL, @Now, NULL);

    INSERT INTO dbo.CompraDetalles (Id, CompraId, ProductoId, Cantidad, PrecioUnitario, PorcentajeDescuento, Total, CuentaContableId)
    SELECT Id, @Id, ProductoId, Cantidad, PrecioUnitario, PorcentajeDescuento, Total, CuentaContableId
    FROM @Detalles;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Compra_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @ProveedorId UNIQUEIDENTIFIER,
    @CentroCostoId UNIQUEIDENTIFIER,
    @AlmacenId UNIQUEIDENTIFIER,
    @OrdenCompraId UNIQUEIDENTIFIER = NULL,
    @EmpleadoCompradorId UNIQUEIDENTIFIER = NULL,
    @Serie NVARCHAR(10) = NULL,
    @Numero NVARCHAR(20) = NULL,
    @TipoDocumento NVARCHAR(30),
    @FechaEmision DATETIME2,
    @NumeroGuiaRemision NVARCHAR(20) = NULL,
    @Referencia NVARCHAR(255) = NULL,
    @LocalProveedor NVARCHAR(255) = NULL,
    @TipoOperacionSunat NVARCHAR(30),
    @Moneda NVARCHAR(5),
    @TasaIgv DECIMAL(5,2),
    @Subtotal DECIMAL(12,2),
    @Igv DECIMAL(12,2),
    @FechaTipoCambio DATETIME2 = NULL,
    @TipoCambio DECIMAL(8,4) = NULL,
    @Total DECIMAL(12,2),
    @GlosaComprobante NVARCHAR(500) = NULL,
    @GlosaMovimiento NVARCHAR(500) = NULL,
    @FechaVencimiento DATETIME2 = NULL,
    @ConfiguracionCuentaOperacionId UNIQUEIDENTIFIER = NULL,
    @Detalles dbo.CompraDetalleTableType READONLY,
    @Encontrada BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Compras SET
        CentroCostoId = @CentroCostoId,
        AlmacenId = @AlmacenId,
        OrdenCompraId = @OrdenCompraId,
        EmpleadoCompradorId = @EmpleadoCompradorId,
        Serie = @Serie,
        Numero = @Numero,
        TipoDocumento = @TipoDocumento,
        FechaEmision = @FechaEmision,
        NumeroGuiaRemision = @NumeroGuiaRemision,
        Referencia = @Referencia,
        LocalProveedor = @LocalProveedor,
        TipoOperacionSunat = @TipoOperacionSunat,
        Moneda = @Moneda,
        TasaIgv = @TasaIgv,
        Subtotal = @Subtotal,
        Igv = @Igv,
        FechaTipoCambio = @FechaTipoCambio,
        TipoCambio = @TipoCambio,
        Total = @Total,
        GlosaComprobante = @GlosaComprobante,
        GlosaMovimiento = @GlosaMovimiento,
        FechaVencimiento = @FechaVencimiento,
        ConfiguracionCuentaOperacionId = @ConfiguracionCuentaOperacionId,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId AND ProveedorId = @ProveedorId;

    IF @@ROWCOUNT = 0
    BEGIN
        SET @Encontrada = 0;
        RETURN;
    END

    SET @Encontrada = 1;

    DELETE FROM dbo.CompraDetalles WHERE CompraId = @Id;

    INSERT INTO dbo.CompraDetalles (Id, CompraId, ProductoId, Cantidad, PrecioUnitario, PorcentajeDescuento, Total, CuentaContableId)
    SELECT Id, @Id, ProductoId, Cantidad, PrecioUnitario, PorcentajeDescuento, Total, CuentaContableId
    FROM @Detalles;
END
GO
