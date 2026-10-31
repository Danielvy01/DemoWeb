-- ============================================================================
-- Stored procedures de alta (Registrar), modificación (Modificar) y baja
-- lógica (EliminarLogico) para las entidades maestras del ERP.
-- Forman el adaptador de persistencia de escritura de la arquitectura
-- hexagonal (Infrastructure/Persistence/Repositories/*SqlRepository.cs).
--
-- Ejecutar contra la base ENMIKADOSBD (SQL Server) antes de desplegar el
-- backend. Es idempotente (CREATE OR ALTER).
-- ============================================================================

-- ==================== CLIENTES ====================
CREATE OR ALTER PROCEDURE dbo.sp_Cliente_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @TipoDocumento NVARCHAR(30),
    @NumeroDocumento NVARCHAR(20),
    @NombreORazonSocial NVARCHAR(255),
    @Direccion NVARCHAR(255) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @DiasCredito INT = 0,
    @Clasificacion NVARCHAR(30) = 'NORMAL'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.Clientes
        (Id, EmpresaId, TipoDocumento, NumeroDocumento, NombreORazonSocial, Direccion, Email, Telefono,
         DiasCredito, Clasificacion, Activo, Bloqueado, CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @TipoDocumento, @NumeroDocumento, @NombreORazonSocial, @Direccion, @Email, @Telefono,
         @DiasCredito, @Clasificacion, 1, 0, @Now, @Now);

    SELECT * FROM dbo.Clientes WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Cliente_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @TipoDocumento NVARCHAR(30) = NULL,
    @NumeroDocumento NVARCHAR(20) = NULL,
    @NombreORazonSocial NVARCHAR(255) = NULL,
    @Direccion NVARCHAR(255) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @DiasCredito INT = NULL,
    @Clasificacion NVARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Clientes SET
        TipoDocumento = COALESCE(@TipoDocumento, TipoDocumento),
        NumeroDocumento = COALESCE(@NumeroDocumento, NumeroDocumento),
        NombreORazonSocial = COALESCE(@NombreORazonSocial, NombreORazonSocial),
        Direccion = COALESCE(@Direccion, Direccion),
        Email = COALESCE(@Email, Email),
        Telefono = COALESCE(@Telefono, Telefono),
        DiasCredito = COALESCE(@DiasCredito, DiasCredito),
        Clasificacion = COALESCE(@Clasificacion, Clasificacion),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Clientes WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Cliente_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Clientes SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Clientes WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

-- ==================== PROVEEDORES ====================
CREATE OR ALTER PROCEDURE dbo.sp_Proveedor_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Ruc NVARCHAR(20),
    @RazonSocial NVARCHAR(255),
    @Direccion NVARCHAR(255) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @DiasCredito INT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.Proveedores
        (Id, EmpresaId, Ruc, RazonSocial, Direccion, Email, Telefono, DiasCredito, Activo, Bloqueado, CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @Ruc, @RazonSocial, @Direccion, @Email, @Telefono, @DiasCredito, 1, 0, @Now, @Now);

    SELECT * FROM dbo.Proveedores WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Proveedor_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Ruc NVARCHAR(20) = NULL,
    @RazonSocial NVARCHAR(255) = NULL,
    @Direccion NVARCHAR(255) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @DiasCredito INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Proveedores SET
        Ruc = COALESCE(@Ruc, Ruc),
        RazonSocial = COALESCE(@RazonSocial, RazonSocial),
        Direccion = COALESCE(@Direccion, Direccion),
        Email = COALESCE(@Email, Email),
        Telefono = COALESCE(@Telefono, Telefono),
        DiasCredito = COALESCE(@DiasCredito, DiasCredito),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Proveedores WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Proveedor_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Proveedores SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Proveedores WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

-- ==================== CATEGORIAS ====================
CREATE OR ALTER PROCEDURE dbo.sp_Categoria_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Nombre NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.Categorias (Id, EmpresaId, Nombre, Activo, CreatedAt, UpdatedAt)
    VALUES (@Id, @EmpresaId, @Nombre, 1, @Now, @Now);

    SELECT * FROM dbo.Categorias WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Categoria_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Nombre NVARCHAR(255) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Categorias SET
        Nombre = COALESCE(@Nombre, Nombre),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Categorias WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Categoria_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Categorias SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Categorias WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

-- ==================== PRODUCTOS ====================
CREATE OR ALTER PROCEDURE dbo.sp_Producto_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @CategoriaId UNIQUEIDENTIFIER = NULL,
    @Codigo NVARCHAR(50),
    @CodigoAlternativo NVARCHAR(50) = NULL,
    @Descripcion NVARCHAR(500),
    @DescripcionAlternativa NVARCHAR(500) = NULL,
    @UnidadMedida NVARCHAR(10) = 'NIU',
    @PrecioVenta DECIMAL(12,2),
    @PrecioLista DECIMAL(12,2) = NULL,
    @PrecioCosto DECIMAL(12,2) = 0,
    @MonedaCompra NVARCHAR(5) = 'PEN',
    @AfectoIGV BIT = 1,
    @TipoExistencia NVARCHAR(2) = '01',
    @EsServicio BIT = 0,
    @StockMinimo DECIMAL(12,2) = 0,
    @StockMaximo DECIMAL(12,2) = 0,
    @DescuentoMayoristaPct DECIMAL(5,2) = 0,
    @DescuentoSuperMayoristaPct DECIMAL(5,2) = 0,
    @Observaciones NVARCHAR(1000) = NULL,
    @CuentaVentaId UNIQUEIDENTIFIER = NULL,
    @CuentaCostoVentaId UNIQUEIDENTIFIER = NULL,
    @CuentaExistenciasId UNIQUEIDENTIFIER = NULL,
    @CodigoBienServicio NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.Productos
        (Id, EmpresaId, CategoriaId, Codigo, CodigoAlternativo, Descripcion, DescripcionAlternativa, UnidadMedida,
         PrecioVenta, PrecioLista, PrecioCosto, MonedaCompra, AfectoIGV, TipoExistencia, EsServicio,
         StockMinimo, StockMaximo, DescuentoMayoristaPct, DescuentoSuperMayoristaPct, Observaciones,
         CuentaVentaId, CuentaCostoVentaId, CuentaExistenciasId, CodigoBienServicio,
         Activo, CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @CategoriaId, @Codigo, @CodigoAlternativo, @Descripcion, @DescripcionAlternativa, @UnidadMedida,
         @PrecioVenta, @PrecioLista, @PrecioCosto, @MonedaCompra, @AfectoIGV, @TipoExistencia, @EsServicio,
         @StockMinimo, @StockMaximo, @DescuentoMayoristaPct, @DescuentoSuperMayoristaPct, @Observaciones,
         @CuentaVentaId, @CuentaCostoVentaId, @CuentaExistenciasId, @CodigoBienServicio,
         1, @Now, @Now);

    SELECT * FROM dbo.Productos WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @CategoriaId UNIQUEIDENTIFIER = NULL,
    @Codigo NVARCHAR(50) = NULL,
    @CodigoAlternativo NVARCHAR(50) = NULL,
    @Descripcion NVARCHAR(500) = NULL,
    @DescripcionAlternativa NVARCHAR(500) = NULL,
    @UnidadMedida NVARCHAR(10) = NULL,
    @PrecioVenta DECIMAL(12,2) = NULL,
    @PrecioLista DECIMAL(12,2) = NULL,
    @PrecioCosto DECIMAL(12,2) = NULL,
    @MonedaCompra NVARCHAR(5) = NULL,
    @AfectoIGV BIT = NULL,
    @TipoExistencia NVARCHAR(2) = NULL,
    @EsServicio BIT = NULL,
    @StockMinimo DECIMAL(12,2) = NULL,
    @StockMaximo DECIMAL(12,2) = NULL,
    @DescuentoMayoristaPct DECIMAL(5,2) = NULL,
    @DescuentoSuperMayoristaPct DECIMAL(5,2) = NULL,
    @Observaciones NVARCHAR(1000) = NULL,
    @CategoriaIdSet BIT = 0, -- 1 indica que se debe sobrescribir CategoriaId aunque sea NULL (desasignar categoría)
    @CuentaVentaId UNIQUEIDENTIFIER = NULL,
    @CuentaVentaIdSet BIT = 0,
    @CuentaCostoVentaId UNIQUEIDENTIFIER = NULL,
    @CuentaCostoVentaIdSet BIT = 0,
    @CuentaExistenciasId UNIQUEIDENTIFIER = NULL,
    @CuentaExistenciasIdSet BIT = 0,
    @CodigoBienServicio NVARCHAR(10) = NULL,
    @CodigoBienServicioSet BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Productos SET
        CategoriaId = CASE WHEN @CategoriaIdSet = 1 THEN @CategoriaId ELSE COALESCE(@CategoriaId, CategoriaId) END,
        Codigo = COALESCE(@Codigo, Codigo),
        CodigoAlternativo = COALESCE(@CodigoAlternativo, CodigoAlternativo),
        Descripcion = COALESCE(@Descripcion, Descripcion),
        DescripcionAlternativa = COALESCE(@DescripcionAlternativa, DescripcionAlternativa),
        UnidadMedida = COALESCE(@UnidadMedida, UnidadMedida),
        PrecioVenta = COALESCE(@PrecioVenta, PrecioVenta),
        PrecioLista = COALESCE(@PrecioLista, PrecioLista),
        PrecioCosto = COALESCE(@PrecioCosto, PrecioCosto),
        MonedaCompra = COALESCE(@MonedaCompra, MonedaCompra),
        AfectoIGV = COALESCE(@AfectoIGV, AfectoIGV),
        TipoExistencia = COALESCE(@TipoExistencia, TipoExistencia),
        EsServicio = COALESCE(@EsServicio, EsServicio),
        StockMinimo = COALESCE(@StockMinimo, StockMinimo),
        StockMaximo = COALESCE(@StockMaximo, StockMaximo),
        DescuentoMayoristaPct = COALESCE(@DescuentoMayoristaPct, DescuentoMayoristaPct),
        DescuentoSuperMayoristaPct = COALESCE(@DescuentoSuperMayoristaPct, DescuentoSuperMayoristaPct),
        Observaciones = COALESCE(@Observaciones, Observaciones),
        CuentaVentaId = CASE WHEN @CuentaVentaIdSet = 1 THEN @CuentaVentaId ELSE COALESCE(@CuentaVentaId, CuentaVentaId) END,
        CuentaCostoVentaId = CASE WHEN @CuentaCostoVentaIdSet = 1 THEN @CuentaCostoVentaId ELSE COALESCE(@CuentaCostoVentaId, CuentaCostoVentaId) END,
        CuentaExistenciasId = CASE WHEN @CuentaExistenciasIdSet = 1 THEN @CuentaExistenciasId ELSE COALESCE(@CuentaExistenciasId, CuentaExistenciasId) END,
        CodigoBienServicio = CASE WHEN @CodigoBienServicioSet = 1 THEN @CodigoBienServicio ELSE COALESCE(@CodigoBienServicio, CodigoBienServicio) END,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Productos WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Producto_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Productos SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Productos WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

-- ==================== CENTROS DE COSTO ====================
CREATE OR ALTER PROCEDURE dbo.sp_CentroCosto_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Nombre NVARCHAR(150),
    @Descripcion NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.CentrosCosto (Id, EmpresaId, Nombre, Descripcion, Activo, CreatedAt, UpdatedAt)
    VALUES (@Id, @EmpresaId, @Nombre, @Descripcion, 1, @Now, @Now);

    SELECT * FROM dbo.CentrosCosto WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CentroCosto_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Nombre NVARCHAR(150) = NULL,
    @Descripcion NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CentrosCosto SET
        Nombre = COALESCE(@Nombre, Nombre),
        Descripcion = COALESCE(@Descripcion, Descripcion),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.CentrosCosto WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_CentroCosto_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.CentrosCosto SET Activo = 0, UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.CentrosCosto WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

-- ==================== EMPLEADOS ====================
CREATE OR ALTER PROCEDURE dbo.sp_Empleado_Registrar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @NumeroDocumento NVARCHAR(20),
    @Nombres NVARCHAR(255),
    @Apellidos NVARCHAR(255),
    @CentroCostoId UNIQUEIDENTIFIER,
    @Cargo NVARCHAR(100),
    @EsComprador BIT = 0,
    @SalarioBase DECIMAL(12,2),
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @CuentaBancaria NVARCHAR(20) = NULL,
    @BancoNombre NVARCHAR(50) = NULL,
    @CCI NVARCHAR(20) = NULL,
    @FechaIngreso DATETIME2 = NULL,
    @TipoContrato NVARCHAR(30) = NULL,
    @RegimenPensionario NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    INSERT INTO dbo.Empleados
        (Id, EmpresaId, NumeroDocumento, Nombres, Apellidos, CentroCostoId, Cargo, EsComprador, SalarioBase, Email, Telefono,
         CuentaBancaria, BancoNombre, CCI, FechaIngreso, TipoContrato, RegimenPensionario, Activo, CreatedAt, UpdatedAt)
    VALUES
        (@Id, @EmpresaId, @NumeroDocumento, @Nombres, @Apellidos, @CentroCostoId, @Cargo, @EsComprador, @SalarioBase, @Email, @Telefono,
         @CuentaBancaria, @BancoNombre, @CCI, COALESCE(@FechaIngreso, @Now), @TipoContrato, @RegimenPensionario, 1, @Now, @Now);

    SELECT * FROM dbo.Empleados WHERE Id = @Id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Empleado_Modificar
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @CentroCostoId UNIQUEIDENTIFIER = NULL,
    @Cargo NVARCHAR(100) = NULL,
    @EsComprador BIT = NULL,
    @SalarioBase DECIMAL(12,2) = NULL,
    @Email NVARCHAR(255) = NULL,
    @Telefono NVARCHAR(30) = NULL,
    @CuentaBancaria NVARCHAR(20) = NULL,
    @BancoNombre NVARCHAR(50) = NULL,
    @CCI NVARCHAR(20) = NULL,
    @Activo BIT = NULL,
    @TipoContrato NVARCHAR(30) = NULL,
    @RegimenPensionario NVARCHAR(10) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Empleados SET
        CentroCostoId = COALESCE(@CentroCostoId, CentroCostoId),
        Cargo = COALESCE(@Cargo, Cargo),
        EsComprador = COALESCE(@EsComprador, EsComprador),
        SalarioBase = COALESCE(@SalarioBase, SalarioBase),
        Email = COALESCE(@Email, Email),
        Telefono = COALESCE(@Telefono, Telefono),
        CuentaBancaria = COALESCE(@CuentaBancaria, CuentaBancaria),
        BancoNombre = COALESCE(@BancoNombre, BancoNombre),
        CCI = COALESCE(@CCI, CCI),
        Activo = COALESCE(@Activo, Activo),
        TipoContrato = COALESCE(@TipoContrato, TipoContrato),
        RegimenPensionario = COALESCE(@RegimenPensionario, RegimenPensionario),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Empleados WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Empleado_EliminarLogico
    @Id UNIQUEIDENTIFIER,
    @EmpresaId UNIQUEIDENTIFIER,
    @Motivo NVARCHAR(255) = NULL,
    @Fecha DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Empleados SET
        Activo = 0,
        FechaCese = COALESCE(@Fecha, SYSUTCDATETIME()),
        MotivoCese = @Motivo,
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @Id AND EmpresaId = @EmpresaId;

    SELECT * FROM dbo.Empleados WHERE Id = @Id AND EmpresaId = @EmpresaId;
END
GO
