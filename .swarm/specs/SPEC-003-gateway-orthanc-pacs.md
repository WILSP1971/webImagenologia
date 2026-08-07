# SPEC-003 — Gateway Orthanc DICOMweb + reverse proxy IIS (F2)

- **Fase del plan:** F2 (PLAN-002 §5)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Implementa (después de aprobación):** CAPTAIN AMERICA + BLACK PANTHER (backend/gateway), QUICKSILVER (config Orthanc/IIS).

---

## 1. Objetivo

Dejar operativa la **cadena de datos extremo a extremo**: `C-FIND` (búsqueda en dcm4chee) → `C-MOVE` bajo demanda (estudio → Orthanc, cache) → re-exposición **DICOMweb** (QIDO-RS/WADO-RS) que consume OHIF, todo servido **mismo-origen** bajo `/PortalImagenologia/dicomweb/` vía reverse proxy IIS. Implementa `DicomWebClient` y `OrthancGatewayService` (contratos definidos en SPEC-002) y aporta la configuración real de Orthanc y del proxy, **con datos reales confirmados por HAR** (`docs/PACS-exposicion.md`).

## 2. Depende de

- **SPEC-001 (dura):** requiere decisión **A** (Orthanc/OHIF). Si F0 forzó Plan B, esta spec se rediseña sobre WADO-URI JPEG de dcm4chee.
- **SPEC-002:** provee las interfaces `IDicomWebClient` / `IOrthancGatewayService` y `VisorOptions`.

## 3. Alcance

### Dentro
- Implementación real de `DicomWebClient` (QIDO-RS/WADO-RS contra Orthanc) y `OrthancGatewayService` (C-FIND / C-MOVE **idempotente** contra dcm4chee).
- `orthanc.json` real (sin placeholder `PACS_SERVER`).
- Fragmento de reverse proxy IIS (ARR + URL Rewrite) para `/PortalImagenologia/dicomweb`.
- Script Lua opcional de solo-lectura.
- Validación empírica de la capa de mapeo Caso/Cuenta ↔ AccessionNumber/PatientID/StudyInstanceUID.

### Fuera
- Hardening completo/seguridad de red → **SPEC-005** (esta spec fija `RemoteAccessAllowed=false` y proxy read-only como base).
- UI/OHIF → **SPEC-004**.

## 4. Diseño detallado

### 4.1 `DicomWebClient` (QIDO-RS / WADO-RS contra Orthanc)

`HttpClient` tipado (base = `VisorOptions.OrthancDicomWebBaseUrl`), autenticación Basic con `OrthancUser`/`OrthancPassword` (desde User-Secrets/env, SPEC-005).

```csharp
public interface IDicomWebClient {
    // QIDO-RS: GET {base}/studies?<query>
    Task<IReadOnlyList<EstudioDicomDto>> QueryStudiesAsync(
        string? accessionNumber, string? patientId,
        string? studyDate = null, string? modality = null, CancellationToken ct = default);

    // WADO-RS rendered: GET {base}/studies/{s}/series/{se}/instances/{i}/rendered
    Task<Stream> GetRenderedInstanceAsync(
        string studyUid, string seriesUid, string instanceUid,
        int? frame, string formato /*jpg|png*/, CancellationToken ct = default);
}
```
- El mapeo del JSON DICOM (tags `0020000D` StudyInstanceUID, `00080050` AccessionNumber, `00100020` PatientID, `00080060` Modality, `00080020` StudyDate, `00081030` StudyDescription, `00201206`/`00201208` counts) a `EstudioDicomDto` se hace aquí.

### 4.2 `OrthancGatewayService` (C-FIND / C-MOVE idempotente)

Usa la **API REST de Orthanc** (`OrthancRestBaseUrl`) para orquestar DIMSE contra dcm4chee sin hablar DIMSE desde .NET.

