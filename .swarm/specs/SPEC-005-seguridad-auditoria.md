# SPEC-005 — Seguridad y auditoría (F4)

- **Fase del plan:** F4 (PLAN-002 §5, §8)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Implementa / valida (después de aprobación):** CAPTAIN AMERICA + BLACK WIDOW (revisión de seguridad), BLACK PANTHER (persistencia BD).

---

## 1. Objetivo

Endurecer autenticación, autorización (rol **y** por caso), token, protección de imágenes y manejo de secretos; y persistir la **auditoría en BD desde el inicio** (dos capas: log estructurado + tabla/SP). Sin PHI en claro más allá de identificadores necesarios; ningún secreto en el repo.

## 2. Depende de

- **SPEC-002** (token, endpoints, `VisorAuditoriaService`), **SPEC-003** (Orthanc/proxy), **SPEC-004** (eventos de cliente auditables).

## 3. Alcance

### Dentro
- Autorización por **rol existente** + autorización por **caso** (nueva).
- Hardening de Orthanc (solo localhost) y del proxy IIS (solo lectura; bloquea STOW/DELETE/modify).
- Persistencia de auditoría en BD: `deploy/db/auditoria-visor.sql` (tabla + SP), sin PHI en claro.
- Manejo de secretos (User-Secrets en dev, variable de entorno en IIS/prod).
- Checklist de verificación para BLACK WIDOW.

### Fuera
- Deploy a producción (puerta aparte, QUICKSILVER con aprobación del Lead).

## 4. Diseño detallado

### 4.1 Autorización — dos niveles

- **Por rol (existente):** `[Authorize(Roles = RoleNames.Policies.AdministradorOrRadiologo)]` en todo `VisorController`. Se **reutiliza** el atributo/política existente; el visor no crea auth paralela.
- **Por caso (nueva):** antes de `POST /Visor/Token` emita un token, `EstudioResolver`/servicio de autorización valida contra la **API/BD clínica** que el radiólogo de la sesión (`ISessionService.ObtenerUsuario()`) **tiene acceso** al caso/estudio solicitado (p.ej. estudio asignado o de su ámbito). Si no → `403` y evento de auditoría `ACCESO_DENEGADO`. El `StudyInstanceUID` del token debe pertenecer al caso que el usuario está autorizado a ver.

### 4.2 Token corto (refuerzo de SPEC-002 §4.4)

- HMAC-SHA256, expiración ~10 min (`VisorOptions.TokenMinutos`), comparación en tiempo constante, `Nonce` anti-replay.
- El endpoint `Abrir/{token}` verifica que `payload.Usuario == sesión.Usuario` (impide reutilización cruzada del enlace).
- El token liga `Usuario + StudyInstanceUID`; un token para el estudio X no sirve para el estudio Y.

### 4.3 Hardening de Orthanc

- `RemoteAccessAllowed=false` (solo `localhost`), `AuthenticationEnabled=true`.
- Credenciales de Orthanc **fuera del repo** (User-Secrets/env); nunca en `orthanc.json` versionado (usar plantilla + inyección en despliegue).
- El navegador **nunca** alcanza Orthanc ni dcm4chee directamente; solo vía IIS mismo-origen.

### 4.4 Hardening del proxy IIS (solo lectura)

- El reverse proxy `/PortalImagenologia/dicomweb/` (SPEC-003) bloquea **STOW-RS** (`POST .../studies` de ingesta), **DELETE** y **modify** → responde `405`.
- Defensa en profundidad: regla URL Rewrite (SPEC-003 §4.4) + Lua de solo-lectura (SPEC-003 §4.5).
- Solo se permiten: QIDO (GET), WADO-RS (GET), metadata (GET) y las consultas POST de query internas necesarias.

### 4.5 Persistencia de auditoría (BD desde el inicio) — `deploy/db/auditoria-visor.sql`

Propuesta (nuevo archivo, revisable por BLACK PANTHER/DBA). **Sin PHI en claro** más allá de identificadores necesarios (StudyInstanceUID, cédula del radiólogo como actor — no del paciente):

