# PLAN-002 — Visor DICOM diagnóstico integrado en Portal Web Radiólogos

> **ENMIENDA 2026-08-08 (ADR-002):** el visor diagnóstico es **MedDream** (certificado), no OHIF.
> Orthanc como gateway DICOMweb hacia dcm4chee **se mantiene**. Stone = solo prueba de humo.
> SPEC-004 y este plan se leen bajo ADR-002; donde diga OHIF como motor clínico, sustituir por MedDream.

- **Autor:** DOCTOR STRANGE (Lead de Arquitectura y Specs — Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO PLAN-002"; **enmendado** 2026-08-08 (MedDream)
- **Insumos autoritativos:** `prompt-lab/prompts/PROMPT-visor-diagnostico-RESET.md`, `docs/PROMPT-visor-diagnostico.md`, `docs/PACS-exposicion.md` (datos HAR reales; prioridad sobre placeholders).
- **Referencia local (NO código correcto, NO se sube a GitHub):** `ActualizacionCodigo/`.

> **RESET.** Este plan es autocontenido y **reemplaza por completo** cualquier plan o spec anterior sobre el visor (PLAN-001 y specs previas quedan ARCHIVADOS). No se debe "continuar desde PLAN-001".

---

## 1. Resumen ejecutivo

Se diseñará e implementará un **visor de imágenes DICOM de calidad diagnóstica** integrado como **módulo nuevo y aislado** dentro de la app .NET 8 (`https://appsintranet.esculapiosis.com/PortalImagenologia`, IIS / Windows Server 2012 R2), disponible **solo desde el módulo Portal Web Radiólogos** (controlador real `PortalRadiologosController`). El radiólogo, desde la grilla de estudios programados, pulsará "Ver imágenes"; la app .NET 8 actuará como **broker de seguridad**: valida la sesión (rol Administrador/Radiologo), resuelve el estudio por Caso/Cuenta o Identificación, emite un **token corto** y audita. El visor consumirá las imágenes vía **DICOMweb moderno servido por Orthanc actuando como gateway** hacia el PACS clásico dcm4chee (que solo expone WADO-URI/DIMSE, no DICOMweb REST). El entregable cierra las 9 tareas del prompt maestro, resuelve las 8 ambigüedades del andamiaje con decisiones cerradas, mantiene intacto el código operativo, no expone secretos ni PHI, actualiza GitHub con commits incrementales y entrega una **Guía PDF** de instalación/configuración/implementación.

---

## 2. Alcance

### Dentro de alcance
- Módulo **Visor** nuevo (controlador, servicios, modelos, vistas, JS/CSS y config propios), sin tocar archivos operativos.
- **Broker de seguridad .NET 8:** resolución Caso/Identificación → `StudyInstanceUID`, token corto, autorización por rol y por caso, auditoría.
- **Integración de UI** del botón "Ver imágenes" **únicamente en Portal Web Radiólogos** (grilla de estudios y/o panel de detalle de `Views/PortalRadiologos/Index.cshtml`, sin modificar la lógica existente — solo aditivo).
- **APIs REST del broker** (contratos).
- **Estrategia de consumo del PACS** vía Orthanc-gateway DICOMweb (decisión ratificada, ver §3).
- **Diseño funcional del visor** (Zoom, Pan, Window Level, Medición, Rotación, Invertir, Series, navegación, MPR si aplica, comparación, descarga, impresión).
- **Guía PDF** paso a paso y **actualización incremental de GitHub** + notificación por Telegram.

### Fuera de alcance (salvo orden expresa del Lead)
- Botón "Ver imágenes" en Lecturas/Reportes u otros módulos.
- Modificación de código operativo/funcional existente.
- **Instalación/configuración física de Orthanc en el servidor real** (queda como **precondición** a confirmar en F0, no como tarea de ejecución de este plan).
- Despliegue a producción (será una puerta aparte con aprobación del Lead — QUICKSILVER).
- Cambios en el esquema de la BD clínica más allá de una tabla/SP de auditoría del visor (ver decisión #7).

---

## 3. Decisión de arquitectura

### 3.1 Restricción de partida
`dcm4chee` (backends `172.16.10.100` / `172.16.50.100`, AE `DCM4CHEE`, DIMSE `11112`) **solo expone WADO-URI clásico** (`:8080/wado`, imagen JPEG por objeto) y **DIMSE**; **no expone DICOMweb REST moderno** (QIDO-RS / WADO-RS / STOW-RS). Un visor diagnóstico de nueva generación (OHIF, Cornerstone3D) **requiere DICOMweb**. Por tanto, la elección de visor y la elección de estrategia de acceso al PACS están acopladas.

### 3.2 Comparativa de visores

| Criterio | **OHIF Viewer v3** | **Cornerstone3D (embebido a medida)** | **Weasis** | **Stone Web Viewer (Orthanc)** |
|---|---|---|---|---|
| Calidad diagnóstica | Muy alta (2D/MPR/series, herramientas completas) | Muy alta (control total, se construye lo que se necesita) | Muy alta (aplicación de escritorio madura) | Alta (2D sólido; MPR limitado) |
| Complejidad de integración | Media (build de SPA + reverse proxy + config datasource) | Alta (hay que construir la UI y la toolbar) | Baja como app externa / Alta para integrarla web | Muy baja (viene con el plugin de Orthanc) |
| Tecnología de acceso | **DICOMweb** (QIDO/WADO-RS) | **DICOMweb** o WADO-URI custom | DIMSE / WADO / DICOMweb | DICOMweb (interno de Orthanc) |
| Integración .NET8 / IIS / WS2012R2 | Buena (SPA estática + proxy ARR; no depende del SO) | Buena (assets estáticos en `wwwroot`) | Pobre (JNLP/desktop, no embebible limpio, requiere Java en cliente) | Buena pero atada a Orthanc |
| Costo / licencia | **MIT (gratis, comercial-friendly)** | **MIT (gratis)** | EPL/GPL (revisar distribución) | Orthanc: GPLv3 / plugins con excepciones |
| Rendimiento | Muy bueno (lazy-load, web workers, codecs) | Muy bueno (ajustable) | Muy bueno (nativo) | Bueno |
| Herramientas out-of-the-box | Completas (Zoom/Pan/WL/medición/rotación/invert/MPR/comparación) | Nulas de fábrica (se implementan) | Completas | Básicas-medias |
| Tiempo a "diagnóstico usable" | **Bajo/medio** | Alto | Bajo (pero fuera del navegador) | El más bajo (prueba de humo) |

### 3.3 Recomendación final (decisiva)
- **Visor recomendado: OHIF Viewer v3 (MIT)** empotrado, apuntando a la fuente DICOMweb de Orthanc, servido mismo-origen bajo `/PortalImagenologia/`. Razón: máxima calidad diagnóstica con el **menor esfuerzo** frente a construir todo con Cornerstone3D; MIT sin fricción de licencia; probado sobre IIS al ser una SPA estática detrás de reverse proxy; herramientas diagnósticas completas ya incluidas.
- **Cornerstone3D** se descarta como base principal por sobrecoste de construir toolbar/MPR/comparación desde cero; se mantiene como *fallback conceptual* (OHIF ya usa Cornerstone3D por debajo, así que no se pierde nada).
- **Weasis** se descarta para el flujo embebido (es desktop/JNLP, requiere Java en el cliente, mala integración web).
- **Stone Web Viewer** se usará **solo como prueba de humo en F0/F2** para validar que la cadena Orthanc→DICOMweb funciona antes de invertir en el build de OHIF.
- **Estrategia de acceso al PACS: se RATIFICA "Orthanc como gateway DICOMweb"** del andamiaje, con estos **ajustes obligatorios**:
  1. Es una estrategia **condicionada a F0**: hasta no confirmar Orthanc desplegado + plugin DICOMweb + ruta de red PACS→Orthanc (puerto 4242 para recibir C-STORE del C-MOVE) + credenciales + retención, **no se compromete implementación** contra ella.
  2. Modelo de flujo real: Orthanc hace **C-FIND** al PACS (búsqueda), **C-MOVE bajo demanda** del estudio elegido hacia sí mismo (cache), y **re-expone DICOMweb** que consume OHIF. Este patrón "fetch-on-demand + cache" es correcto y se conserva.
  3. **Plan B documentado** (por si F0 falla y Orthanc no puede desplegarse a tiempo): visor de fidelidad reducida basado en **WADO-URI JPEG** de dcm4chee (`:8080/wado`) renderizado con un visor 2D ligero. Es inferior en calidad diagnóstica (sin window-level dinámico real ni MPR) pero no bloquea la entrega. Se elige A (Orthanc/OHIF) como camino principal; B queda como contingencia explícita, no como diseño paralelo.

---

## 4. Arquitectura recomendada (diagrama)

```
                         Windows Server 2012 R2 · IIS
  ┌───────────────────────────────────────────────────────────────────────────┐
  │  https://appsintranet.esculapiosis.com/PortalImagenologia  (mismo origen)   │
  │                                                                             │
  │   [Navegador · Radiólogo]                                                   │
  │        │  (1) click "Ver imágenes" (fila del estudio)                       │
  │        ▼                                                                     │
  │   App .NET 8  ──►  MÓDULO VISOR (NUEVO, aislado)                             │
  │   (cookie auth,    ┌───────────────────────────────────────────────┐       │
  │    ISessionService)│ VisorController  (broker de seguridad)         │       │
  │        │           │  Resolver → Token(corto) → Abrir → Preview     │       │
  │        │           │  VisorTokenService (HMAC/JWT ~10 min)          │       │
  │        │           │  VisorAuditoriaService (log + BD)              │       │
  │        │           │  DicomWebClient / OrthancGatewayService        │       │
  │        │           └───────────────────────────────────────────────┘       │
  │        │  (2) resuelve Caso/Identificación → StudyInstanceUID (QIDO/C-FIND) │
  │        ▼                                                                     │
  │   BD clínica (API Esculapio / SP)  ◄── (mapea Caso/Cuenta ↔ paciente)       │
  │        │                                                                     │
  │        │  (3) navegador abre OHIF (SPA estática en wwwroot, mismo origen)    │
  │        ▼                                                                     │
  │   OHIF Viewer  ──(4) DICOMweb QIDO/WADO-RS, mismo origen──►                  │
  │                    IIS reverse proxy (ARR + URL Rewrite)                     │
  │                       /PortalImagenologia/dicomweb  ─────────►  Orthanc      │
  └───────────────────────────────────────────────────────────────────────────┘
                                                        (localhost:8042, sin acceso externo)
                                                              │  Orthanc GATEWAY
                                                              │  C-FIND / C-MOVE (DIMSE)
                                                              ▼
                       ┌──────────────────────────────────────────────────────┐
                       │  PACS dcm4chee  AE=DCM4CHEE  DIMSE 11112               │
                       │   172.16.10.100 (CAMPBELL/FUNDACION) · 172.16.50.100   │
                       │   (SANTAMARTA) — WADO-URI :8080/wado (JPEG)            │
                       │  PACS Orthanc real  AE=ESCULAPIO_ORTHANC 192.168.2.17  │
                       │   DIMSE 4242 · REST/DICOMweb 8042                      │
                       └──────────────────────────────────────────────────────┘
```

**Notas del flujo:** (a) OHIF nunca habla directo con dcm4chee; (b) Orthanc solo es alcanzable en `localhost` (`RemoteAccessAllowed=false`), IIS termina TLS y hace de proxy de solo lectura; (c) el token corto liga usuario+estudio y expira ~10 min; (d) toda apertura/descarga/impresión se audita.

---

## 5. Fases del plan

> Regla transversal (todas las fases): **NUNCA** se modifican archivos operativos existentes. Todo lo nuevo vive en carpetas propias del módulo Visor. Namespace definitivo = `WebImagenologia.Web.*` (ver decisión #1). Commits incrementales por fase + aviso Telegram.

### F0 — Precondiciones y validación contra el servidor real
- **Objetivo:** confirmar que la estrategia Orthanc-gateway es viable en producción ANTES de construir contra ella.
- **Entregables:** checklist de precondiciones verificado; documento `docs/visor/F0-validacion-entorno.md` con resultados reales; decisión A (Orthanc/OHIF) vs Plan B (WADO-URI).
- **Archivos/carpetas nuevos:** `docs/visor/` (solo documentación, sin PHI).
- **Verificaciones concretas:**
  1. ¿Orthanc desplegado en `192.168.2.17:8042` con plugin DICOMweb activo? (`GET /system`, `GET /dicom-web/studies`).
  2. Ruta de red IIS→Orthanc y **PACS→Orthanc:4242** (el C-MOVE necesita que dcm4chee alcance a Orthanc por C-STORE) — firewall/VLAN.
  3. Credenciales Orthanc de producción (fuera del repo).
  4. Política de retención/caché de Orthanc (tamaño de disco, modo Recycle).
  5. Registrar el AET `ESCULAPIO_ORTHANC` (host+4242) **en el PACS dcm4chee**.
  6. Prueba de humo con **Stone Web Viewer** sobre un estudio real de test.
- **Riesgos:** Orthanc no desplegado; PACS no puede hacer C-STORE de vuelta; sin credenciales.
- **Criterio de aceptación:** checklist 1–6 respondido con evidencia real; decisión A/B tomada y registrada.

### F1 — Broker de seguridad .NET 8 + APIs REST
- **Objetivo:** implementar el módulo Visor server-side (resolución, token, auditoría, APIs), integrado con `ISessionService` real.
- **Entregables:** `VisorController`, `VisorTokenService`, `VisorAuditoriaService`, `DicomWebClient`, `OrthancGatewayService`, `VisorModels`/`VisorOptions`, registro DI, sección de config `Visor` (sin secretos).
- **Archivos/carpetas nuevos (rutas propuestas, no tocan lo existente):**
  - `src/WebImagenologia.Web/Controllers/VisorController.cs`
  - `src/WebImagenologia.Web/Services/Visor/` (los servicios e interfaces)
  - `src/WebImagenologia.Web/Models/Visor/` (DTOs + `VisorOptions`)
  - Registro DI: fichero de extensión `AddVisorModule(this IServiceCollection)` invocado con **una sola línea aditiva** en `Program.cs` (ver decisión #3).
  - `appsettings.json`: sección `Visor` **sin** `TokenSecret`/credenciales (User-Secrets/env).
- **Riesgos:** dependencia de F0 (endpoint Orthanc); mapeo Caso↔Accession (ver decisión #4 y §9).
- **Criterio de aceptación:** compila; `Resolver` devuelve estudios contra Orthanc de test; `Token` emite y `Abrir` valida/expira; auditoría registra evento; ningún archivo operativo modificado (solo la línea aditiva en `Program.cs`).

### F2 — Integración con Orthanc / PACS (gateway DICOMweb)
- **Objetivo:** dejar la cadena de datos extremo a extremo funcionando: C-FIND→C-MOVE→DICOMweb→OHIF.
- **Entregables:** `orthanc.json` corregido con **datos reales** (ver decisión — quitar `PACS_SERVER`); regla de reverse proxy `web.dicomweb.config` para `/PortalImagenologia/dicomweb` (ARR+URL Rewrite) como **fragmento a fusionar** documentado, no sobrescribiendo el `web.config` operativo a mano sino con instrucción de fusión en la Guía; script Lua de solo-lectura opcional.
- **Archivos/carpetas nuevos:** `deploy/orthanc/orthanc.json`, `deploy/iis/web.dicomweb.config`, `deploy/orthanc/orthanc-readonly.lua`, `docs/visor/F2-cadena-dicomweb.md`.
- **Riesgos:** BulkDataURI de OHIF debe coincidir con el sub-path público (`DicomWeb.Root` = `/PortalImagenologia/dicomweb/`) — a confirmar (decisión #6); CORS evitado por mismo-origen.
- **Criterio de aceptación:** `GET /PortalImagenologia/dicomweb/studies?AccessionNumber=<caso-real-test>` devuelve el `StudyInstanceUID`; un C-MOVE bajo demanda trae el estudio a Orthanc y OHIF lo abre.

### F3 — UI del visor en Portal Web Radiólogos
- **Objetivo:** botón "Ver imágenes" en la grilla/panel de detalle de Portal Radiólogos + vista que embebe OHIF.
- **Entregables:** vistas `Views/Visor/Abrir.cshtml` y `Views/Visor/TokenInvalido.cshtml`; SPA de OHIF compilada en `wwwroot/visor/`; JS aditivo `wwwroot/js/visor-ver-imagenes.js` que añade el botón por fila usando los `data-*` ya presentes (`data-consecutivo`, `data-empresa`, `data-no-cuenta`) **sin editar la lógica existente del Index**.
- **Archivos/carpetas nuevos:** `src/WebImagenologia.Web/Views/Visor/`, `src/WebImagenologia.Web/wwwroot/visor/` (build OHIF), `src/WebImagenologia.Web/wwwroot/js/visor-ver-imagenes.js`, `src/WebImagenologia.Web/wwwroot/css/visor.css`.
- **Punto de integración (decisión #8):** el botón se inyecta por JS/partial en la **columna de acciones** de cada fila y/o en el **panel de detalle**, apoyándose en los `data-*` existentes; se prefiere una **partial view aditiva** referenciada desde el layout del módulo antes que editar `Index.cshtml`. Si fuese imprescindible una línea en `Index.cshtml`, se limita a incluir la partial (cambio aditivo mínimo, aprobado explícitamente).
- **Build/ubicación de OHIF (decisión #5):** OHIF se compila **fuera del repo** y su `dist/` se copia a `wwwroot/visor/` (assets estáticos versionados, sin node_modules), servido mismo-origen bajo `/PortalImagenologia/visor/`. No se despliega como app IIS separada.
- **Riesgos:** peso del bundle; rutas base del SPA bajo sub-path `/PortalImagenologia`.
- **Criterio de aceptación:** desde una fila de estudio, "Ver imágenes" abre OHIF con el estudio correcto; funcionan Zoom/Pan/WL/medición/rotación/invert/series/navegación; MPR si la modalidad lo soporta; descarga e impresión operativas.

### F4 — Seguridad y auditoría
- **Objetivo:** endurecer auth/autorización/token/auditoría y manejo de secretos.
- **Entregables:** autorización por rol (`AdministradorOrRadiologo`) reutilizando el atributo existente; **autorización por caso** (el radiólogo solo abre estudios que le corresponden, validado contra la API/BD antes de emitir token); token corto firmado; Orthanc no expuesto (`RemoteAccessAllowed=false`); proxy de solo lectura; persistencia de auditoría (decisión #7); secretos en User-Secrets/env.
- **Archivos/carpetas nuevos:** `docs/visor/F4-seguridad.md`; (si aplica) SP/tabla de auditoría en script `deploy/db/auditoria-visor.sql`.
- **Riesgos:** fuga de PHI en logs; token replay; acceso lateral a Orthanc.
- **Criterio de aceptación:** BLACK WIDOW valida: HTTPS forzado, roles aplicados, autorización por caso efectiva, token expira, Orthanc inaccesible desde la red, auditoría persistida sin PHI en claro; ningún secreto en el repo.

### F5 — Guía PDF, cierre GitHub y notificación
- **Objetivo:** empaquetar el conocimiento operativo y cerrar entrega.
- **Entregables:** **Guía PDF** paso a paso (instalación de Orthanc, config, plugin DICOMweb, reverse proxy IIS, build/copia de OHIF, sección `Visor`, User-Secrets, pruebas, troubleshooting), con carpetas/archivos implicados; historial de commits incrementales en GitHub; avisos por Telegram.
- **Archivos/carpetas nuevos:** `docs/visor/Guia-Instalacion-Visor.pdf` (+ fuente `.md`).
- **Riesgos:** que la guía filtre secretos/PHI (revisión obligatoria antes de subir).
- **Criterio de aceptación:** la guía permite a un tercero desplegar el visor de cero; commits incrementales verificables; nada de `.har`/PHI/secretos en GitHub.

---

## 6. Resolución explícita de las 8 inconsistencias del andamiaje

1. **Namespace.** **Decisión:** se usa el namespace real del proyecto **`WebImagenologia.Web.*`** (`.Controllers`, `.Services.Visor`, `.Models.Visor`). El `WebImagenologia` del andamiaje se descarta.
2. **Sesión.** **Decisión:** se inyecta y usa **`ISessionService`** (`ObtenerUsuario()`) para obtener usuario/rol/cédula. **Se elimina** el uso directo de `HttpContext.Session.GetString("Usuario"/"Cedula")` del andamiaje.
3. **DI faltante.** **Decisión:** se registran todos los servicios del visor mediante un método de extensión `AddVisorModule()` y se invoca con **una única línea aditiva** en `Program.cs` (no se reescribe el archivo). Requiere confirmar que `AddSession`/`UseSession` ya existen (sí existen) y el `PathBase /PortalImagenologia` (aplicado por IIS).
4. **Caso/Cuenta == AccessionNumber (0008,0050)?** **Decisión:** **NO se asume**. En F0/F2 se valida con un QIDO manual (`?AccessionNumber=<caso real de test>`). El `Resolver` implementará una **capa de mapeo** en el broker: primero intenta la relación **Caso/Cuenta→(AccessionNumber | PatientID | StudyInstanceUID)** resolviendo contra la **BD/API clínica** (que ya conoce `NoCuenta`↔paciente) y, si el PACS indexa por accession, usa accession; si no, cae a `PatientID` (Identificación) + filtros. El mapeo definitivo queda fijado por el resultado de F0 y documentado; no se hardcodea el supuesto.
5. **Build/ubicación de OHIF.** **Decisión:** build fuera del repo, `dist/` copiado a **`wwwroot/visor/`**, servido mismo-origen bajo `/PortalImagenologia/visor/`. **No** app IIS separada. (detalle en F3).
6. **`DicomWeb.Root` de Orthanc (`/PortalImagenologia/dicomweb/`).** **Decisión:** se **fija a `/PortalImagenologia/dicomweb/`** para que las BulkDataURI coincidan con el sub-path público del proxy, y se marca como **"a confirmar contra el servidor real en F0/F2"** (evidencia de QIDO real). Es la ruta objetivo, sujeta a verificación, no un dato ya cerrado.
7. **Persistencia de auditoría en BD.** **Decisión:** **se persiste desde el inicio** (no se difiere). Auditoría en dos capas: (a) `ILogger` estructurado inmediato y (b) tabla/SP dedicados (p. ej. `InsertAuditoriaVisorImagenesDiag`) vía la API/servicio de datos existente, con script en `deploy/db/auditoria-visor.sql`. Se registra usuario, cédula, StudyInstanceUID, acción (ABRIR/DESCARGAR/IMPRIMIR/EVENTO), IP y timestamp, **sin PHI en claro** más allá de identificadores necesarios.
8. **Punto de integración UI.** **Decisión:** botón "Ver imágenes" en la **columna de acciones de la grilla** de `Views/PortalRadiologos/Index.cshtml` (junto a "Seleccionar") y/o en el **panel de detalle**, inyectado de forma **aditiva** (partial view + JS que reusa `data-consecutivo`/`data-empresa`/`data-no-cuenta`), sin alterar la lógica de la vista operativa. Controlador de destino: `VisorController` nuevo. (Nota de corrección: el controlador real es **`PortalRadiologosController`**, no "RadiologosPortalController" como decía el andamiaje.)

> **Corrección adicional detectada (no dejar abierta):** `ActualizacionCodigo/orthanc.json` trae en `DicomModalities.pacs` el placeholder **`AET: "PACS_SERVER"` / `Host: 172.16.10.100`**. **Decisión:** en F2 el `orthanc.json` real usará **`AET: "DCM4CHEE"`**, hosts `172.16.10.100` y `172.16.50.100`, puerto `11112` (datos reales confirmados por HAR). Se prohíbe cualquier placeholder `PACS_SERVER`.

---

## 7. APIs REST propuestas (contratos a alto nivel)

Base (IIS aplica PathBase `/PortalImagenologia`). Todas exigen sesión válida + rol Administrador/Radiologo. El detalle fino (payloads/códigos) va en las specs.

| # | Verbo · Ruta | Contrato (1–2 líneas) |
|---|---|---|
| 1 | `GET /Visor/Resolver?caso=…` \| `?identificacion=…` | Resuelve el estudio por Caso/Cuenta o Identificación (mapeo broker→QIDO/C-FIND). Devuelve lista de estudios con `StudyInstanceUID`, modalidad, fecha, nº imágenes. |
| 2 | `POST /Visor/Token` `{ studyInstanceUID }` | Verifica autorización por caso, emite token corto firmado (~10 min) ligado a usuario+estudio, audita "ABRIR". Devuelve `{ token, expira, viewerUrl }`. |
| 3 | `GET /Visor/Abrir/{token}` | Valida token/expiración; sirve la vista que embebe OHIF apuntando a la fuente DICOMweb mismo-origen. Token inválido → vista `TokenInvalido`. |
| 4 | `GET /Visor/Preview?studyUid=&seriesUid=&instanceUid=&frame=&formato=` | Render JPG/PNG de una instancia (miniatura/descarga/impresión) vía WADO-RS rendered; audita "DESCARGAR". |
| 5 | `POST /Visor/Auditoria` `{ studyInstanceUID, accion, detalle }` | Registra eventos del cliente (medición, impresión, etc.) en la auditoría persistida. |
| — | `/PortalImagenologia/dicomweb/*` (reverse proxy, no es endpoint .NET) | QIDO-RS/WADO-RS servidos por Orthanc vía ARR, solo-lectura, mismo-origen (sin CORS). |

---

## 8. Seguridad

- **Transporte:** HTTPS forzado (IIS termina TLS; `UseHttpsRedirection` ya activo); Orthanc en HTTP solo localhost.
- **Autenticación:** cookie de sesión existente (`CookieAuthentication`) + `ISessionService`. El visor **no** crea un sistema de auth paralelo.
- **Autorización:** `[Authorize(Roles = RoleNames.Policies.AdministradorOrRadiologo)]` en `VisorController`; **más** autorización por caso (el radiólogo solo abre estudios que le corresponden, validado contra API/BD antes de emitir token).
- **Token corto:** firmado (HMAC-SHA256 stateless como en el andamiaje, o JWT equivalente), expiración ~10 min, liga usuario+`StudyInstanceUID`, comparación en tiempo constante. `TokenSecret` ≥32 chars **en User-Secrets/env, nunca en el repo**.
- **Protección de imágenes:** Orthanc `RemoteAccessAllowed=false` (solo localhost); IIS proxy de **solo lectura** (bloquea STOW/DELETE/modify); el navegador nunca toca dcm4chee ni Orthanc directamente.
- **Auditoría/trazabilidad:** doble capa (log + BD), sin PHI en claro; eventos ABRIR/DESCARGAR/IMPRIMIR/EVENTO con usuario, cédula, IP, estudio, timestamp.
- **Secretos:** `TokenSecret`, `OrthancUser`/`OrthancPassword` fuera del repo; `.har`/PHI jamás a GitHub (ya en `.gitignore`).

---

## 9. Riesgos globales y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| **Orthanc no desplegado/mal configurado en producción** (plugin DICOMweb, red, credenciales, retención) | Bloqueante para el camino principal (OHIF/DICOMweb) | F0 lo trata como **precondición dura** con evidencia real; **Plan B** WADO-URI JPEG documentado como contingencia. |
| **Caso/Cuenta NO mapea 1:1 a AccessionNumber (0008,0050)** | El `Resolver` no encuentra estudios | No asumir (decisión #4): validar con QIDO real; capa de mapeo broker con fallback a `PatientID`/Identificación resuelto contra BD clínica. |
| **PACS no puede hacer C-STORE de vuelta a Orthanc:4242** (firewall/VLAN) | C-MOVE falla, no llega el estudio | Verificar ruta PACS→Orthanc en F0; registrar AET en dcm4chee. |
| **BulkDataURI de OHIF no coincide con sub-path del proxy** | OHIF carga metadatos pero no imágenes | Fijar `DicomWeb.Root=/PortalImagenologia/dicomweb/`; validar con QIDO/WADO reales (decisión #6). |
| **Peso/latencia de estudios grandes (TC/RM)** | C-MOVE lento, mala UX | C-MOVE asíncrono con feedback; cache Orthanc (Recycle); THOR revisa performance. |
| **Modificación accidental de código operativo** | Regresión en producción | Todo aditivo en carpetas propias; único cambio permitido en `Program.cs` = 1 línea `AddVisorModule()`; WOLVERINE revisa diffs. |
| **Fuga de PHI/secretos a GitHub** | Incidente de seguridad/legal | `.gitignore` de `ActualizacionCodigo/`/`.har`; secretos en User-Secrets/env; revisión BLACK WIDOW antes de cada push. |
| **Licenciamiento del visor** | Riesgo legal | OHIF y Cornerstone3D son MIT; se descarta Weasis para embebido. |

---

## 10. Criterios de aceptación del PLAN completo (checklist, ligado a las 9 tareas del prompt maestro)

- [ ] **T1 Arquitectura** — diagrama + justificación + datos reales del PACS + decisión de gateway razonada (§3, §4).
- [ ] **T2 Flujo de integración** App↔IIS↔PACS↔Visor↔BD trazable extremo a extremo (§4, F1–F3).
- [ ] **T3 Comparativa** OHIF/Cornerstone/Weasis/Stone con ventajas/desventajas/complejidad/costo/licencia/rendimiento/integración + recomendación argumentada (§3.2–3.3).
- [ ] **T4 Flujo de búsqueda** por (a) Caso/Cuenta y (b) Identificación hasta ver el estudio, con resolución a `StudyInstanceUID` (decisión #4, API #1).
- [ ] **T5 Conversión vs nativo** — decisión DICOMweb+OHIF nativo (principal) vs WADO-URI JPEG (contingencia), con trade-off calidad/peso (§3.3, riesgos).
- [ ] **T6 Diseño funcional** — Zoom/Pan/WL/Medición/Rotación/Invertir/Series/navegación/MPR(si aplica)/comparación/descarga/impresión (F3).
- [ ] **T7 APIs REST** — contratos del broker definidos (§7) y detallados en specs.
- [ ] **T8 Seguridad** — HTTPS, auth cookie + roles, token corto, autorización por caso, protección de imágenes, auditoría/trazabilidad (§8, F4).
- [ ] **T9 GitHub + PDF** — commits incrementales sin secretos/PHI, avisos Telegram, Guía PDF paso a paso con carpetas/archivos (F5).
- [ ] **Extra** — las 8 (+1) ambigüedades del andamiaje resueltas con decisión cerrada (§6); ningún archivo operativo modificado salvo cambios aditivos aprobados.

---

## 11. Siguiente paso — SPECs a generar tras aprobación

> **NO se crean todavía.** Estas SPECs solo se redactarán **después de "APROBADO PLAN-002"** del Lead. Nombres tentativos, uno por fase/componente principal:

- **SPEC-001 — Precondiciones y validación de entorno (F0):** checklist verificable Orthanc/DICOMweb/red/credenciales/retención + decisión A/B.
- **SPEC-002 — Broker de seguridad .NET 8 y APIs REST (F1):** `VisorController`, servicios, DTOs, `VisorOptions`, DI aditivo, contratos detallados de las 5 APIs.
- **SPEC-003 — Gateway Orthanc DICOMweb + reverse proxy IIS (F2):** `orthanc.json` real, C-FIND/C-MOVE bajo demanda, regla ARR/URL Rewrite, validación de mapeo caso↔accession.
- **SPEC-004 — UI del visor en Portal Web Radiólogos (F3):** botón "Ver imágenes" aditivo, vistas `Abrir`/`TokenInvalido`, build y empotrado de OHIF, diseño funcional de herramientas.
- **SPEC-005 — Seguridad y auditoría (F4):** token corto, autorización por caso, hardening Orthanc/proxy, persistencia de auditoría (SP/tabla), manejo de secretos.
- **SPEC-006 — Guía PDF y cierre (F5):** estructura de la guía, plan de commits incrementales, notificaciones Telegram, checklist anti-PHI/anti-secretos.

Posible ADR asociado: **ADR-001 — "Orthanc como gateway DICOMweb + OHIF"** (registro de la decisión de arquitectura de §3, con su Plan B).
