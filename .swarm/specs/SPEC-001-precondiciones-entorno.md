# SPEC-001 — Precondiciones y validación de entorno (F0)

- **Fase del plan:** F0 (PLAN-002 §5)
- **Autor:** DOCTOR STRANGE (Avengers Swarm)
- **Fecha:** 2026-08-07
- **Estado:** **APROBADO** por el Lead (2026-08-07) — "APROBADO SPEC-001..006"
- **Ejecutores previstos:** QUICKSILVER (infra/red/despliegue) y/o el Lead con acceso a la red del PACS. DOCTOR STRANGE define el CÓMO verificar; no ejecuta contra el servidor real.

---

## 1. Objetivo

Confirmar, con **evidencia real y reproducible**, que la estrategia de arquitectura ratificada (Orthanc como gateway DICOMweb + OHIF v3, ver ADR-001) es viable contra el entorno de producción **antes** de invertir esfuerzo de implementación (F1–F3) contra ella. El entregable clave de esta spec es una **decisión binaria registrada: Camino A (Orthanc/OHIF) vs Plan B (WADO-URI JPEG)**.

> Regla dura: **sin SPEC-001 verificada y decisión A/B tomada, F1–F3 NO deben ejecutarse contra el Orthanc real.** F1 puede avanzar en dev/mock (Orthanc local de laboratorio o stubs); F2/F3 contra el PACS real quedan bloqueadas hasta el visto bueno de F0.

## 2. Depende de

- Nada previo (es la primera puerta). Es dependencia de SPEC-002 (parcial), SPEC-003 (dura) y SPEC-004 (dura).

## 3. Alcance

### Dentro
- Verificación de las **6 precondiciones** de F0 con comandos/requests concretos.
- Registro de resultados reales en `docs/visor/F0-validacion-entorno.md` (sin PHI, sin secretos).
- Decisión A/B firmada por el Lead.

