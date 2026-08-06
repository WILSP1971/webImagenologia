# FASE 02 — Login de Acceso + ApiClient

## Objetivo
Implementar el flujo completo de Login: carga de servidores desde la API,
validación de credenciales, sesión cifrada con DataProtection, y los 5
métodos base del `EsculapioApiClient`.

## Vista Login (Account/Login.cshtml)

### Campos UI
| Campo | Tipo | Descripción |
|-------|------|-------------|
| Usuario | `<input type="text">` | Usuario del sistema Esculapio |
| Contraseña | `<input type="password">` | Contraseña |
| Servidor | `<select>` | DropdownList con servidores disponibles (API) |
| Botón Acceder | `<button type="submit">` | Dispara el login |

### ViewModel: `LoginViewModel`
```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es requerido")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un servidor")]
    public string ServidorSeleccionado { get; set; } = string.Empty;  // ConnectionString cifrado

    public List<ServidorDto> Servidores { get; set; } = new();
}
```

### DTO: `ServidorDto`
```csharp
public record ServidorDto(
    string Descripcion,   // texto visible en el dropdown
    string IpConexion,
    string BdConexion,
    string PortConexion
);
```

## Proceso del Login

1. `GET /Account/Login` → `AccountController.Login()`:
   - Llama `IEsculapioApiClient.ObtenerServidoresAsync()`
   - Retorna `LoginViewModel` con la lista de servidores

2. `POST /Account/Login` → `AccountController.LoginPost()`:
   - Valida `ModelState`
   - Extrae `IpConexion`, `BdConexion`, `PortConexion` del servidor seleccionado
   - Llama `IEsculapioApiClient.ValidarConexionAsync(ip, usuario, password)`
   - Si OK → crea claims principal con Rol del usuario retornado por la API
   - Cifra el ConnectionString con `IDataProtector` y lo guarda en Session
   - `HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal)`
   - Redirige a `/Home/Index`
   - Si FAIL → `ModelState.AddModelError` + retorna vista con mensaje de error

## IEsculapioApiClient — implementación de los 5 métodos base

```csharp
Task<IEnumerable<ServidorDto>> ObtenerServidoresAsync(CancellationToken ct);
Task<IEnumerable<EmpresaDto>> ObtenerEmpresasAsync(string ip, string bd, string port, string usuario, string pwd, CancellationToken ct);
Task<UsuarioConexionDto> ValidarConexionAsync(string ip, string usuario, string pwd, CancellationToken ct);
Task<DiagnosticoDto> ObtenerDiagnosticoCuentaAsync(string empresa, decimal noCuenta, CancellationToken ct);
Task<IEnumerable<NotaMedicaDto>> ObtenerNotasMedicasCuentaAsync(string empresa, decimal noCuenta, CancellationToken ct);
```

Implementación real (usando `HttpClient` + `System.Text.Json`).

## SessionService
Métodos:
- `void GuardarConnectionString(string connStringCifrado)`
- `string? ObtenerConnectionString()`
- `void GuardarUsuario(UsuarioConexionDto usuario)`
- `UsuarioConexionDto? ObtenerUsuario()`
- `void LimpiarSesion()`

## Logout
`GET /Account/Logout` → limpia sesión + `HttpContext.SignOutAsync()` + redirige a Login.

## Tests de la Fase 02
- Login con servidor mock retorna sesión válida
- Login con credenciales incorrectas muestra error
- Logout elimina la sesión
- `ObtenerServidoresAsync` deserializa correctamente el JSON mock

## Archivos a generar
- `src/WebImagenologia.Web/Controllers/AccountController.cs`
- `src/WebImagenologia.Web/Models/ViewModels/LoginViewModel.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/ServidorDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/EmpresaDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/UsuarioConexionDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/DiagnosticoDto.cs`
- `src/WebImagenologia.Web/Models/ApiDtos/NotaMedicaDto.cs`
- `src/WebImagenologia.Web/Services/EsculapioApiClient.cs` (implementación completa)
- `src/WebImagenologia.Web/Services/SessionService.cs`
- `src/WebImagenologia.Web/Views/Account/Login.cshtml`
- `src/WebImagenologia.Tests/LoginTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: todos PASS
- `secrets`: ok (el ConnectionString se cifra, nunca en claro)
- `endpoints_cubiertos`: ObtenerServidoresAsync, ValidarConexionAsync
