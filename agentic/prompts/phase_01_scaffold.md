# FASE 01 — Scaffold de la Solución .NET 8

## Objetivo
Crear la solución ASP.NET Core MVC 8 completa con la estructura base, configuración
de Bootstrap 5, inyección de dependencias, y el cliente API tipado vacío.

## Tareas a completar

### 1. Crear solución y proyectos
```powershell
dotnet new sln -n WebImagenologia -o src/
dotnet new mvc -n WebImagenologia.Web -o src/WebImagenologia.Web --no-https false
dotnet new xunit -n WebImagenologia.Tests -o src/WebImagenologia.Tests
dotnet sln src/WebImagenologia.sln add src/WebImagenologia.Web/WebImagenologia.Web.csproj
dotnet sln src/WebImagenologia.sln add src/WebImagenologia.Tests/WebImagenologia.Tests.csproj
```

### 2. Estructura de carpetas en WebImagenologia.Web
```
Controllers/
Models/
  ApiDtos/       ← records que mapean respuestas de la API Esculapio
  ViewModels/    ← clases mutables para las Razor Views
  Domain/        ← enums, constantes de dominio
Services/
  IEsculapioApiClient.cs
  EsculapioApiClient.cs
  ISessionService.cs
  SessionService.cs
Views/
  Shared/
    _Layout.cshtml          ← Bootstrap 5 CDN, navbar con menú de roles
    _ValidationScriptsPartial.cshtml
  Account/
    Login.cshtml            ← placeholder (Fase 02)
  Parametros/               ← placeholder (Fases 03-05)
  Condicional/              ← placeholder (Fases 06-07)
  PortalRadiologos/         ← placeholder (Fase 08)
  Lecturas/                 ← placeholder (Fase 09)
  Reportes/                 ← placeholder (Fase 10)
  Home/
    Index.cshtml            ← redirige a Login si no hay sesión
wwwroot/
  css/site.css
  js/
    account.js
    parametros.js
    portalRadiologos.js
    lecturas.js
    reportes.js
  lib/bootstrap/           ← Bootstrap 5 local (fallback, CDN es primario)
```

### 3. Program.cs — configuración base
```csharp
// Registrar:
// - HttpClient tipado: AddHttpClient<IEsculapioApiClient, EsculapioApiClient>
// - Session: AddSession con timeout 30 min
// - DataProtection: AddDataProtection
// - Autenticación: AddAuthentication + AddCookie
// - Autorización: AddAuthorization con políticas por rol
// - ISessionService: AddScoped
// - Logging: AddLogging con ILogger<T>
// MVC con vistas: AddControllersWithViews
```

### 4. appsettings.json
```json
{
  "EsculapioApi": {
    "BaseUrl": "https://appsintranet.esculapiosis.com/ApiCampbell/api",
    "TimeoutSeconds": 30
  },
  "Session": {
    "TimeoutMinutes": 30
  }
}
```

### 5. _Layout.cshtml — Bootstrap 5
- Navbar con logo "Imagenología Esculapio"
- Menú de navegación con links condicionales por rol (Parámetros solo Admin,
  Portal Radiólogos solo Radiólogo, etc.)
- Footer con versión y empresa
- Bootstrap 5 CDN + site.css

### 6. IEsculapioApiClient — stub vacío
Declarar la interfaz con los 5 métodos del spec (retornando `Task<T>`) pero
con implementación que arroja `NotImplementedException` — se completará en Fase 02.

### 7. Tests mínimos de la Fase 01
- Test que verifica que la aplicación arranca (`WebApplicationFactory`)
- Test que verifica que `GET /` redirige a `/Account/Login` si no hay sesión

## Archivos a generar (artefactos esperados)
- `src/WebImagenologia.sln`
- `src/WebImagenologia.Web/WebImagenologia.Web.csproj`
- `src/WebImagenologia.Web/Program.cs`
- `src/WebImagenologia.Web/appsettings.json`
- `src/WebImagenologia.Web/Controllers/HomeController.cs`
- `src/WebImagenologia.Web/Services/IEsculapioApiClient.cs`
- `src/WebImagenologia.Web/Services/EsculapioApiClient.cs`
- `src/WebImagenologia.Web/Services/ISessionService.cs`
- `src/WebImagenologia.Web/Services/SessionService.cs`
- `src/WebImagenologia.Web/Views/Shared/_Layout.cshtml`
- `src/WebImagenologia.Web/Views/Home/Index.cshtml`
- `src/WebImagenologia.Web/wwwroot/css/site.css`
- `src/WebImagenologia.Tests/WebImagenologia.Tests.csproj`
- `src/WebImagenologia.Tests/StartupTests.cs`

## Gates de esta fase
- `build`: ok
- `tests`: todos PASS (mínimo 2 tests)
- `secrets`: ok
- `api-client-unicity`: ok
