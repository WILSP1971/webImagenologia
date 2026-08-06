# FASE 07 — Condicionales > Automatización Programación de Estudios

## Objetivo
Implementar la UI de configuración de la automatización N8N y el CRUD de la tabla
`estudiosdiagnosticos_automatizacionwf`. La UI permite configurar el tipo (Radiólogos
u Operadores), frecuencia y hora de ejecución, y guarda la parametrización que
el workflow N8N leerá en cada ejecución.

## Vista: Condicional/Automatizacion.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Tipo de Programación | `<input type="radio">` | Opciones: "Radiólogos" / "Operador" |
| Frecuencia | `<select>` | Por ahora solo "Diario" (DIA) |
| Hora Inicio | `<input type="time">` | Formato HH:mm |
| Estado | `<input type="checkbox">` | Activo / Inactivo |
| Botón Registrar | `<button type="submit">` | Guarda la automatización |
| Grid Programaciones | `<table>` | Registros existentes en `estudiosdiagnosticos_automatizacionwf` |

### ViewModel: `AutomatizacionViewModel`
```csharp
public class AutomatizacionViewModel
{
    public int? IdAutomatizacion { get; set; }
    public string TipoProgramacion { get; set; } = "RAD";  // RAD | OPE
    public string Frecuencia { get; set; } = "DIA";
    public string HoraAutomatizacion { get; set; } = "06:00";
    public bool Activo { get; set; } = true;

    public List<AutomatizacionDto> AutomatizacionesRegistradas { get; set; } = new();
}
```

### DTO: `AutomatizacionDto`
```csharp
public record AutomatizacionDto(
    int IdAutomatizacion,
    string Frecuencia,       // char(3): DIA
    string HoraAutomatizacion,  // varchar(15): HH:mm
    string Estado            // char(3): ACT | INA
);
```

## Proceso

### Registro (POST)
1. Validar ModelState
2. POST a API → inserta/actualiza en `estudiosdiagnosticos_automatizacionwf`
3. **Invocar webhook N8N** para actualizar el schedule del workflow:
   ```
   POST https://n8n.esculapiosis.com/webhook/actualizar-schedule
   Body: { "frecuencia": "DIA", "hora": "06:00", "activo": true }
   ```
   Esta llamada se hace desde el servidor .NET vía `HttpClient` (un cliente separado
   del `EsculapioApiClient` — puede ser `IHttpClientFactory` con nombre "N8NClient").
4. Redirigir con TempData de resultado

### Activar / Desactivar
- Botón de toggle en el grid que llama al webhook N8N con `activo: true/false`
- Actualiza `Estado` en `estudiosdiagnosticos_automatizacionwf` vía API

## Nota sobre N8N
El workflow N8N se configura en Fase 11. En esta fase, el web solo debe poder
invocar el webhook de N8N con los parámetros correctos. Si el webhook no está
disponible, el orquestador debe registrar un warning (no BLOCKED) y continuar.

## Seguridad
- `[Authorize(Roles = "Administrador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/CondicionalController.cs` (Automatizacion actions)
- `src/WebImagenologia.Web/Models/ViewModels/AutomatizacionViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/AutomatizacionDto.cs`
- `src/WebImagenologia.Web/Views/Condicional/Automatizacion.cshtml`
- `src/WebImagenologia.Tests/AutomatizacionTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `ui_routes`:
  - GET /Condicional/Automatizacion
  - POST /Condicional/Automatizacion/Registrar
  - POST /Condicional/Automatizacion/ToggleEstado
