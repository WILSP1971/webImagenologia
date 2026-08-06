# FASE 08 — Portal Web Radiólogos

## Objetivo
Implementar el portal exclusivo para médicos/radiólogos. Incluye grid de estudios
programados, panel de diagnóstico, notas médicas y gestión de audio.

## Vista: PortalRadiologos/Index.cshtml

### Estructura de la página
```
┌─────────────────────────────────────────────────┐
│  [Empresa: dropdown] [Nombre Empresa: text readonly]  │
│  [Fecha - Hora: text/datetime readonly]              │
├─────────────────────────────────────────────────┤
│  GRID — Programación de Lecturas                    │
│  (pendientes de hoy + días anteriores sin leer)     │
│  Cols: NoCuenta | Paciente | Servicio | Fecha |     │
│        Dependencia | Estado | [Seleccionar]         │
├─────────────────────────────────────────────────┤
│  TABS                                               │
│  [Diagnósticos] [Notas Médicas]                     │
├─────────────────────────────────────────────────┤
│  PANEL — Información del Caso                       │
│  NoCuenta, NoOrden, Servicio, Dependencia, Médico   │
│  [Upload Audio] [Play] [Eliminar Audio]             │
│  Formatos: mp3, wav, ogg, m4a (max 25 MB)          │
│  [Botón Grabar]                                     │
└─────────────────────────────────────────────────┘
```

### ViewModel: `PortalRadiologosViewModel`
```csharp
public class PortalRadiologosViewModel
{
    // Filtros
    public string EmpresaSeleccionada { get; set; } = string.Empty;
    public string NombreEmpresa { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.Now;

    // Grid programación
    public List<EstudioProgramadoDto> EstudiosProgramados { get; set; } = new();

    // Estudio seleccionado (panel de información)
    public EstudioProgramadoDto? EstudioSeleccionado { get; set; }

    // Paneles de tabs
    public List<DiagnosticoDto> Diagnosticos { get; set; } = new();
    public List<NotaMedicaDto> NotasMedicas { get; set; } = new();

    // Empresas para dropdown
    public List<EmpresaDto> EmpresasDisponibles { get; set; } = new();

    // Audio
    public bool TieneAudio { get; set; }
}
```

### DTOs adicionales
```csharp
public record EstudioProgramadoDto(
    string Empresa,
    long Consecutivo,
    decimal NoCuenta,
    string CedulaMedico,
    DateOnly FechaProgramacion,
    string Servicio,
    string CodServicio,
    string Dependencia,
    decimal NoOrden,
    string UsuarioOperador,
    DateOnly FechaAsignacion,
    string Estado
);
```

## Proceso

### Carga inicial (GET /PortalRadiologos)
1. Obtener empresas asignadas al usuario logueado (sesión)
2. Si tiene empresa seleccionada → cargar grid de estudios programados
   - Source: tabla `estudiosdiagnosticos_programacion` filtrada por:
     - `Empresa` = empresa seleccionada
     - `CedulaMedico` = cédula del médico en sesión
     - `FechaProgramacion` <= hoy (incluye pendientes de días anteriores)
     - `Estado` != 'LEIDO'
3. Retornar ViewModel

### Seleccionar estudio del grid (AJAX)
```
GET /PortalRadiologos/DetalleEstudio?consecutivo=XX&empresa=XX
→ retorna JSON con:
  - Datos del estudio (EstudioProgramadoDto)
  - Diagnósticos (IEsculapioApiClient.ObtenerDiagnosticoCuentaAsync)
  - Notas médicas (IEsculapioApiClient.ObtenerNotasMedicasCuentaAsync)
  - hasAudio: bool
```

### Cambiar empresa (AJAX)
```
GET /PortalRadiologos/EstudiosPorEmpresa?empresa=XX
→ retorna JSON con la lista de EstudioProgramadoDto
```

### Upload de audio
```
POST /PortalRadiologos/SubirAudio
Content-Type: multipart/form-data
Body: { archivo: File, consecutivo: long, empresa: string }
```
- Validar: content-type en [audio/mpeg, audio/wav, audio/ogg, audio/mp4, audio/x-m4a]
- Validar: tamaño <= 25 MB
- Llamar API externa para guardar el BLOB en `estudiosdiagnosticos_programacion.AudioRadiologo`
- **Si el endpoint de upload no existe en la API → emitir BLOCKED y notificar al Lead**

### Play de audio
```
GET /PortalRadiologos/ObtenerAudio?consecutivo=XX&empresa=XX
→ retorna FileResult con el audio (streaming)
```

### Eliminar audio
```
POST /PortalRadiologos/EliminarAudio
Body: { consecutivo: long, empresa: string }
```

### Grabar (botón Grabar)
- Abre modal con MediaRecorder API (vanilla JS)
- Al terminar, el blob de audio se sube via fetch al endpoint SubirAudio

## Seguridad
- `[Authorize(Roles = "Radiologo")]`

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/PortalRadiologosController.cs`
- `src/WebImagenologia.Web/Models/ViewModels/PortalRadiologosViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/EstudioProgramadoDto.cs`
- `src/WebImagenologia.Web/Views/PortalRadiologos/Index.cshtml`
- `src/WebImagenologia.Web/wwwroot/js/portalRadiologos.js` (AJAX + MediaRecorder)
- `src/WebImagenologia.Tests/PortalRadiologosTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: PASS (incluye test de validación de mime de audio)
- `ui_routes`:
  - GET /PortalRadiologos
  - GET /PortalRadiologos/DetalleEstudio
  - POST /PortalRadiologos/SubirAudio
  - GET /PortalRadiologos/ObtenerAudio
  - POST /PortalRadiologos/EliminarAudio
