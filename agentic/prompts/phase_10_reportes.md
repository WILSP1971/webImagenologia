# FASE 10 — Consultas / Reportes

## Objetivo
Implementar el módulo de consultas y reportes con filtros por empresa, radiólogo,
servicio y rango de fechas. Incluye exportación a Excel/PDF básica.

## Vista: Reportes/Index.cshtml

### Filtros disponibles
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Empresa | `<select>` | Empresa (multicheckbox o dropdown) |
| Radiólogo | `<select>` | Filtrar por médico |
| Servicio | `<select>` | Filtrar por servicio |
| Dependencia | `<select>` | Filtrar por dependencia |
| Fecha Inicial | `<input type="date">` | |
| Fecha Final | `<input type="date">` | |
| Estado | `<select>` | PEN / LEI / TODO |
| Botón Consultar | `<button>` | |
| Botón Exportar Excel | `<button>` | Exporta el grid a Excel |

### Reportes disponibles (tabs o links)
1. **Resumen por Radiólogo** — cuántos estudios asignados vs leídos por médico
2. **Detalle de Estudios** — grid completo con todos los campos
3. **Estudios Sin Resultado** — basado en `estudiosdiagnosticos_sinresultado`
4. **Programación por Empresa** — vista de `estudiosdiagnosticos_programacion`

### ViewModel: `ReportesViewModel`
```csharp
public class ReportesViewModel
{
    // Filtros
    public List<string> EmpresasSeleccionadas { get; set; } = new();
    public string CedulaMedico { get; set; } = string.Empty;
    public string CodServicio { get; set; } = string.Empty;
    public string CodDependencia { get; set; } = string.Empty;
    public DateOnly FechaInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
    public DateOnly FechaFinal { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Estado { get; set; } = "TODO";
    public string TipoReporte { get; set; } = "DETALLE";

    // Datos para controles
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();
    public List<MedicoDto> MedicosDisponibles { get; set; } = new();
    public List<ServicioDto> ServiciosDisponibles { get; set; } = new();
    public List<DependenciaDto> DependenciasDisponibles { get; set; } = new();

    // Resultados
    public List<ReporteDetalleDto> DetalleEstudios { get; set; } = new();
    public List<ResumenRadiologoDto> ResumenRadiologos { get; set; } = new();
}
```

### DTOs
```csharp
public record ReporteDetalleDto(
    string Empresa, long Consecutivo, decimal NoCuenta,
    string NombreMedico, string NombreServicio, string NombreDependencia,
    DateOnly FechaProgramacion, string Estado, bool TieneAudio);

public record ResumenRadiologoDto(
    string CedulaMedico, string NombreMedico,
    int TotalAsignados, int TotalLeidos, int TotalPendientes);
```

## Exportación a Excel
- Usar `ClosedXML` (NuGet) para generar `.xlsx`
- Endpoint: `GET /Reportes/ExportarExcel` con los mismos parámetros del filtro
- Retornar `FileResult` con content-type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

## Seguridad
- `[Authorize(Roles = "Administrador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/ReportesController.cs`
- `src/WebImagenologia.Web/Models/ViewModels/ReportesViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/ReporteDetalleDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/ResumenRadiologoDto.cs`
- `src/WebImagenologia.Web/Views/Reportes/Index.cshtml`
- `src/WebImagenologia.Tests/ReportesTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `ui_routes`:
  - GET /Reportes
  - POST /Reportes/Consultar
  - GET /Reportes/ExportarExcel
