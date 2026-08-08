# SPEC-002 — Broker de seguridad .NET 8 y APIs REST (F1)

- **Fase del plan:** F1 (PLAN-002 §5)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Implementa (después de aprobación):** CAPTAIN AMERICA (server-side) con revisión de WOLVERINE.

---

## 1. Objetivo

Definir el contrato detallado del **módulo Visor server-side** de la app .NET 8 que actúa como **broker de seguridad**: resuelve el estudio, autoriza por rol y por caso, emite un token corto firmado, audita y expone las 5 APIs REST del broker. Todo el módulo es **nuevo y aislado** bajo el namespace real `WebImagenologia.Web.*`; el único cambio en código operativo permitido es **una línea aditiva** en `Program.cs` (`AddVisorModule()`).

## 2. Depende de

- **SPEC-001 (parcial):** F1 puede desarrollarse en **dev/mock** sin esperar F0; pero el `Resolver` y el `DicomWebClient` contra Orthanc **real** requieren SPEC-001 verificada (decisión A). Los tests de F1 usan mocks/stubs de Orthanc.
- Reutiliza infraestructura existente: `ISessionService`, `RoleNames.Policies.AdministradorOrRadiologo`, `AddSession`/`UseSession` (ya presentes).

## 3. Alcance

### Dentro
- `VisorController` con 5 endpoints: `Resolver`, `Token`, `Abrir`, `Preview`, `Auditoria`.
- Servicios: `VisorTokenService`, `VisorAuditoriaService`, `DicomWebClient`, `OrthancGatewayService` (contratos/interfaces; el detalle DICOM/proxy real es de SPEC-003).
- DTOs (`Models/Visor/`) y `VisorOptions`.
- Registro DI aditivo `AddVisorModule()` + 1 línea en `Program.cs`.
- Sección `Visor` en `appsettings.json` **sin secretos**.

### Fuera
- Contenido real de `orthanc.json`, reverse proxy y C-FIND/C-MOVE físico → **SPEC-003**.
- UI, botón, MedDream → **SPEC-004** (ADR-002; OHIF descartado como motor diagnóstico).
- Script SQL de auditoría y hardening → **SPEC-005** (esta spec define solo el contrato de `VisorAuditoriaService`).

## 4. Diseño detallado

### 4.1 Estructura de carpetas (todas nuevas, bajo `src/WebImagenologia.Web/`)

```
src/WebImagenologia.Web/
├── Controllers/
│   └── VisorController.cs                 (NUEVO)
├── Services/Visor/                        (NUEVA carpeta)
│   ├── IVisorTokenService.cs
│   ├── VisorTokenService.cs
│   ├── IVisorAuditoriaService.cs
│   ├── VisorAuditoriaService.cs
│   ├── IDicomWebClient.cs
│   ├── DicomWebClient.cs                  (impl. real detallada en SPEC-003)
│   ├── IOrthancGatewayService.cs
│   ├── OrthancGatewayService.cs           (impl. real detallada en SPEC-003)
│   ├── IEstudioResolver.cs
│   ├── EstudioResolver.cs                 (capa de mapeo Caso/Cuenta<->UID)
│   └── VisorServiceCollectionExtensions.cs  (AddVisorModule)
└── Models/Visor/                          (NUEVA carpeta)
    ├── EstudioDicomDto.cs
    ├── TokenPayload.cs
    ├── TokenRequest.cs
    ├── TokenResponse.cs
    ├── ResolverResponse.cs
    ├── AuditoriaRequest.cs
    └── VisorOptions.cs
```

### 4.2 DTOs (forma exacta — `Models/Visor/`)

