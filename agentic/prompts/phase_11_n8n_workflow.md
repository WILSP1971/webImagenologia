# FASE 11 — Workflow N8N — Programación de Estudios

## Objetivo
Generar el archivo `n8n/workflows/programacion-estudios.json` con el workflow
completo para la distribución automática de estudios radiológicos.
El workflow se importa y configura en `https://n8n.esculapiosis.com`.

## Estructura del Workflow

### Nombre: `ProgramacionEstudiosDiagnosticos`
### Trigger: Cron (`Schedule Trigger`)
- Configurado por defecto para ejecutar diariamente a las 06:00
- El schedule se actualiza dinámicamente desde el web (Fase 07) vía webhook

### Nodos del workflow

#### Nodo 1: Schedule Trigger (Cron)
```json
{
  "type": "n8n-nodes-base.scheduleTrigger",
  "name": "Cron - Programacion Diaria",
  "parameters": {
    "rule": {
      "interval": [{ "field": "hours", "hoursInterval": 24 }]
    }
  }
}
```

#### Nodo 2: Leer configuración de automatización
```json
{
  "type": "n8n-nodes-base.mysql",
  "name": "Leer Config Automatizacion",
  "parameters": {
    "operation": "executeQuery",
    "query": "SELECT * FROM estudiosdiagnosticos_automatizacionwf WHERE Estado = 'ACT' LIMIT 1"
  }
}
```

#### Nodo 3: Verificar si está activo (IF)
```json
{
  "type": "n8n-nodes-base.if",
  "name": "¿Automatización Activa?",
  "parameters": {
    "conditions": {
      "string": [{ "value1": "={{ $json.Estado }}", "value2": "ACT" }]
    }
  }
}
```

#### Nodo 4: MySQL — ConsOrdenesResultados
```json
{
  "type": "n8n-nodes-base.mysql",
  "name": "Obtener Estudios Sin Resultado",
  "parameters": {
    "operation": "executeQuery",
    "query": "CALL ConsOrdenesResultados('{{ $env.EMPRESA }}', '{{ $today.format(\"YYYY-MM-DD\") }}', '{{ $today.format(\"YYYY-MM-DD\") }}', 'RX')"
  }
}
```

#### Nodo 5: MySQL — Get_ProgramacionEstudiosDiagnosticos
```json
{
  "type": "n8n-nodes-base.mysql",
  "name": "Programar Estudios a Radiologos",
  "parameters": {
    "operation": "executeQuery",
    "query": "CALL Get_ProgramacionEstudiosDiagnosticos()"
  }
}
```

#### Nodo 6: HTTP Request — Notificar al Web (opcional)
```json
{
  "type": "n8n-nodes-base.httpRequest",
  "name": "Notificar Resultado",
  "parameters": {
    "url": "={{ $env.WEB_CALLBACK_URL }}/Condicional/Automatizacion/NotificarEjecucion",
    "method": "POST",
    "jsonParameters": true,
    "bodyParametersJson": "={ \"fecha\": \"{{ $today }}\", \"estudios\": {{ $json.length }} }"
  }
}
```

## Stored Procedures a crear

### `ConsOrdenesResultados`
Archivo: `db/stored_procedures/ConsOrdenesResultados.sql`
```sql
-- Retorna estudios sin resultado dentro del rango de fechas
-- para la empresa y laboratorio indicados.
-- Inserta los resultados en estudiosdiagnosticos_sinresultado
-- y los devuelve como SELECT.
DELIMITER $$
CREATE PROCEDURE ConsOrdenesResultados(
    IN p_Empresa VARCHAR(12),
    IN p_FechaInicial DATE,
    IN p_FechaFinal DATE,
    IN p_LabRX VARCHAR(10)
)
BEGIN
    -- Limpiar resultados anteriores del día
    DELETE FROM estudiosdiagnosticos_sinresultado
    WHERE Empresa = p_Empresa
      AND Fecha_cargo BETWEEN p_FechaInicial AND p_FechaFinal;

    -- Insertar estudios sin resultado desde la tabla de órdenes
    -- (script completo se debe adaptar al esquema de órdenes de Esculapio)
    INSERT INTO estudiosdiagnosticos_sinresultado (
        Empresa, NoCuenta, NoOrden, CodServicio, Dependencia,
        Fecha_cargo, Servicio, Consecutivo, EstadoNoResultado
    )
    SELECT
        p_Empresa, o.NoCuenta, o.NoOrden, o.CodServicio, o.Dependencia,
        o.FechaCargo, o.Servicio, o.Consecutivo, 'PEN'
    FROM ordenes o  -- tabla de órdenes del sistema Esculapio (confirmar nombre real)
    WHERE o.Empresa = p_Empresa
      AND o.FechaCargo BETWEEN p_FechaInicial AND p_FechaFinal
      AND o.Servicio = p_LabRX
      AND o.NoResultado IS NULL;

    -- Retornar los estudios sin resultado
    SELECT * FROM estudiosdiagnosticos_sinresultado
    WHERE Empresa = p_Empresa
      AND Fecha_cargo BETWEEN p_FechaInicial AND p_FechaFinal;
END$$
DELIMITER ;
```

### `Get_ProgramacionEstudiosDiagnosticos`
Archivo: `db/stored_procedures/Get_ProgramacionEstudiosDiagnosticos.sql`
```sql
-- Distribuye los estudios sin resultado entre los radiólogos
-- según la parametrización de estudiosdiagnosticos_medicos
-- e inserta en estudiosdiagnosticos_programacion.
DELIMITER $$
CREATE PROCEDURE Get_ProgramacionEstudiosDiagnosticos()
BEGIN
    DECLARE done INT DEFAULT FALSE;
    -- Cursor sobre estudiosdiagnosticos_sinresultado (Estado = 'PEN')
    -- y parametrización de estudiosdiagnosticos_medicos
    -- Lógica de distribución round-robin por radiólogo y empresa
    -- Insertar en estudiosdiagnosticos_programacion
    -- Marcar como procesado en estudiosdiagnosticos_sinresultado
    -- (implementación completa según lógica de negocio de Esculapio)
    SELECT 'Programación completada' AS resultado;
END$$
DELIMITER ;
```

> **Nota para el Lead**: Los stored procedures tienen stubs. La lógica interna
> completa depende del esquema de órdenes de Esculapio (tabla `ordenes` o equivalente).
> Verificar el nombre real de la tabla de órdenes antes de implementar el SP.

## Variables de entorno N8N requeridas
```
EMPRESA=<código empresa por defecto>
WEB_CALLBACK_URL=<URL del servidor web>
```
Configurar en N8N Settings → Environment Variables.

## Archivos a generar
- `n8n/workflows/programacion-estudios.json` (workflow completo exportable)
- `db/stored_procedures/ConsOrdenesResultados.sql`
- `db/stored_procedures/Get_ProgramacionEstudiosDiagnosticos.sql`

## Gates de esta fase
- `build`: ok (no aplica .NET)
- JSON válido: `programacion-estudios.json` parseable sin errores
- SPs sintácticamente correctos (validar con `EXPLAIN`)
