# FASE 03 — Parámetros > Radiólogos por Empresa

## Objetivo
Implementar la vista y lógica completa de parametrización de radiólogos por empresa.

## Vista: Parametros/Radiologos.cshtml

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| DropdownList Médicos | `<select>` | Lista de médicos radiólogos disponibles |
| Nombre Médico | `<input type="text">` readonly | Se rellena al seleccionar el dropdown |
| Usuario Esculapio | `<input type="text">` | Se digita manualmente |
| Empresas (multicheckbox) | `<input type="checkbox">` múltiple | Empresas activas del servidor |
| Nombre Empresa | `<input type="text">` readonly | Se rellena al seleccionar empresa |
| Botón Agregar/Registrar | `<button type="submit">` | Registra el radiólogo |
| Grid Médicos registrados | `<table>` | Lista de radiólogos guardados |

### ViewModel: `RadiologosViewModel`
```csharp
public class RadiologosViewModel
{
    // Formulario de registro
    public string CedulaMedico { get; set; } = string.Empty;
    public string NombreMedico { get; set; } = string.Empty;
    public string UsuarioEsculapio { get; set; } = string.Empty;
    public List<string> EmpresasSeleccionadas { get; set; } = new();

    // Datos para poblar controles
    public List<MedicoDto> MedicosDisponibles { get; set; } = new();
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();

    // Grid
    public List<RadiologoRegistradoDto> RadiologosRegistrados { get; set; } = new();
}
```

## Proceso

### Carga inicial (GET)
1. Obtener empresas activas → `IEsculapioApiClient.ObtenerEmpresasAsync()` con datos de sesión
2. Obtener lista de médicos radiólogos → endpoint de médicos (a definir en API)
3. Obtener grid de radiólogos registrados → endpoint de radiólogos registrados
4. Retornar `RadiologosViewModel` completo

### Registro (POST)
1. Validar `ModelState`
2. Llamar endpoint POST de la API para registrar el radiólogo con sus empresas
3. Si OK → redirigir a GET con mensaje de éxito (`TempData`)
4. Si FAIL → mostrar error en la vista

### Editar (GET /Parametros/Radiologos/Editar/{cedula})
1. Cargar datos del radiólogo desde la API
2. Retornar ViewModel con datos prellenados

### Eliminar (POST /Parametros/Radiologos/Eliminar/{cedula})
1. Confirmar con modal Bootstrap antes de ejecutar
2. Llamar endpoint DELETE de la API
3. Redirigir a GET con mensaje de resultado

## Seguridad
- `[Authorize(Roles = "Administrador")]` en el controller

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/ParametrosController.cs` (Radiologos actions)
- `src/WebImagenologia.Web/Models/ViewModels/RadiologosViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/MedicoDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/RadiologoRegistradoDto.cs`
- `src/WebImagenologia.Web/Views/Parametros/Radiologos.cshtml`
- `src/WebImagenologia.Web/Views/Parametros/RadiologosEditar.cshtml`
- `src/WebImagenologia.Tests/ParametrosRadiologosTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS
- `lint-cshtml`: ok (sin lógica de negocio en vista)
- `ui_routes`: GET /Parametros/Radiologos, POST /Parametros/Radiologos/Registrar
