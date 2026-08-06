/*
  Stored Procedure: ConsOrdenesResultados
  Propósito: Obtener estudios sin resultado en un rango de fechas
             para una empresa y laboratorio específicos.
             Inserta los resultados en estudiosdiagnosticos_sinresultado
             y los retorna como SELECT.

  Invocado desde: Workflow N8N (ProgramacionEstudiosDiagnosticos)
  NO invocar desde la aplicación web .NET.

  Parámetros:
    p_Empresa      VARCHAR(12)  — Código de la empresa
    p_FechaInicial DATE         — Fecha inicio del rango
    p_FechaFinal   DATE         — Fecha fin del rango
    p_LabRX        VARCHAR(10)  — Código de laboratorio RX

  NOTA: El nombre de la tabla de órdenes del sistema Esculapio debe
  confirmarse con el equipo de BD antes de implementar la lógica de INSERT.
  El stub actual usa un placeholder 'ordenes_esculapio'.
*/

DELIMITER $$

DROP PROCEDURE IF EXISTS `ConsOrdenesResultados`$$

CREATE PROCEDURE `ConsOrdenesResultados`(
    IN p_Empresa       VARCHAR(12),
    IN p_FechaInicial  DATE,
    IN p_FechaFinal    DATE,
    IN p_LabRX         VARCHAR(10)
)
BEGIN
    -- Limpiar resultados anteriores del día para evitar duplicados
    DELETE FROM estudiosdiagnosticos_sinresultado
    WHERE Empresa = p_Empresa
      AND Fecha_cargo BETWEEN p_FechaInicial AND p_FechaFinal;

    -- ─────────────────────────────────────────────────────────────────────
    -- STUB: Reemplazar 'ordenes_esculapio' con el nombre real de la tabla
    -- de órdenes del sistema Esculapio. Confirmar columnas con el equipo.
    -- ─────────────────────────────────────────────────────────────────────
    INSERT INTO estudiosdiagnosticos_sinresultado (
        Empresa,
        NoCuenta,
        NoIdentificacion,
        NoOrden,
        Valor,
        CodServicio,
        nombreServicio,
        Dependencia,
        Fecha_cargo,
        codEsquema,
        Servicio,
        fecha_real_cargo,
        hora_real_cargo,
        Usuario_grabacargo,
        Consecutivo,
        NoResultado,
        Reporte,
        EstadoNoResultado
    )
    SELECT
        p_Empresa,
        o.NoCuenta,
        o.NoIdentificacion,
        o.NoOrden,
        o.Valor,
        o.CodServicio,
        o.NombreServicio,
        o.Dependencia,
        o.FechaCargo,
        o.CodEsquema,
        o.Servicio,
        o.FechaRealCargo,
        o.HoraRealCargo,
        o.UsuarioGrabaCargo,
        o.Consecutivo,
        NULL,     -- NoResultado: sin resultado aún
        'NO',     -- Reporte: no reportado
        'PEN'     -- EstadoNoResultado: Pendiente
    FROM ordenes_esculapio o   -- ← CONFIRMAR nombre real de tabla
    WHERE o.Empresa   = p_Empresa
      AND o.FechaCargo BETWEEN p_FechaInicial AND p_FechaFinal
      AND o.Servicio  = p_LabRX
      AND (o.NoResultado IS NULL OR o.NoResultado = '');

    -- Retornar todos los estudios sin resultado para este rango
    SELECT *
    FROM estudiosdiagnosticos_sinresultado
    WHERE Empresa    = p_Empresa
      AND Fecha_cargo BETWEEN p_FechaInicial AND p_FechaFinal
      AND EstadoNoResultado = 'PEN';

END$$

DELIMITER ;
