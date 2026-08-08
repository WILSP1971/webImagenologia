# SPEC-004 — UI del visor en Portal Web Radiólogos (MedDream)

- **Fase del plan:** F3 (PLAN-002 §5)
- **Fecha:** 2026-08-08
- **Estado:** **REVISADO** — supersede de la versión OHIF (2026-08-07). Visor = **MedDream** (ADR-002).
- **Depende de:** SPEC-002 (broker), SPEC-003 (Orthanc/DICOMweb), ADR-002.

---

## 1. Objetivo

Integrar de forma **estrictamente aditiva** el botón **"Ver Imágenes"** en la grilla de estudios de Portal Web Radiólogos y lanzar **MedDream** (visor diagnóstico certificado) con token, sin reescribir la lógica de `Views/PortalRadiologos/Index.cshtml`.

## 2. Alcance

### Dentro
- Botón "Ver Imágenes" por fila (y/o panel detalle), vía partial + JS aditivo.
- Flujo cliente: `Resolver` → selección si hay varios estudios → `Token` → abrir `ViewerUrl` (MedDream).
- Vista `Abrir.cshtml`: puente que valida token de app y redirige a MedDream si el launch directo no se usó.
- `TokenInvalido.cshtml` (ya existente).

### Fuera
- Build/embebido de OHIF (descartado por ADR-002).
- Instalación física de MedDream en el servidor (documentada en Guía; requiere licencia Softneta).

## 3. Diseño

### 3.1 Integración aditiva del botón

- **Partial:** `Views/PortalRadiologos/_VisorBotonPartial.cshtml` — marca datos/URLs y carga el JS.
- **Una línea** al final de `Index.cshtml` (sección Scripts): `@await Html.PartialAsync("_VisorBotonPartial")`.
- **JS:** `wwwroot/js/visor-ver-imagenes.js` — inyecta botón junto a "Seleccionar"; no modifica handlers existentes.
- Reutiliza `data-consecutivo`, `data-empresa`, `data-no-cuenta`.

### 3.2 Flujo del botón

1. Click → `GET /Visor/Resolver?caso={NoCuenta}`.
2. Si 0 estudios → alerta.
3. Si ≥2 → mini-selector de `StudyInstanceUID` (modalidad/fecha/descripción).
4. `POST /Visor/Token` con antiforgery header `RequestVerificationToken`.
5. `window.open(viewerUrl)` → MedDream (`?token=...`) o puente `/Visor/Abrir/{token}`.

### 3.3 Launch MedDream

- Preferido: `VisorOptions.MedDreamEnabled=true` + TokenService → `ViewerUrl` = `{MedDreamViewerBaseUrl}/?token={meddreamToken}`.
- Fallback (TokenService no configurado): `ViewerUrl` = `/Visor/Abrir/{appToken}` y `Abrir` muestra enlace con `?study={uid}&storage={storage}` **solo si** `MedDreamAllowStudyQueryString=true` (lab); en producción exigir token.

## 4. Archivos

| Archivo | Acción |
|---|---|
| `Views/PortalRadiologos/_VisorBotonPartial.cshtml` | Nuevo |
| `wwwroot/js/visor-ver-imagenes.js` | Nuevo |
| `wwwroot/css/visor.css` | Nuevo |
| `Views/PortalRadiologos/Index.cshtml` | +1 línea Scripts |
| `Views/Visor/Abrir.cshtml` | Actualizar (MedDream) |
| `Services/Visor/MedDreamLaunchService.cs` | Nuevo |

## 5. Criterios de aceptación

- [ ] "Ver Imágenes" abre MedDream (o puente) con el estudio resuelto por `NoCuenta`.
- [ ] `git diff` de `Index.cshtml`: ≤1 línea aditiva de partial; lógica intacta.
- [ ] Token inválido → `TokenInvalido`.
- [ ] Sin modificar handlers de audio/selección existentes.
- [ ] Sin OHIF en `wwwroot/visor/` como requisito.