```csharp
public interface IOrthancGatewayService {
    // C-FIND remoto contra la modality "pacs" (dcm4chee)
    Task<IReadOnlyList<EstudioDicomDto>> FindStudiesAsync(
        string? accessionNumber, string? patientId, CancellationToken ct = default);

    // C-MOVE bajo demanda: trae el estudio del PACS a Orthanc (idempotente).
    // Si el estudio YA esta en Orthanc (QIDO local lo encuentra) -> NO dispara C-MOVE.
    Task EnsureStudyPresentAsync(string studyInstanceUID, CancellationToken ct = default);
}
```
- **C-FIND:** `POST {rest}/modalities/pacs/query` con `{ "Level":"Study", "Query":{ "AccessionNumber":"...", "PatientID":"..." } }`, luego `GET /queries/{id}/answers?expand`.
- **C-MOVE idempotente:** `EnsureStudyPresentAsync` primero hace `GET {dicomweb}/studies?StudyInstanceUID=...`; si ya está cacheado, retorna sin mover; si no, `POST {rest}/modalities/pacs/move` con `TargetAet=ESCULAPIO_ORTHANC`. Esto evita C-MOVE duplicados (idempotencia).
- **Feedback asíncrono** para estudios grandes (TC/RM): el `move` puede lanzarse con seguimiento de job (`GET /jobs/{id}`); la UI muestra progreso (SPEC-004). THOR revisa performance.

### 4.3 `orthanc.json` real (SIN placeholder `PACS_SERVER`)

`deploy/orthanc/orthanc.json` (nuevo). Valores reales de `docs/PACS-exposicion.md`:

```jsonc
{
  "Name": "ESCULAPIO_ORTHANC",
  "DicomAet": "ESCULAPIO_ORTHANC",
  "DicomPort": 4242,
  "HttpPort": 8042,

  // Solo localhost; IIS termina TLS y hace de proxy (SPEC-005).
  "RemoteAccessAllowed": false,
  "AuthenticationEnabled": true,
  // Usuarios/credenciales NO en el repo -> inyectar por entorno / archivo fuera de git.

  "DicomModalities": {
    // AET REAL = DCM4CHEE (NO "PACS_SERVER"). Dos backends dcm4chee.
    "pacs":            { "AET": "DCM4CHEE", "Host": "172.16.10.100", "Port": 11112 },
    "pacs_santamarta": { "AET": "DCM4CHEE", "Host": "172.16.50.100", "Port": 11112 }
  },

  "DicomWeb": {
    "Enable": true,
    // BulkDataURI debe coincidir con el sub-path publico del proxy (PLAN-002 decision #6).
    "Root": "/PortalImagenologia/dicomweb/",
    "EnableWado": true,
    "WadoRoot": "/PortalImagenologia/wado"
  },

  // Cache fetch-on-demand con purga (retencion validada en F0/V4).
  "MaximumStorageSize": 0,          // ajustar segun disco confirmado en F0
  "StorageCompression": true
}
```
> `pacs_santamarta` es una **decisión de diseño no trivial** derivada del plan: el HAR confirma dos backends dcm4chee (`172.16.10.100` CAMPBELL/FUNDACION y `172.16.50.100` SANTAMARTA) con el **mismo AET `DCM4CHEE`**. Se registran ambas modalities; el `EstudioResolver` elige el backend según el origen del caso (o intenta ambos). No contradice el plan (§6 nombra ambos hosts con AET `DCM4CHEE`).

### 4.4 Reverse proxy IIS (ARR + URL Rewrite) — fragmento a **fusionar**

`deploy/iis/web.dicomweb.config` (nuevo). Es un **fragmento documentado a fusionar** en el `web.config` operativo (NO se sobrescribe a mano; la Guía de SPEC-006 explica la fusión). Proxy de **solo lectura**, mismo-origen (evita CORS).

```xml
<!-- FRAGMENTO a fusionar en <system.webServer><rewrite><rules> del web.config operativo. NO sobrescribir el archivo. -->
<rule name="Visor-DICOMweb-Proxy" stopProcessing="true">
  <match url="^PortalImagenologia/dicomweb/(.*)" />
  <!-- Bloquear metodos de escritura: STOW/DELETE/modify -> solo GET/POST-de-consulta -->
  <conditions logicalGrouping="MatchAny">
    <add input="{REQUEST_METHOD}" pattern="^(DELETE|PUT|PATCH)$" />
  </conditions>
  <action type="CustomResponse" statusCode="405" statusReason="Method Not Allowed" />
</rule>
<rule name="Visor-DICOMweb-Forward" stopProcessing="true">
  <match url="^PortalImagenologia/dicomweb/(.*)" />
  <action type="Rewrite" url="http://localhost:8042/PortalImagenologia/dicomweb/{R:1}" />
</rule>
<rule name="Visor-WADO-Forward" stopProcessing="true">
  <match url="^PortalImagenologia/wado/(.*)" />
  <action type="Rewrite" url="http://localhost:8042/PortalImagenologia/wado/{R:1}" />
</rule>
```
> Requiere **ARR (Application Request Routing)** con proxy habilitado a nivel de servidor. La regla de bloqueo de escritura vive tanto aquí (defensa en profundidad) como en el Lua de §4.5. STOW (`POST .../studies` con multipart) se restringe adicionalmente en SPEC-005.