```csharp
// EstudioDicomDto — un estudio resuelto (proveniente de QIDO/C-FIND vía broker)
public sealed class EstudioDicomDto {
    public string StudyInstanceUID { get; init; } = "";
    public string? AccessionNumber { get; init; }
    public string? PatientId { get; init; }          // identificador, NO nombre en claro
    public string? Modality { get; init; }           // p.ej. "CT","MR","CR"
    public string? StudyDate { get; init; }          // yyyyMMdd (DICOM)
    public string? StudyDescription { get; init; }
    public int? NumberOfSeries { get; init; }
    public int? NumberOfInstances { get; init; }
}

// TokenRequest — cuerpo de POST /Visor/Token
public sealed class TokenRequest {
    public string StudyInstanceUID { get; init; } = "";
}

// TokenResponse — respuesta de POST /Visor/Token
public sealed class TokenResponse {
    public string Token { get; init; } = "";
    public DateTimeOffset Expira { get; init; }
    public string ViewerUrl { get; init; } = "";     // /PortalImagenologia/Visor/Abrir/{token}
}

// TokenPayload — contenido firmado del token (parte "payload" antes del HMAC)
public sealed class TokenPayload {
    public string Usuario { get; init; } = "";       // login del radiologo
    public string Cedula { get; init; } = "";        // cedula del radiologo (auditoria)
    public string StudyInstanceUID { get; init; } = "";
    public long IssuedAtUnix { get; init; }
    public long ExpiresAtUnix { get; init; }
    public string Nonce { get; init; } = "";         // anti-replay
}

// ResolverResponse — respuesta de GET /Visor/Resolver
public sealed class ResolverResponse {
    public string CriterioBusqueda { get; init; } = ""; // "caso" | "identificacion"
    public IReadOnlyList<EstudioDicomDto> Estudios { get; init; } = Array.Empty<EstudioDicomDto>();
}

// AuditoriaRequest — cuerpo de POST /Visor/Auditoria (eventos del cliente)
public sealed class AuditoriaRequest {
    public string StudyInstanceUID { get; init; } = "";
    public string Accion { get; init; } = "";        // "MEDICION"|"IMPRIMIR"|"DESCARGAR"|"EVENTO"...
    public string? Detalle { get; init; }
}

// VisorOptions — seccion "Visor" de appsettings (SIN secretos)
public sealed class VisorOptions {
    public string OrthancRestBaseUrl { get; init; } = "http://localhost:8042";
    public string OrthancDicomWebBaseUrl { get; init; } = "http://localhost:8042/PortalImagenologia/dicomweb";
    public string OrthancAet { get; init; } = "ESCULAPIO_ORTHANC";
    public int TokenMinutos { get; init; } = 10;
    public string ViewerBasePath { get; init; } = "/PortalImagenologia/visor";
    // TokenSecret, OrthancUser, OrthancPassword NO viven aqui -> User-Secrets/env
}
```

### 4.3 Contrato de los 5 endpoints (`VisorController`)

