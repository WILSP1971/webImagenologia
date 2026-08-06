/*
  Stored Procedure: Get_ProgramacionEstudiosDiagnosticos
  Propósito: Distribuir los estudios sin resultado entre los radiólogos
             parametrizados, según la configuración de
             estudiosdiagnosticos_medicos, e insertarlos en
             estudiosdiagnosticos_programacion.

  Invocado desde: Workflow N8N (ProgramacionEstudiosDiagnosticos)
  NO invocar desde la aplicación web .NET.

  Lógica de distribución:
    1. Leer estudios pendientes de estudiosdiagnosticos_sinresultado (Estado='PEN')
    2. Por cada empresa, obtener el radiólogo con menor carga del día
    3. Respetar la cuota diaria de estudiosdiagnosticos_medicos
    4. Insertar en estudiosdiagnosticos_programacion
    5. Marcar como procesado en estudiosdiagnosticos_sinresultado

  NOTA: La lógica completa de distribución debe validarse con el área de
  negocio de Esculapio. El nombre de tabla de órdenes en ConsOrdenesResultados
  debe confirmarse antes de desplegar en producción.
*/

DELIMITER $$

DROP PROCEDURE IF EXISTS `Get_ProgramacionEstudiosDiagnosticos`$$

CREATE PROCEDURE `Get_ProgramacionEstudiosDiagnosticos`()
BEGIN
    DECLARE v_done           INT DEFAULT FALSE;
    DECLARE v_empresa        VARCHAR(12);
    DECLARE v_consecutivo    BIGINT(20);
    DECLARE v_noCuenta       DECIMAL(11,0);
    DECLARE v_noOrden        DECIMAL(11,0);
    DECLARE v_codServicio    VARCHAR(45);
    DECLARE v_dependencia    VARCHAR(12);
    DECLARE v_servicio       VARCHAR(6);
    DECLARE v_cedulaMedico   VARCHAR(45);
    DECLARE v_cantidadMax    DECIMAL(11,0);
    DECLARE v_fechaHoy       DATE;

    DECLARE cur_estudios CURSOR FOR
        SELECT
            sr.Empresa,
            sr.Consecutivo,
            sr.NoCuenta,
            sr.NoOrden,
            sr.CodServicio,
            sr.Dependencia,
            sr.Servicio
        FROM estudiosdiagnosticos_sinresultado sr
        WHERE sr.EstadoNoResultado = 'PEN'
        ORDER BY sr.Empresa, sr.Fecha_cargo, sr.Consecutivo;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = TRUE;

    SET v_fechaHoy = CURDATE();

    OPEN cur_estudios;

    loop_estudios: LOOP
        SET v_cedulaMedico = NULL;

        FETCH cur_estudios INTO
            v_empresa, v_consecutivo, v_noCuenta, v_noOrden,
            v_codServicio, v_dependencia, v_servicio;

        IF v_done THEN
            LEAVE loop_estudios;
        END IF;

        SELECT
            med.CedulaMedico,
            med.Cantidad
        INTO
            v_cedulaMedico,
            v_cantidadMax
        FROM estudiosdiagnosticos_medicos med
        WHERE med.Empresa        = v_empresa
          AND med.CodServicio    = v_codServicio
          AND med.CodDependencia = v_dependencia
          AND med.Estado         = 'ACT'
          AND (
              SELECT COUNT(*)
              FROM estudiosdiagnosticos_programacion prog
              WHERE prog.Empresa           = v_empresa
                AND prog.CedulaMedico      = med.CedulaMedico
                AND prog.FechaProgramacion = v_fechaHoy
          ) < med.Cantidad
        ORDER BY (
            SELECT COUNT(*)
            FROM estudiosdiagnosticos_programacion prog2
            WHERE prog2.Empresa           = v_empresa
              AND prog2.CedulaMedico      = med.CedulaMedico
              AND prog2.FechaProgramacion = v_fechaHoy
        ) ASC
        LIMIT 1;

        IF v_cedulaMedico IS NOT NULL THEN
            INSERT INTO estudiosdiagnosticos_programacion (
                Empresa,
                Consecutivo,
                NoCuenta,
                CedulaMedico,
                FechaProgramacion,
                Servicio,
                CodServicio,
                Dependencia,
                NoOrden,
                FechaAsignacion,
                Estado
            ) VALUES (
                v_empresa,
                v_consecutivo,
                v_noCuenta,
                v_cedulaMedico,
                v_fechaHoy,
                v_servicio,
                v_codServicio,
                v_dependencia,
                v_noOrden,
                v_fechaHoy,
                'PEN'
            );

            UPDATE estudiosdiagnosticos_sinresultado
            SET EstadoNoResultado = 'PRO'
            WHERE Empresa     = v_empresa
              AND Consecutivo = v_consecutivo;
        END IF;

    END LOOP loop_estudios;

    CLOSE cur_estudios;

    SELECT
        COALESCE(Empresa, 'ALL') AS Empresa,
        v_fechaHoy               AS FechaProgramacion,
        COUNT(*)                 AS TotalProgramados
    FROM estudiosdiagnosticos_programacion
    WHERE FechaProgramacion = v_fechaHoy
    GROUP BY Empresa;

END$$

DELIMITER ;
