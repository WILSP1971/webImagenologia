# SPEC-004 — UI del visor en Portal Web Radiólogos (F3)

- **Fase del plan:** F3 (PLAN-002 §5)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Implementa (después de aprobación):** SPIDER-MAN (UX) + DAREDEVIL (frontend) + CAPTAIN AMERICA (vistas/wiring). OHIF lo compila QUICKSILVER fuera del repo.

---

## 1. Objetivo

Integrar de forma **estrictamente aditiva** el botón "Ver imágenes" en la grilla de estudios de Portal Web Radiólogos, y embeber el visor **OHIF v3** (SPA estática servida mismo-origen bajo `/PortalImagenologia/visor/`) con las herramientas diagnósticas requeridas. **No se edita la lógica** de `Views/PortalRadiologos/Index.cshtml`.

## 2. Depende de

- **SPEC-002:** endpoints `Resolver`/`Token`/`Abrir`/`Preview`/`Auditoria`.
- **SPEC-003:** fuente DICOMweb operativa (`/PortalImagenologia/dicomweb/`). En dev/mock puede usarse un Orthanc de laboratorio.

## 3. Alcance

### Dentro
- Botón "Ver imágenes" por fila, inyectado vía **partial view + JS aditivo**, reusando los `data-*` ya presentes (`data-consecutivo`, `data-empresa`, `data-no-cuenta`).
- Vistas nuevas `Views/Visor/Abrir.cshtml` y `Views/Visor/TokenInvalido.cshtml`.
- Build de OHIF fuera del repo → `wwwroot/visor/` (assets estáticos, sin `node_modules`).
- Diseño funcional mínimo verificable de las herramientas.

### Fuera
- Endurecimiento de seguridad/auditoría → **SPEC-005** (esta spec consume los endpoints ya definidos).
- Config de Orthanc/proxy → **SPEC-003**.

## 4. Diseño detallado

### 4.1 Integración aditiva del botón (sin tocar la lógica del Index)

