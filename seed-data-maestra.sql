-- Seed de data maestra para SISERP (ENMIKADOSBD)
-- Carga: almacén principal, categorías base, series de comprobante y plan de cuentas (PCGE simplificado)
-- para la empresa existente. Idempotente: usa IF NOT EXISTS antes de cada insert.

DECLARE @EmpresaId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Empresas ORDER BY CreatedAt);

IF @EmpresaId IS NULL
BEGIN
    PRINT 'No hay ninguna Empresa registrada; no se cargó data maestra.';
    RETURN;
END

-- 1) Almacén principal
IF NOT EXISTS (SELECT 1 FROM Almacenes WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO Almacenes (Id, EmpresaId, Nombre, Direccion, Principal)
    VALUES (NEWID(), @EmpresaId, N'Almacén Principal', NULL, 1);
END

-- 1.b) Centros de costo base (RRHH / planilla): Operaciones, Ventas, Sistemas
IF NOT EXISTS (SELECT 1 FROM CentrosCosto WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO CentrosCosto (Id, EmpresaId, Nombre, Descripcion, Activo, CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), @EmpresaId, N'Operaciones', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (NEWID(), @EmpresaId, N'Ventas', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
        (NEWID(), @EmpresaId, N'Sistemas', NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
END

-- 2) Categorías base de productos (tabla global, no depende de EmpresaId)
IF NOT EXISTS (SELECT 1 FROM Categorias)
BEGIN
    INSERT INTO Categorias (Id, Nombre) VALUES
        (NEWID(), N'General'),
        (NEWID(), N'Mercadería'),
        (NEWID(), N'Materia Prima'),
        (NEWID(), N'Producto Terminado'),
        (NEWID(), N'Servicios');
END

-- 3) Series de comprobante (factura F001, boleta B001)
IF NOT EXISTS (SELECT 1 FROM SeriesComprobante WHERE EmpresaId = @EmpresaId AND TipoComprobante = 'FACTURA')
BEGIN
    INSERT INTO SeriesComprobante (Id, EmpresaId, TipoComprobante, Serie, CorrelativoActual)
    VALUES (NEWID(), @EmpresaId, 'FACTURA', N'F001', 0);
END

IF NOT EXISTS (SELECT 1 FROM SeriesComprobante WHERE EmpresaId = @EmpresaId AND TipoComprobante = 'BOLETA')
BEGIN
    INSERT INTO SeriesComprobante (Id, EmpresaId, TipoComprobante, Serie, CorrelativoActual)
    VALUES (NEWID(), @EmpresaId, 'BOLETA', N'B001', 0);
END

-- 4) Plan de cuentas contable (PCGE simplificado, cuentas de nivel 2 más usadas)
IF NOT EXISTS (SELECT 1 FROM CuentasContables WHERE EmpresaId = @EmpresaId)
BEGIN
    INSERT INTO CuentasContables (Id, EmpresaId, Codigo, Nombre, Tipo, Naturaleza, CuentaPadreId, Activa) VALUES
        (NEWID(), @EmpresaId, '10', N'Efectivo y Equivalentes de Efectivo', 'ACTIVO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '12', N'Cuentas por Cobrar Comerciales - Terceros', 'ACTIVO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '20', N'Mercaderías', 'ACTIVO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '25', N'Materias Primas', 'ACTIVO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '33', N'Inmuebles, Maquinaria y Equipo', 'ACTIVO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '40', N'Tributos, Contraprestaciones y Aportes por Pagar', 'PASIVO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '42', N'Cuentas por Pagar Comerciales - Terceros', 'PASIVO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '46', N'Cuentas por Pagar Diversas - Terceros', 'PASIVO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '50', N'Capital', 'PATRIMONIO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '59', N'Resultados Acumulados', 'PATRIMONIO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '60', N'Compras', 'COSTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '69', N'Costo de Ventas', 'COSTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '62', N'Gastos de Personal', 'GASTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '63', N'Gastos de Servicios Prestados por Terceros', 'GASTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '64', N'Gastos por Tributos', 'GASTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '65', N'Otros Gastos de Gestión', 'GASTO', 'DEUDORA', NULL, 1),
        (NEWID(), @EmpresaId, '70', N'Ventas', 'INGRESO', 'ACREEDORA', NULL, 1),
        (NEWID(), @EmpresaId, '75', N'Otros Ingresos de Gestión', 'INGRESO', 'ACREEDORA', NULL, 1);
END

-- Resumen final
SELECT
    (SELECT COUNT(*) FROM Almacenes WHERE EmpresaId = @EmpresaId) AS Almacenes,
    (SELECT COUNT(*) FROM Categorias) AS Categorias,
    (SELECT COUNT(*) FROM SeriesComprobante WHERE EmpresaId = @EmpresaId) AS SeriesComprobante,
    (SELECT COUNT(*) FROM CuentasContables WHERE EmpresaId = @EmpresaId) AS CuentasContables;