```sql
-- deploy/db/auditoria-visor.sql  (PROPUESTA)
CREATE TABLE dbo.AuditoriaVisorImagenesDiag (
    Id                BIGINT IDENTITY(1,1) PRIMARY KEY,
    FechaUtc          DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    Usuario           NVARCHAR(100)  NOT NULL,   -- login del radiologo (actor)
    CedulaUsuario     NVARCHAR(30)   NULL,       -- cedula del radiologo (actor, NO paciente)
    StudyInstanceUID  NVARCHAR(128)  NOT NULL,   -- identificador tecnico del estudio
    NoCuentaCaso      NVARCHAR(50)   NULL,       -- caso/cuenta clinico (identificador, no PHI clinica)
    Accion            NVARCHAR(20)   NOT NULL,   -- ABRIR|DESCARGAR|IMPRIMIR|MEDICION|EVENTO|ACCESO_DENEGADO
    Detalle           NVARCHAR(400)  NULL,       -- texto corto, SIN nombres ni datos clinicos del paciente
    IpOrigen          NVARCHAR(45)   NULL
);
GO
CREATE PROCEDURE dbo.InsertAuditoriaVisorImagenesDiag
    @Usuario NVARCHAR(100), @CedulaUsuario NVARCHAR(30) = NULL,
    @StudyInstanceUID NVARCHAR(128), @NoCuentaCaso NVARCHAR(50) = NULL,
    @Accion NVARCHAR(20), @Detalle NVARCHAR(400) = NULL, @IpOrigen NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.AuditoriaVisorImagenesDiag
        (Usuario, CedulaUsuario, StudyInstanceUID, NoCuentaCaso, Accion, Detalle, IpOrigen)
    VALUES
        (@Usuario, @CedulaUsuario, @StudyInstanceUID, @NoCuentaCaso, @Accion, @Detalle, @IpOrigen);
END
GO
```
- `VisorAuditoriaService` (SPEC-002) hace **doble capa**: (a) `ILogger` estructurado inmediato (sin PHI) y (b) invoca `InsertAuditoriaVisorImagenesDiag` vía la API/servicio de datos existente.
- **Regla anti-PHI:** `Detalle` nunca contiene nombre del paciente, diagnóstico ni datos clínicos; solo identificadores técnicos y acción.

### 4.6 Manejo de secretos

- **Dev:** User-Secrets (`dotnet user-secrets set "Visor:TokenSecret" ...`, `Visor:OrthancUser`, `Visor:OrthancPassword`).
- **IIS/prod:** variables de entorno del proceso/AppPool (o configuración protegida del servidor), leídas por `IConfiguration`.
- `TokenSecret` ≥32 chars. **Nunca** en `appsettings.json` ni en `orthanc.json` versionados.

## 5. Contratos / Archivos afectados

Todos **nuevos**:
- `deploy/db/auditoria-visor.sql` (tabla + SP propuestos)
- `docs/visor/F4-seguridad.md` (checklist de seguridad + resultados, sin PHI/secretos)
- (lógica de autorización por caso vive en los servicios ya definidos en SPEC-002; aquí se especifica su comportamiento, no se crean archivos operativos nuevos fuera de `Services/Visor/`).

## 6. Criterios de aceptación verificables (checklist para BLACK WIDOW)

- [ ] **HTTPS forzado** (IIS termina TLS; `UseHttpsRedirection` activo); Orthanc solo HTTP localhost.
- [ ] **Roles aplicados:** un usuario sin rol Administrador/Radiologo recibe `403` en todos los endpoints.
- [ ] **Autorización por caso efectiva:** un radiólogo NO puede emitir token para un caso fuera de su ámbito (`403` + auditoría `ACCESO_DENEGADO`).
- [ ] **Token expira** (~10 min) y token con firma alterada es rechazado; token de estudio X no abre estudio Y; token de otro usuario no funciona en `Abrir`.
- [ ] **Orthanc inaccesible desde la red** (`RemoteAccessAllowed=false`; test de conexión externa a `:8042` falla).
- [ ] **Proxy solo lectura:** STOW/DELETE/PUT/PATCH sobre `/PortalImagenologia/dicomweb/` responden `405`.
- [ ] **Auditoría persistida** en `AuditoriaVisorImagenesDiag` para ABRIR/DESCARGAR/IMPRIMIR/EVENTO, **sin PHI en claro**.
- [ ] **Sin secretos en el repo:** grep de `TokenSecret`/`OrthancPassword`/`Password` en el árbol versionado = 0 (fuera de nombres de clave de config).
- [ ] `.har`/PHI no presentes en GitHub (`.gitignore` de `ActualizacionCodigo/` vigente).

## 7. Riesgos específicos

- **Fuga de PHI en logs/auditoría.** Mitigación: regla anti-PHI en `Detalle`, revisión de campos, BLACK WIDOW valida muestras.
- **Token replay.** Mitigación: `Nonce` + expiración corta + binding usuario↔estudio.
- **Acceso lateral a Orthanc** (bypass del proxy). Mitigación: `RemoteAccessAllowed=false` + firewall (F0).
- **Secreto en config versionada.** Mitigación: User-Secrets/env + criterio de grep en aceptación.
- **Autorización por caso mal modelada** (falsos negativos que bloqueen a radiólogos legítimos). Mitigación: definir la regla de ámbito con el Lead/negocio antes de implementar.