### Fuera
- Instalación/configuración física de Orthanc (queda como precondición a confirmar, no como tarea de este plan — PLAN-002 §2).
- Cualquier código .NET (eso es SPEC-002 en adelante).
- Cambios en dcm4chee salvo el **registro del AET receptor** (verificación #5), que lo ejecuta el administrador del PACS.

## 4. Diseño detallado — Checklist de verificación (6 puntos)

> Los valores reales (hosts, AETs, puertos) provienen de `docs/PACS-exposicion.md` (datos HAR reales, prioridad sobre placeholders). Sustituir `<...>` por datos de test reales al ejecutar.
> Convención de credenciales en los ejemplos: `-u usuario:clave` se toma de User-Secrets/variable de entorno del operador, **nunca** se escribe en el documento de resultados.

### V1 — Orthanc desplegado en `192.168.2.17:8042` con plugin DICOMweb activo
**Ejecuta:** QUICKSILVER (desde una máquina con ruta a `192.168.2.17`).
**Cómo verificar:**
```
# ¿Orthanc responde y qué versión?
curl -s -u <user>:<pass> http://192.168.2.17:8042/system
# Debe devolver JSON con "Name", "Version", "ApiVersion".

# ¿El plugin DICOMweb está cargado?
curl -s -u <user>:<pass> http://192.168.2.17:8042/plugins
# Debe listar "dicom-web" (o "orthanc-dicomweb").

# ¿DICOMweb responde QIDO?
curl -s -u <user>:<pass> http://192.168.2.17:8042/dicom-web/studies
# Debe devolver 200 con un array JSON (puede estar vacío si Orthanc no tiene estudios cacheados aún).
```
**Criterio OK:** `/system` responde 200; `/plugins` incluye el plugin DICOMweb; `/dicom-web/studies` responde 200 (array, aunque vacío).
**Criterio FALLA:** timeout, 401 persistente sin credenciales válidas, o `/plugins` no incluye DICOMweb.

### V2 — Ruta de red bidireccional: IIS→Orthanc y **PACS→Orthanc:4242** (C-STORE de vuelta)
**Ejecuta:** QUICKSILVER + administrador de red/PACS.
**Cómo verificar:**
```
# (a) Desde el servidor IIS hacia Orthanc REST (8042):
Test-NetConnection 192.168.2.17 -Port 8042      # PowerShell (WS2012R2)
# (b) Desde el servidor IIS hacia Orthanc DIMSE (4242):
Test-NetConnection 192.168.2.17 -Port 4242
# (c) CRITICO — desde el host del PACS dcm4chee hacia Orthanc:4242
#     (el C-MOVE hace que dcm4chee abra una conexion C-STORE de VUELTA a Orthanc):
Test-NetConnection 192.168.2.17 -Port 4242      # ejecutado EN 172.16.10.100 / .50.100
# (d) Desde Orthanc hacia el PACS DIMSE (11112):
Test-NetConnection 172.16.10.100 -Port 11112
Test-NetConnection 172.16.50.100 -Port 11112
```
**Criterio OK:** las 4 rutas (a,b,c,d) con `TcpTestSucceeded : True`. **La ruta (c) es la más frecuente causa de fallo silencioso** (firewall/VLAN entre segmento clínico y `192.168.2.x`).
**Criterio FALLA:** cualquiera de (a–d) bloqueada; en especial (c) → el C-MOVE nunca completa aunque el C-FIND funcione.

### V3 — Credenciales de Orthanc de producción disponibles fuera del repo
**Ejecuta:** el Lead / QUICKSILVER.
**Cómo verificar:** confirmar que existen `OrthancUser`/`OrthancPassword` y que autentican contra `/system` (V1). Registrar **solo** "credenciales confirmadas: SÍ/NO" en el documento de resultados; **jamás** el valor.
**Criterio OK:** un `GET /system` autenticado devuelve 200 y el par credencial vive en User-Secrets (dev) / variable de entorno (IIS). **Criterio FALLA:** no hay credenciales, o solo existen en texto plano en un archivo del repo.

### V4 — Política de retención / caché de Orthanc
**Ejecuta:** QUICKSILVER.
**Cómo verificar:**
```
curl -s -u <user>:<pass> http://192.168.2.17:8042/statistics
# CountStudies, TotalDiskSizeMB → cuánto ocupa hoy.
# Revisar orthanc.json en el servidor: MaximumStorageSize, MaximumPatientCount, StorageCompression.
```
**Criterio OK:** hay una política definida (modo Recycle con tope de tamaño o política de purga acordada) y disco suficiente para el patrón fetch-on-demand de TC/RM. **Criterio FALLA:** sin límite y disco insuficiente → riesgo de llenar el disco del nodo.

### V5 — AET `ESCULAPIO_ORTHANC` registrado en el PACS dcm4chee como destino C-MOVE
**Ejecuta:** administrador del PACS dcm4chee.
**Cómo verificar:**
```
# En dcm4chee: el AE receptor ESCULAPIO_ORTHANC (host 192.168.2.17, puerto 4242)
# debe estar dado de alta como destino valido de C-MOVE.
# Verificacion funcional (C-ECHO desde Orthanc hacia el PACS y viceversa):
curl -s -u <user>:<pass> -X POST http://192.168.2.17:8042/modalities/pacs/echo
# 200 => Orthanc ve al PACS. Debe existir la modality "pacs" (config de SPEC-003).
```
**Criterio OK:** C-ECHO Orthanc→PACS exitoso **y** `ESCULAPIO_ORTHANC:192.168.2.17:4242` registrado como AE en dcm4chee (equivalente al `EchoSuccess` histórico de Oviyam en `docs/PACS-exposicion.md`). **Criterio FALLA:** el PACS no conoce el AET receptor → rechaza el C-MOVE.

### V6 — Prueba de humo con Stone Web Viewer sobre un estudio real de test
**Ejecuta:** QUICKSILVER + un radiólogo/Lead que aporte un caso de test.
**Cómo verificar:**
```
# 1) Disparar C-MOVE de un estudio de test desde el PACS hacia Orthanc:
curl -s -u <user>:<pass> -X POST http://192.168.2.17:8042/modalities/pacs/move \
  -d '{ "Level":"Study", "Resources":[{ "StudyInstanceUID":"<UID-de-test>" }], "TargetAet":"ESCULAPIO_ORTHANC" }'
# 2) Confirmar que llego a Orthanc:
curl -s -u <user>:<pass> "http://192.168.2.17:8042/dicom-web/studies?StudyInstanceUID=<UID-de-test>"
# 3) Abrir en Stone Web Viewer (plugin de Orthanc) y ver la imagen:
#    http://192.168.2.17:8042/stone-webviewer/index.html?study=<UID-de-test>
```
**Criterio OK:** el estudio de test se ve completo en Stone Web Viewer → **la cadena Orthanc→DICOMweb funciona** y OHIF (que usa la misma fuente) también funcionará. **Criterio FALLA:** el C-MOVE no completa (revisar V2c/V5) o Stone no renderiza.

## 5. Contratos / Archivos afectados

Todos **nuevos**, solo documentación (sin PHI, sin secretos):

- `docs/visor/F0-validacion-entorno.md` — plantilla de resultados con una fila por verificación V1–V6: `estado (OK/FALLA/N-A)`, `evidencia resumida (sin PHI)`, `ejecutor`, `fecha`.
- Sección final **"Decisión A/B"** en ese mismo documento, firmada por el Lead.

**No** se toca ningún archivo operativo ni de código en esta fase.

## 6. Criterio de decisión A vs B (registro obligatorio)

| Resultado | Decisión |
|---|---|
| **V1..V6 = OK** | **Camino A** — Orthanc/OHIF (principal). Se habilitan SPEC-002 (integración real), SPEC-003 y SPEC-004 contra el entorno real. |
| **V1 OK pero V2c/V5 FALLA** (C-MOVE no vuelve) | Bloqueo parcial: escalar a red/PACS para abrir la ruta. Si no se resuelve en el plazo → **Plan B**. |
| **V1 FALLA** (Orthanc no desplegado / sin plugin) | Escalar despliegue de Orthanc a QUICKSILVER/infra. Si no hay Orthanc a tiempo → **Plan B**. |
| **Plan B (WADO-URI JPEG de dcm4chee `:8080/wado`)** | Visor 2D de fidelidad reducida (sin WL dinámico real ni MPR). No bloquea la entrega. F2/F3 se rediseñan sobre WADO-URI (contingencia documentada, no diseño paralelo — PLAN-002 §3.3). |

## 7. Criterios de aceptación verificables (checklist)

- [ ] V1 ejecutada con evidencia (JSON de `/system`, `/plugins`, `/dicom-web/studies`).
- [ ] V2 (a,b,c,d) ejecutada; ruta crítica PACS→Orthanc:4242 confirmada explícitamente.
- [ ] V3 credenciales confirmadas fuera del repo (registrado SÍ/NO, nunca el valor).
- [ ] V4 política de retención/caché registrada.
- [ ] V5 C-ECHO Orthanc↔PACS y AET receptor registrado en dcm4chee.
- [ ] V6 estudio de test visible en Stone Web Viewer.
- [ ] `docs/visor/F0-validacion-entorno.md` creado, sin PHI ni secretos.
- [ ] **Decisión A/B tomada y firmada por el Lead** en el documento.

## 8. Riesgos específicos

- **Ruta C-STORE de vuelta (V2c) bloqueada por firewall/VLAN** entre el segmento clínico (`172.16.x`) y `192.168.2.17` — es el fallo más común y silencioso (el C-FIND funciona, el C-MOVE "cuelga"). Mitigación: verificar (c) explícitamente desde el host del PACS.
- **Orthanc sin plugin DICOMweb** aunque el REST responda → todo OHIF depende de esto.
- **Filtrar PHI** al copiar respuestas reales en el documento de resultados. Mitigación: registrar solo StudyInstanceUID de test y conteos, nunca nombres/identificaciones de pacientes.
- **Credenciales en claro** en el documento de evidencias. Mitigación: prohibido; solo "confirmadas SÍ/NO".