Estrategia (PLAN-002 decisión #8): **partial view aditiva + JS**, no reescritura del Index.

- **Partial nueva:** `src/WebImagenologia.Web/Views/PortalRadiologos/_VisorBotonPartial.cshtml` — renderiza el `<script>`/marcador que el JS usa para inyectar el botón. Se referencia con **una única línea aditiva** al final de `Index.cshtml` si es imprescindible: `@await Html.PartialAsync("_VisorBotonPartial")` (cambio aditivo mínimo, requiere OK explícito del Lead; si el layout del módulo permite incluirla sin tocar el Index, se prefiere esa vía).
- **JS aditivo:** `wwwroot/js/visor-ver-imagenes.js` — al cargar el DOM, recorre las filas de la grilla, lee `data-consecutivo`/`data-empresa`/`data-no-cuenta` **ya existentes** y añade un botón "Ver imágenes" en la columna de acciones (junto a "Seleccionar") y/o en el panel de detalle. **No** modifica handlers existentes; solo **agrega** listeners a elementos nuevos.
- **Flujo del botón (cliente):**
  1. click → `GET /Visor/Resolver?caso=<no-cuenta>` (o `?identificacion=` según disponibilidad).
  2. Si hay ≥1 estudio, `POST /Visor/Token { studyInstanceUID }` del estudio elegido (si hay varios, mostrar mini-selector).
  3. Con `viewerUrl` de la respuesta → abrir `GET /Visor/Abrir/{token}` en pestaña/modal.

### 4.2 Vistas nuevas

- `Views/Visor/Abrir.cshtml`: recibe el estudio validado por token y **embebe OHIF** apuntando a la fuente DICOMweb mismo-origen (`/PortalImagenologia/dicomweb/`) con `StudyInstanceUID` en query. Incluye `visor.css`. Enlaza los eventos de cliente (medición/impresión/descarga) al `POST /Visor/Auditoria`.
- `Views/Visor/TokenInvalido.cshtml`: mensaje claro de "enlace expirado o inválido, vuelva a abrir desde la grilla" (sin exponer detalles técnicos). Devuelta por el endpoint 3 cuando el token no valida.

### 4.3 Build y ubicación de OHIF (PLAN-002 decisión #5)

- OHIF v3 se compila **fuera del repo** (`yarn build` con `PUBLIC_URL=/PortalImagenologia/visor/` y datasource DICOMweb apuntando a `/PortalImagenologia/dicomweb/`).
- El `dist/` resultante se **copia** a `src/WebImagenologia.Web/wwwroot/visor/` (solo assets estáticos versionados; **sin** `node_modules`, sin fuentes de build en el repo).
- Servido mismo-origen bajo `/PortalImagenologia/visor/`. **No** app IIS separada.
- La `app-config.js` de OHIF fija el datasource `dicomweb` con `qidoRoot`/`wadoRoot` = `/PortalImagenologia/dicomweb/` y `singlepart`/`bulkDataURI` acorde a SPEC-003.
- Base path del SPA = `/PortalImagenologia/visor/` para que el routing interno de OHIF funcione bajo el sub-path.

### 4.4 Diseño funcional mínimo (herramientas OHIF a verificar)

Zoom, Pan, Window Level, Medición (longitud/ángulo/ROI), Rotación, Invertir, panel de Series, navegación entre imágenes (scroll/teclas), **MPR** (si la modalidad lo soporta: TC/RM con series volumétricas), **comparación** (dos estudios/series lado a lado), **descarga** (vía `/Visor/Preview`), **impresión** (imagen renderizada). Cada una out-of-the-box de OHIF v3 (herramientas completas, PLAN-002 §3.2).

### 4.5 Archivos estáticos

- `wwwroot/css/visor.css` — estilos del contenedor/modal del visor (aditivos, prefijados `visor-` para no colisionar).
- `wwwroot/js/visor-ver-imagenes.js` — inyección de botón + flujo cliente + envío de eventos de auditoría.

## 5. Contratos / Archivos afectados

Todos **nuevos** salvo la posible **única línea aditiva** en `Index.cshtml`:
- `src/WebImagenologia.Web/Views/PortalRadiologos/_VisorBotonPartial.cshtml` (nuevo)
- `src/WebImagenologia.Web/Views/Visor/Abrir.cshtml` (nuevo)
- `src/WebImagenologia.Web/Views/Visor/TokenInvalido.cshtml` (nuevo)
- `src/WebImagenologia.Web/wwwroot/visor/**` (build OHIF copiado, nuevo)
- `src/WebImagenologia.Web/wwwroot/js/visor-ver-imagenes.js` (nuevo)
- `src/WebImagenologia.Web/wwwroot/css/visor.css` (nuevo)
- **(condicionado a OK del Lead)** 1 línea `@await Html.PartialAsync("_VisorBotonPartial")` en `Views/PortalRadiologos/Index.cshtml`. La **lógica** del Index no se toca.

## 6. Criterios de aceptación verificables (checklist)

- [ ] Desde una fila de estudio, "Ver imágenes" abre OHIF con el **estudio correcto** (StudyInstanceUID coincide con el caso).
- [ ] `git diff` de `Index.cshtml`: a lo sumo **1 línea añadida** (la partial), cero líneas de lógica modificadas.
- [ ] Herramientas verificadas manualmente (una casilla cada una): [ ] Zoom [ ] Pan [ ] Window Level [ ] Medición [ ] Rotación [ ] Invertir [ ] Series [ ] Navegación [ ] MPR (si aplica a la modalidad) [ ] Comparación [ ] Descarga (`/Visor/Preview`) [ ] Impresión.
- [ ] Token expirado/inválido → se muestra `TokenInvalido.cshtml` (no error crudo).
- [ ] OHIF carga bajo `/PortalImagenologia/visor/` sin errores de ruta base ni de CORS (mismo-origen).
- [ ] `wwwroot/visor/` contiene solo assets estáticos (sin `node_modules`).
- [ ] Eventos de medición/impresión/descarga generan `POST /Visor/Auditoria`.

## 7. Riesgos específicos

- **Rutas base del SPA bajo sub-path** `/PortalImagenologia` → 404 de assets. Mitigación: `PUBLIC_URL`/base path fijados en el build.
- **Peso del bundle** OHIF → carga lenta. Mitigación: assets estáticos con cache de IIS; THOR revisa performance.
- **Colisión de JS/CSS** con la vista operativa. Mitigación: prefijos `visor-`, listeners solo sobre elementos nuevos, sin sobrescribir handlers.
- **Editar la lógica del Index** por descuido. Mitigación: criterio de aceptación de `git diff` (≤1 línea aditiva).
