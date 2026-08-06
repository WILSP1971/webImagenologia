# FASE 06 — Condicionales > Asignación de No. de Estudios por Empresa

## Objetivo
Implementar la vista de asignación de número de estudios por empresa (Lectura / No Lectura).
Opera sobre la tabla `estudiosdiagnosticos_medicos` — asigna cuántos estudios
corresponden a cada médico por empresa/servicio.

## Vista: Condicional/Asignacion.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| DropdownList Empresa | `<select>` | Empresas activas del servidor |
| DropdownList Médico | `<select>` | Médicos registrados en la empresa seleccionada |
| DropdownList Dependencia | `<select>` | Dependencias de la empresa |
| DropdownList Servicio | `<select>` | Servicios por dependencia |
| Cantidad | `<input type="number">` | Cantidad de estudios a asignar al médico |
| Estado | `<select>` | ACT / INA |
| Botón Registrar | `<button type="submit">` | |
| Grid de asignaciones | `<table>` | Registros de `estudiosdiagnosticos_medicos` (Editar, Eliminar) |

### ViewModel: `AsignacionViewModel`
```csharp
public class AsignacionViewModel
{
    public string Empresa { get; set; } = string.Empty;
    public string CedulaMedico { get; set; } = string.Empty;
    public string CodDependencia { get; set; } = string.Empty;
    public string CodServicio { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Estado { get; set; } = "ACT";

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();
    public List<MedicoDto> MedicosDisponibles { get; set; } = new();
    public List<DependenciaDto> DependenciasDisponibles { get; set; } = new();
    public List<ServicioDto> ServiciosDisponibles { get; set; } = new();

    public List<AsignacionMedicoDto> AsignacionesRegistradas { get; set; } = new();
}
```

### DTO: `AsignacionMedicoDto`
```csharp
public record AsignacionMedicoDto(
    string Empresa,
    string CedulaMedico,
    string NombreMedico,
    string CodDependencia,
    string CodServicio,
    decimal Cantidad,
    string Estado
);
```

## Proceso

### Filtrado en cascada (AJAX)
```
GET /Condicional/Asignacion/MedicosPorEmpresa?empresa=XX
GET /Condicional/Asignacion/DependenciasPorEmpresa?empresa=XX
GET /Condicional/Asignacion/ServiciosPorDependencia?codDependencia=XX
```

Cada endpoint retorna JSON para actualizar los dropdowns dependientes.

### Registro (POST)
1. Validar ModelState
2. POST a API → registra en `estudiosdiagnosticos_medicos`
3. Redirigir con TempData

### Grid
- Filtrar por empresa seleccionada
- Botones Editar y Eliminar con confirmación modal

## Seguridad
- `[Authorize(Roles = "Administrador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/CondicionalController.cs` (Asignacion actions)
- `src/WebImagenologia.Web/Models/ViewModels/AsignacionViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/AsignacionMedicoDto.cs`
- `src/WebImagenologia.Web/Views/Condicional/Asignacion.cshtml`
- `src/WebImagenologia.Tests/AsignacionTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `ui_routes`:
  - GET /Condicional/Asignacion
  - POST /Condicional/Asignacion/Registrar
  - GET /Condicional/Asignacion/MedicosPorEmpresa
