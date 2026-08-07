# SPEC-006 — Guía PDF y cierre (F5)

- **Fase del plan:** F5 (PLAN-002 §5, §10 T9)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Ejecuta (después de aprobación):** IRON MAN (orquesta commits/Telegram), QUICKSILVER (empaquetado PDF), con revisión anti-PHI de BLACK WIDOW.

---

## 1. Objetivo

Empaquetar el conocimiento operativo en una **Guía PDF** paso a paso, definir el **plan de commits incrementales** por fase, el **formato de notificación por Telegram**, y el **checklist final anti-PHI/anti-secretos** obligatorio antes de cada push a GitHub.

## 2. Depende de

- Todas las anteriores (SPEC-001..SPEC-005): la guía documenta lo implementado y validado.

## 3. Alcance

### Dentro
- Estructura/índice de la Guía PDF y su fuente `.md`.
- Plan de commits incrementales por fase con mensajes sugeridos.
- Formato y disparadores de notificación Telegram.
- Checklist anti-PHI/anti-secretos pre-push.

### Fuera
- Deploy a producción (puerta aparte).

## 4. Diseño detallado

### 4.1 Estructura de la Guía PDF (`docs/visor/Guia-Instalacion-Visor.md` → `.pdf`)

Índice propuesto:
1. **Introducción y arquitectura** (resumen de ADR-001: Orthanc gateway + OHIF; diagrama de PLAN-002 §4).
2. **Instalación de Orthanc** en `192.168.2.17` (servicio, puertos 4242/8042).
3. **Plugin DICOMweb** (habilitación y verificación `GET /plugins`).
4. **Configuración `orthanc.json`** (AET `ESCULAPIO_ORTHANC`, modalities `pacs`/`pacs_santamarta` con AET `DCM4CHEE`, `DicomWeb.Root`, `RemoteAccessAllowed=false`) — remitir a `deploy/orthanc/orthanc.json`.
5. **Registro del AET receptor** en dcm4chee (C-MOVE destino) y verificación C-ECHO.
6. **Reverse proxy IIS** (instalar ARR, fusionar el fragmento `deploy/iis/web.dicomweb.config`, **no sobrescribir** el web.config operativo).
7. **Build y copia de OHIF** (build fuera del repo con `PUBLIC_URL=/PortalImagenologia/visor/`, copiar `dist/` a `wwwroot/visor/`).
8. **Sección `Visor` en `appsettings`** (valores no secretos) + **User-Secrets/variables de entorno** (`TokenSecret`, `OrthancUser`, `OrthancPassword`).
9. **Base de datos de auditoría** (ejecutar `deploy/db/auditoria-visor.sql`).
10. **Pruebas** (smoke Stone; QIDO por accession; C-MOVE; OHIF abre; herramientas; token expira; auditoría persiste).
11. **Troubleshooting** (C-MOVE no vuelve → firewall PACS→Orthanc:4242; BulkDataURI ≠ sub-path; 401 Orthanc; assets 404 por base path).
12. **Anexo: carpetas y archivos implicados** (mapa de todo lo nuevo por fase).

Regla: la guía **no** contiene secretos ni PHI; usa `<placeholders>` para credenciales.

### 4.2 Plan de commits incrementales (mensajes sugeridos)

Un lote de commits por fase, cada uno atómico y aditivo:
- F0: `docs(visor): F0 validacion de entorno Orthanc/DICOMweb (sin PHI)`
- F1: `feat(visor): broker de seguridad .NET8 + APIs REST (VisorController, servicios, DTOs)` · `chore(visor): registro DI aditivo AddVisorModule en Program.cs`
- F2: `feat(visor): gateway Orthanc DICOMweb (DicomWebClient, OrthancGateway) + orthanc.json real (AET DCM4CHEE)` · `chore(iis): fragmento reverse proxy dicomweb (a fusionar)`
- F3: `feat(visor): UI boton Ver imagenes (aditivo) + vistas Abrir/TokenInvalido` · `chore(visor): build OHIF copiado a wwwroot/visor`
- F4: `feat(visor): autorizacion por caso + auditoria persistida (SP/tabla)` · `chore(security): hardening Orthanc/proxy solo-lectura`
- F5: `docs(visor): Guia PDF de instalacion/configuracion del visor`

Cada commit pasa el checklist §4.4 antes del push.

### 4.3 Notificación por Telegram (qué y cuándo)

- **Cuándo:** al cerrar cada **fase** (F0–F5) y al aprobarse cada **SPEC** ("APROBADO SPEC-XXX").
- **Formato sugerido:**
  ```
  [Avengers Swarm · webImagenologia]
  Fase: F<n> — <titulo>  |  Estado: CERRADA/APROBADA
  Commits: <hashes cortos>  |  Rama: main
  Resumen: <1-2 lineas, sin PHI ni secretos>
  Siguiente: <fase/spec pendiente + puerta de aprobacion>
  ```
- **Regla:** el mensaje **nunca** incluye StudyInstanceUID reales de pacientes, credenciales ni rutas con datos clínicos.

### 4.4 Checklist final anti-PHI/anti-secretos (pre-push, obligatorio)

- [ ] `git status`/`git diff` revisados: no hay `.har`, no hay dumps, no hay `ActualizacionCodigo/` (verificar `.gitignore`).
- [ ] grep de secretos = 0: `TokenSecret`, `OrthancPassword`, `Password`, claves API, connection strings con credenciales.
- [ ] Ningún archivo con **PHI** (nombres de pacientes, identificaciones reales, diagnósticos, StudyInstanceUID de pacientes reales en docs).
- [ ] `appsettings.json` / `orthanc.json` versionados **sin** valores secretos (solo placeholders).
- [ ] La Guía PDF y los `docs/visor/*` no filtran credenciales ni PHI.
- [ ] Solo cambios **aditivos** en código operativo (a lo sumo: 1 línea en `Program.cs` + 1 línea partial en `Index.cshtml`, ambas ya aprobadas).
- [ ] Revisión de BLACK WIDOW registrada antes del push.

## 5. Contratos / Archivos afectados

Todos **nuevos**:
- `docs/visor/Guia-Instalacion-Visor.md` (fuente) y `docs/visor/Guia-Instalacion-Visor.pdf` (entregable).
- (No modifica código operativo.)

## 6. Criterios de aceptación verificables (checklist)

- [ ] La Guía permite a un tercero desplegar el visor **de cero** (instalación→config→build→pruebas).
- [ ] Índice completo (12 secciones §4.1) presente; anexo de carpetas/archivos por fase.
- [ ] Historial de commits incrementales por fase verificable en GitHub, mensajes acordes a §4.2.
- [ ] Notificaciones Telegram emitidas al cierre de cada fase/aprobación de SPEC, con el formato §4.3.
- [ ] Checklist §4.4 aplicado en cada push; nada de `.har`/PHI/secretos en GitHub.

## 7. Riesgos específicos

- **La guía filtra secretos/PHI.** Mitigación: placeholders + revisión obligatoria de BLACK WIDOW antes de subir.
- **Commits no atómicos** que mezclen fases. Mitigación: un lote por fase, mensajes de §4.2.
- **PDF desactualizado** respecto a los archivos reales. Mitigación: generar el PDF desde el `.md` fuente al cierre de F5, tras validar F0–F4.