### 4.5 Script Lua de solo-lectura opcional

`deploy/orthanc/orthanc-readonly.lua` (nuevo): rechaza `IncomingHttpRequestFilter` para métodos/paths de escritura DICOMweb (STOW-RS, DELETE de recursos) cuando lleguen por el proxy. Defensa en profundidad complementaria al proxy.

### 4.6 Validación de la capa de mapeo Caso/Cuenta ↔ identificadores

Ejecutar contra un caso real de test (post-F0):
- `GET /PortalImagenologia/dicomweb/studies?AccessionNumber=<caso-test>` → ¿devuelve StudyInstanceUID?
- Si no → `GET .../studies?PatientID=<identificacion-test>` → fallback.
- Registrar en `docs/visor/F2-cadena-dicomweb.md` cuál llave funciona (fija el mapeo definitivo, decisión #4).

## 5. Contratos / Archivos afectados

Todos **nuevos** (ninguno operativo):
- `src/WebImagenologia.Web/Services/Visor/DicomWebClient.cs` (impl. de la interfaz de SPEC-002).
- `src/WebImagenologia.Web/Services/Visor/OrthancGatewayService.cs` (impl.).
- `deploy/orthanc/orthanc.json`
- `deploy/orthanc/orthanc-readonly.lua`
- `deploy/iis/web.dicomweb.config` (fragmento a fusionar, **no** reemplaza el web.config operativo)
- `docs/visor/F2-cadena-dicomweb.md` (evidencia, sin PHI)

## 6. Criterios de aceptación verificables (checklist)

- [ ] `orthanc.json` **sin** ninguna aparición de `PACS_SERVER`; AET = `DCM4CHEE`; hosts `172.16.10.100` y `172.16.50.100`; puerto `11112`; `DicomWeb.Root = /PortalImagenologia/dicomweb/`; `RemoteAccessAllowed=false`.
- [ ] `GET /PortalImagenologia/dicomweb/studies?AccessionNumber=<caso-real-test>` devuelve el `StudyInstanceUID` esperado.
- [ ] Un `C-MOVE` bajo demanda trae el estudio de test a Orthanc y `EnsureStudyPresentAsync` es **idempotente** (segunda llamada no dispara otro C-MOVE — verificable por conteo de jobs).
- [ ] `WADO-RS rendered` sirve una imagen (jpg/png) de una instancia de test vía `GET /Visor/Preview`.
- [ ] El proxy IIS responde mismo-origen (sin cabeceras CORS necesarias) y **bloquea** DELETE/PUT/PATCH (405) y STOW.
- [ ] `docs/visor/F2-cadena-dicomweb.md` documenta qué llave (accession/patientId) resolvió el caso, sin PHI.
- [ ] Ningún archivo operativo modificado; el web.config operativo se **fusiona** por instrucción de la Guía, no se sobrescribe.

## 7. Riesgos específicos

- **BulkDataURI ≠ sub-path del proxy** → OHIF carga metadatos pero no imágenes. Mitigación: `DicomWeb.Root=/PortalImagenologia/dicomweb/` + validación WADO real.
- **C-STORE de vuelta bloqueado** (heredado de F0/V2c) → C-MOVE no completa. Mitigación: dependencia dura de SPEC-001.
- **Dos backends con mismo AET** puede confundir la selección de origen. Mitigación: dos modalities (`pacs`, `pacs_santamarta`) + lógica de origen en `EstudioResolver`.
- **Sobrescribir el web.config operativo** por error. Mitigación: entregar solo un fragmento + instrucción de fusión (nunca reemplazo).
- **C-MOVE duplicados** en estudios grandes. Mitigación: idempotencia por QIDO local previo.
