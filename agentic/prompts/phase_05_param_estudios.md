# FASE 05 — Condicionales > Parametrización de Estudios

## Objetivo
Implementar la vista y lógica de parametrización de la cantidad de estudios
por dependencia/servicio y empresa. Escribe en `estudiosdiagnosticos_empresa`.

## Vista: Parametros/Estudios.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| DropdownList Dependencias | `<select>` | Dependencias disponibles (API) |
| Nombre Dependencia | `<input type="text">` readonly | Se rellena al seleccionar |
| DropdownList Servicios | `<select>` | Servicios por dependencia (API, se filtra según dependencia) |
| Nombre Servicio | `<input type="text">` readonly | Se rellena al seleccionar |
| Cantidad | `<input type="number">` | Cantidad entera de estudios |
| Lectura/No Lectura | `<input type="checkbox">` | Default: checked (Lectura) |
| Empresas (multicheckbox) | `<input type="checkbox">` múltiple | Empresas activas del servidor |
| Botón Registrar | `<button type="submit">` | |
| Grid Servicios registrados | `<table>` | Registros en `estudiosdiagnosticos_empresa` (Editar, Eliminar) |

### ViewModel: `EstudiosViewModel`
```csharp
public class EstudiosViewModel
{
    public string CodDependencia { get; set; } = string.Empty;
    public string NombreDependencia { get; set; } = string.Empty;
    public string CodServicio { get; set; } = string.Empty;
    public string NombreServicio { get; set; } = string.Empty;
    public string CodEsquema { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public bool EsLectura { get; set; } = true;  // Lectura=true, NoLectura=false
    public List<string> EmpresasSeleccionadas { get; set; } = new();

    // Datos para controles
    public List<DependenciaDto> DependenciasDisponibles { get; set; } = new();
    public List<ServicioDto> ServiciosDisponibles { get; set; } = new();  // filtrado vía AJAX
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();

    // Grid
    public List<EstudioEmpresaDto> EstudiosRegistrados { get; set; } = new();
}
```

### DTOs adicionales
```csharp
public record DependenciaDto(string CodDependencia, string NombreDependencia);
public record ServicioDto(string CodServicio, string NombreServicio, string CodDependencia, string CodEsquema);
public record EstudioEmpresaDto(
    string Empresa, string CodDependencia, string CodServicio,
    string CodEsquema, decimal Cantidad, string Estado);
```

## Proceso

### Carga inicial (GET)
1. Obtener dependencias → API
2. Obtener empresas activas → API
3. Obtener grid → API (tabla `estudiosdiagnosticos_empresa`)
4. Retornar ViewModel (servicios vacíos, se cargan vía AJAX)

### AJAX — Cargar servicios por dependencia
```
GET /Parametros/Estudios/ServiciosPorDependencia?codDependencia=XX
→ retorna JSON: [ { CodServicio, NombreServicio } ]
```

### Registro (POST)
1. Por cada empresa seleccionada → POST a API con todos los campos
2. Redirigir con TempData de resultado

### Editar / Eliminar
- Editar: GET con datos prellenados
- Eliminar: confirmación modal + POST a API

## Seguridad
- `[Authorize(Roles = "Administrador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/ParametrosController.cs` (Estudios actions)
- `src/WebImagenologia.Web/Models/ViewModels/EstudiosViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/DependenciaDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/ServicioDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/EstudioEmpresaDto.cs`
- `src/WebImagenologia.Web/Views/Parametros/Estudios.cshtml`
- `src/WebImagenologia.Web/wwwroot/js/parametros.js` (AJAX de servicios)
- `src/WebImagenologia.Tests/ParametrosEstudiosTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `ui_routes`:
  - GET /Parametros/Estudios
  - POST /Parametros/Estudios/Registrar
  - GET /Parametros/Estudios/ServiciosPorDependencia (endpoint AJAX)