Base efectiva: IIS aplica PathBase `/PortalImagenologia`. Todos exigen sesión válida + `[Authorize(Roles = RoleNames.Policies.AdministradorOrRadiologo)]`. Usuario/rol/cédula se obtienen **exclusivamente** de `ISessionService.ObtenerUsuario()` — **prohibido** `HttpContext.Session.GetString(...)` directo (PLAN-002 decisión #2).

| # | Verbo · Ruta | Params | Éxito | Errores | Auditoría |
|---|---|---|---|---|---|
| 1 | `GET /Visor/Resolver` | query `caso` **o** `identificacion` (uno obligatorio) | `200` + `ResolverResponse` | `400` falta criterio / ambos; `401` sin sesión; `403` rol inválido; `404` sin estudios; `502` Orthanc no responde | — (solo lectura de búsqueda) |
| 2 | `POST /Visor/Token` | body `TokenRequest` | `200` + `TokenResponse` | `400` UID vacío; `401`/`403`; `403` autorización por caso falla (SPEC-005); `502` | evento `ABRIR` |
| 3 | `GET /Visor/Abrir/{token}` | route `token` | `200` vista `Abrir.cshtml` (embebe OHIF) | token inválido/expirado → `200` vista `TokenInvalido.cshtml` (no 4xx para UX); `401` sin sesión | — |
| 4 | `GET /Visor/Preview` | query `studyUid`,`seriesUid`,`instanceUid`,`frame?`,`formato?`(jpg/png) | `200` + `image/jpeg`\|`image/png` (WADO-RS rendered) | `400` params; `401`/`403`; `404`; `502` | evento `DESCARGAR` |
| 5 | `POST /Visor/Auditoria` | body `AuditoriaRequest` | `204` | `400` acción inválida; `401`/`403` | evento según `Accion` |

Notas de contrato:
- El endpoint 3 valida el token vía `VisorTokenService.Validar(token)` y verifica que el `Usuario` del payload coincide con el de la sesión actual (anti-uso cruzado).
- `/PortalImagenologia/dicomweb/*` **no es un endpoint .NET**: es el reverse proxy a Orthanc (SPEC-003).

### 4.4 `VisorTokenService` (HMAC-SHA256, stateless)

- **Formato del token:** `base64url(payloadJson) ~ base64url(hmacSha256(payloadJson, TokenSecret))`. Separador `~`. (Ratifica el formato del andamiaje.)
- **Firma:** HMAC-SHA256 sobre el JSON del `TokenPayload`. `TokenSecret` ≥32 chars, obtenido de `IConfiguration["Visor:TokenSecret"]` respaldado por **User-Secrets (dev)** / **variable de entorno (IIS/prod)** — nunca `appsettings.json`.
- **Validación:** decodifica payload, recomputa HMAC y compara en **tiempo constante** (`CryptographicOperations.FixedTimeEquals`); rechaza si `now > ExpiresAtUnix`; expiración configurable vía `VisorOptions.TokenMinutos` (default 10).
- **API interna:**
  ```csharp
  string Emitir(TokenPayload payload);
  bool TryValidar(string token, out TokenPayload payload); // false si firma/expiración inválida
  ```

### 4.5 `EstudioResolver` — capa de mapeo Caso/Cuenta ↔ AccessionNumber/PatientID/StudyInstanceUID

- **No asume 1:1** (PLAN-002 decisión #4). Estrategia con fallback:
  1. Resuelve `Caso/Cuenta` → `(AccessionNumber | PatientID)` contra la **BD/API clínica existente** (que ya conoce `NoCuenta`↔paciente).
  2. Intenta QIDO por `AccessionNumber`; si el PACS no indexa por accession o no hay match → **fallback** a QIDO por `PatientID` (Identificación) + filtros (fecha/modalidad).
  3. Si el criterio de entrada es `identificacion`, va directo a QIDO por `PatientID`.
- El mapeo definitivo se **fija con el resultado de SPEC-001/SPEC-003**; en F1 el resolver se codifica con ambas rutas y una interfaz `IEstudioResolver` que permite inyectar el cliente real o un mock.

### 4.6 Registro DI aditivo (`AddVisorModule`) y única línea en `Program.cs`

`VisorServiceCollectionExtensions.cs` (nuevo):
```csharp
public static class VisorServiceCollectionExtensions {
    public static IServiceCollection AddVisorModule(this IServiceCollection services, IConfiguration config) {
        services.Configure<VisorOptions>(config.GetSection("Visor"));
        services.AddScoped<IVisorTokenService, VisorTokenService>();
        services.AddScoped<IVisorAuditoriaService, VisorAuditoriaService>();
        services.AddScoped<IEstudioResolver, EstudioResolver>();
        services.AddHttpClient<IDicomWebClient, DicomWebClient>();      // HttpClient tipado -> Orthanc
        services.AddHttpClient<IOrthancGatewayService, OrthancGatewayService>();
        return services;
    }
}
```
En `Program.cs`, **una sola línea aditiva** (junto al resto de `builder.Services.Add...`, sin reescribir el archivo):
```csharp
builder.Services.AddVisorModule(builder.Configuration);
```

### 4.7 Configuración `appsettings.json` (sección `Visor`, sin secretos)

```json
"Visor": {
  "OrthancRestBaseUrl": "http://localhost:8042",
  "OrthancDicomWebBaseUrl": "http://localhost:8042/PortalImagenologia/dicomweb",
  "OrthancAet": "ESCULAPIO_ORTHANC",
  "TokenMinutos": 10,
  "ViewerBasePath": "/PortalImagenologia/visor"
}
```
`TokenSecret`, `OrthancUser`, `OrthancPassword` → **User-Secrets/env** (SPEC-005).

## 5. Contratos / Archivos afectados

- **Nuevos:** todos los de §4.1 + la sección `Visor` en `appsettings.json` (aditiva, sin secretos).
- **Modificado (único cambio aditivo aprobado):** `src/WebImagenologia.Web/Program.cs` — 1 línea `AddVisorModule(...)`.
- **No se toca** ningún otro archivo operativo.

## 6. Criterios de aceptación verificables (checklist)

- [ ] `dotnet build` de `WebImagenologia.Web` compila sin errores ni warnings nuevos.
- [ ] Tests unitarios de `VisorTokenService`: emite token; valida token correcto; **rechaza** token con firma alterada; **rechaza** token expirado; comparación en tiempo constante.
- [ ] Test unitario de `EstudioResolver` con `IDicomWebClient` mock: ruta accession OK, fallback a PatientID cuando accession no matchea.
- [ ] Smoke test de cada endpoint (con mocks si Orthanc real no está listo): `Resolver` (200/400/404), `Token` (200 + `ABRIR` auditado), `Abrir` (vista OK / `TokenInvalido` con token roto), `Preview` (image/jpeg), `Auditoria` (204).
- [ ] `git diff` confirma que **el único archivo operativo modificado es `Program.cs` con exactamente 1 línea añadida**.
- [ ] Ningún secreto en `appsettings.json` ni en el repo (grep de `TokenSecret`/`Password`).
- [ ] `ISessionService` usado en todos los endpoints; **cero** ocurrencias de `HttpContext.Session.GetString`.

## 7. Riesgos específicos

- **Acoplamiento a Orthanc real** en `Resolver`/`Preview`: mitigado con interfaces + mocks para F1; validación real diferida a SPEC-003 tras F0.
- **Mapeo caso↔accession** no 1:1: mitigado con fallback y con el resultado empírico de F0.
- **Fuga del `TokenSecret`** si alguien lo pone en `appsettings.json`: mitigado por criterio de aceptación (grep) y revisión de WOLVERINE/BLACK WIDOW.
- **Uso accidental de `HttpContext.Session` directo** (patrón del andamiaje): mitigado por criterio de aceptación explícito.
