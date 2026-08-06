# FASE 09 — Portal Web Lecturas de Estudios

## Objetivo
Implementar el portal de lecturas y resultados de estudios. Permite a los
operadores/administradores revisar el estado de las lecturas, ver el avance
de radiólogos y gestionar estudios leídos vs pendientes.

## Vista: Lecturas/Index.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| DropdownList Empresa | `<select>` | Empresas activas |
| Fecha Inicial | `<input type="date">` | Rango de búsqueda |
| Fecha Final | `<input type="date">` | Rango de búsqueda |
| DropdownList Radiólogo | `<select>` | Filtrar por médico (todos = vacío) |
| Estado | `<select>` | PEN (Pendiente) / LEI (Leído) / TODO |
| Botón Consultar | `<button>` | |
| Grid Lecturas | `<table>` | Estudios filtrados de `estudiosdiagnosticos_programacion` |

### Columnas del grid
| Columna | Fuente |
|---------|--------|
| NoCuenta | estudiosdiagnosticos_programacion |
| Servicio | estudiosdiagnosticos_programacion |
| Médico | estudiosdiagnosticos_programacion (CedulaMedico) |
| Fecha Programación | FechaProgramacion |
| Fecha Asignación | FechaAsignacion |
| Estado | Estado |
| Tiene Audio | AudioRadiologo IS NOT NULL |
| Acción | [Ver Detalle] |

### ViewModel: `LecturasViewModel`
```csharp
public class LecturasViewModel
{
    // Filtros
    public string EmpresaSeleccionada { get; set; } = string.Empty;
    public DateOnly FechaInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly FechaFinal { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string CedulaMedicoFiltro { get; set; } = string.Empty;
    public string EstadoFiltro { get; set; } = "TODO";

    // Datos para controles
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();
    public List<MedicoDto> MedicosDisponibles { get; set; } = new();

    // Grid
    public List<LecturaDto> Lecturas { get; set; } = new();
}
```

### DTO: `LecturaDto`
```csharp
public record LecturaDto(
    string Empresa,
    long Consecutivo,
    decimal NoCuenta,
    string CedulaMedico,
    string NombreMedico,
    DateOnly FechaProgramacion,
    DateOnly FechaAsignacion,
    string CodServicio,
    string NombreServicio,
    string Estado,
    bool TieneAudio
);
```

## Vista detalle: Lecturas/Detalle.cshtml
- Información completa del estudio
- Diagnóstico (tab) → `IEsculapioApiClient.ObtenerDiagnosticoCuentaAsync`
- Notas Médicas (tab) → `IEsculapioApiClient.ObtenerNotasMedicasCuentaAsync`
- Player de audio si `TieneAudio == true`

## Proceso

### Consultar (POST)
1. Obtener estudios filtrados desde API (parámetros: empresa, fechas, médico, estado)
2. Retornar ViewModel con grid

### Ver Detalle (GET /Lecturas/Detalle/{consecutivo}?empresa=XX)
1. Cargar datos del estudio
2. Cargar diagnóstico y notas desde API
3. Si tiene audio → incluir flag para el player

## Seguridad
- `[Authorize(Roles = "Administrador,Operador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/LecturasController.cs`
- `src/WebImagenologia.Web/Models/ViewModels/LecturasViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/LecturaDto.cs`
- `src/WebImagenologia.Web/Views/Lecturas/Index.cshtml`
- `src/WebImagenologia.Web/Views/Lecturas/Detalle.cshtml`
- `src/WebImagenologia.Tests/LecturasTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `ui_routes`:
  - GET /Lecturas
  - POST /Lecturas/Consultar
  - GET /Lecturas/Detalle/{consecutivo}
