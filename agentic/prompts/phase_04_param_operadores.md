# FASE 04 — Parámetros > Operadores por Empresa

## Objetivo
Implementar la vista y lógica de parametrización de operadores/funcionarios por empresa.
Sigue el mismo patrón arquitectónico que la Fase 03 (Radiólogos).

## Vista: Parametros/Operadores.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| DropdownList Operadores | `<select>` | Lista de operadores/funcionarios disponibles |
| Usuario Esculapio | `<input type="text">` | Se digita manualmente |
| Nombre Operador | `<input type="text">` readonly | Se rellena al seleccionar |
| Empresas (multicheckbox) | `<input type="checkbox">` múltiple | Empresas activas del servidor |
| Botón Agregar/Registrar | `<button type="submit">` | Registra el operador |
| Grid Operadores registrados | `<table>` | Lista de operadores guardados (Editar, Eliminar) |

### ViewModel: `OperadoresViewModel`
```csharp
public class OperadoresViewModel
{
    public string CedulaOperador { get; set; } = string.Empty;
    public string NombreOperador { get; set; } = string.Empty;
    public string UsuarioEsculapio { get; set; } = string.Empty;
    public List<string> EmpresasSeleccionadas { get; set; } = new();

    public List<OperadorDto> OperadoresDisponibles { get; set; } = new();
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();
    public List<OperadorRegistradoDto> OperadoresRegistrados { get; set; } = new();
}
```

## Proceso
Idéntico al de Fase 03 (Radiólogos) pero para la tabla `estudiosdiagnosticos_medicos`
en su columna de operadores:

### Carga inicial (GET)
1. Obtener empresas activas (sesión)
2. Obtener lista de operadores → endpoint API
3. Obtener grid de operadores registrados → endpoint API
4. Retornar ViewModel

### Registro (POST /Parametros/Operadores/Registrar)
1. Validar ModelState
2. Llamar API endpoint POST
3. Redirigir con TempData de resultado

### Editar / Eliminar
- Mismos patrones que Fase 03

## Seguridad
- `[Authorize(Roles = "Administrador")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/ParametrosController.cs` (Operadores actions — agregar al controller existente)
- `src/WebImagenologia.Web/Models/ViewModels/OperadoresViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/OperadorDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/OperadorRegistradoDto.cs`
- `src/WebImagenologia.Web/Views/Parametros/Operadores.cshtml`
- `src/WebImagenologia.Web/Views/Parametros/OperadoresEditar.cshtml`
- `src/WebImagenologia.Tests/ParametrosOperadoresTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `lint-cshtml`: ok
- `ui_routes`: GET /Parametros/Operadores, POST /Parametros/Operadores/Registrar
