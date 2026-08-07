# Cómo expone los datos el PACS (contexto para el enjambre)

> Documento de contexto para el diseño de **PortalImagenologia**
> (`https://appsintranet.esculapiosis.com/PortalImagenologia`, IIS sobre Windows Server 2012 R2).
> Responde a la pregunta del enjambre: *"¿cómo expone los datos el PACS?"*

## Resumen ejecutivo

Hoy ya existe un acceso web funcional al PACS: **Oviyam** corriendo en el servidor
`190.14.253.123` en `http://localhost:8080/oviyam/`. Oviyam es un visor DICOM web
*zero-footprint* que normalmente se despliega sobre **dcm4chee** (Tomcat en `:8080`).

Por tanto, **el nuevo portal no necesita inventar el acceso al PACS**: puede reutilizar
los mismos mecanismos que Oviyam ya usa.

## Cómo expone los datos el PACS

El PACS expone la información por dos vías:

1. **DIMSE (DICOM "clásico", sobre TCP)** — usado para *buscar/consultar* estudios:
   - `C-FIND` → consulta (Patient ID, Study Date, Modality, etc.)
   - `C-MOVE` / `C-GET` → recuperación de imágenes
   - Puerto típico: `104` u `11112`.
   - Requiere AE Title, host y puerto configurados (lo que Oviyam guarda por cada pestaña).

2. **WADO / DICOMweb (sobre HTTP)** — **la vía relevante para PortalImagenologia**,
   porque es HTTP y la puede consumir directamente una app web en IIS:
   - `WADO-URI` → recupera un objeto/imagen: `.../wado?requestType=WADO&studyUID=...&seriesUID=...&objectUID=...`
   - Si el backend es dcm4chee-arc 5.x, además hay **DICOMweb REST completo**:
     - `QIDO-RS` → búsqueda de estudios/series/instancias (JSON)
     - `WADO-RS` → recuperación de instancias/frames/metadata
     - `STOW-RS` → almacenamiento

### Las 4 pestañas de Oviyam

Las pestañas **CAMPBELL, FUNDACIONCAMPBELL, SANTAMARTA, VISORIMAGENLOGIA** son
**4 orígenes / AE Titles** ya configurados. Pueden ser 4 PACS distintos o 4 buzones/AE
del mismo dcm4chee. **Hay que confirmar cuál es cuál** (ver plantilla más abajo).

## Opciones de arquitectura para PortalImagenologia

- **Opción A — Rápida:** embeber Oviyam (iframe) o desplegar otra instancia bajo
  `/PortalImagenologia`. Menos control de UI, pero funciona ya.
- **Opción B — Propia:** construir el visor (p.ej. con **Cornerstone.js** / OHIF) consumiendo
  los **mismos endpoints WADO/DICOMweb** que Oviyam usa. Más trabajo, control total de UI/SSO
  bajo el dominio `appsintranet.esculapiosis.com`.

> Decisión pendiente del enjambre: A vs B. La elección depende del backend real
> (dcm4chee 2.x solo WADO-URI vs dcm4chee-arc 5.x con DICOMweb completo).

---

## Plantilla: 3 datos a confirmar en el servidor

> Completar directamente en el servidor `190.14.253.123` (o donde corra el PACS)
> y devolver esto al enjambre.

### 1. Endpoint WADO / DICOMweb real

Abrir un estudio en Oviyam → **F12 → pestaña Network** → copiar la(s) URL(s) de las
peticiones que traen la imagen. Esa URL es *literalmente* cómo se recuperan las imágenes.

- Base URL WADO observada: `__________________________________`
  _(ej. `http://localhost:8080/wado?requestType=WADO&studyUID=...`)_
- ¿Hay endpoints DICOMweb REST? (QIDO/WADO/STOW-RS): `[ ] Sí   [ ] No`
  - Base DICOMweb (si aplica): `__________________________________`
    _(ej. `http://localhost:8080/dcm4chee-arc/aets/{AET}/rs`)_
- ¿Requiere autenticación (usuario/token/Keycloak)? `[ ] No  [ ] Sí →` `__________`

### 2. Configuración de conexión al PACS (por cada AE / pestaña)

Revisar la config de Oviyam (`datasource.properties` o la administración de AEs)
y anotar los parámetros de cada origen:

| Pestaña            | AE Title | Host / IP | Puerto DIMSE | Notas |
|--------------------|----------|-----------|--------------|-------|
| CAMPBELL           |          |           |              |       |
| FUNDACIONCAMPBELL  |          |           |              |       |
| SANTAMARTA         |          |           |              |       |
| VISORIMAGENLOGIA   |          |           |              |       |

### 3. Backend y versión del PACS

- Software PACS: `[ ] dcm4chee 2.x   [ ] dcm4chee-arc 5.x   [ ] otro →` `__________`
- Versión exacta: `__________`
- Servidor de aplicaciones: `[ ] Tomcat  [ ] WildFly  [ ] otro →` `__________`
- ¿Corre en el mismo host que el web/IIS (`190.14.253.123`) o en otro? `__________`
- Puerto HTTP del backend: `__________` _(observado: 8080)_

---

## Notas de red / despliegue (a validar)

- El portal vive en IIS bajo `/PortalImagenologia`. Si consume el PACS por HTTP,
  hay que decidir si IIS actúa como **reverse proxy** hacia `:8080` (ARR) o si el
  navegador del usuario llega directo al PACS (CORS + accesibilidad de red).
- `localhost:8080` solo es accesible *desde el servidor*. Para acceso desde clientes
  hay que exponerlo (proxy en IIS es lo más limpio y mantiene todo bajo el mismo dominio HTTPS).

_Última actualización: 2026-08-07_
