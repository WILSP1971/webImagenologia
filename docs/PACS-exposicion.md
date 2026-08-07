# Cómo expone los datos el PACS (contexto real para el enjambre)

> Documento de contexto para el diseño de **PortalImagenologia**
> (`https://appsintranet.esculapiosis.com/PortalImagenologia`, IIS sobre Windows Server 2012 R2).
> Responde a la pregunta del enjambre: *"¿cómo expone los datos el PACS?"*
>
> **Los datos de este documento NO son suposiciones.** Se extrajeron de la captura de red
> real (`ActualizacionCodigo/localhost.har`) del visor Oviyam en producción, endpoint
> `DicomNodes.do` y `Echo.do`.

## Resumen ejecutivo

El acceso web al PACS hoy es **Oviyam** en `http://localhost:8080/oviyam/` sobre **dcm4chee**
(Tomcat en `:8080`). Oviyam consulta por **DIMSE** y recupera imágenes por **WADO-URI**
(`:8080/wado`) devolviéndolas ya como **JPEG**.

Existen **dos backends PACS** y **cuatro orígenes lógicos** (las pestañas de Oviyam).

## Datos reales confirmados (fuente: `DicomNodes.do`)

| Pestaña (logicalname) | Backend | Host | Puerto DIMSE | AE Title | Retrieve | WADO | imageType | previews |
|-----------------------|---------|------|-------------|----------|----------|------|-----------|----------|
| **CAMPBELL**          | dcm4chee | `172.16.10.100` | `11112` | `DCM4CHEE` | WADO | `:8080/wado` | JPEG | true |
| **FUNDACIONCAMPBELL** | dcm4chee | `172.16.10.100` | `11112` | `DCM4CHEE` | WADO | `:8080/wado` | JPEG | false |
| **SANTAMARTA**        | dcm4chee | `172.16.50.100` | `11112` | `DCM4CHEE` | WADO | `:8080/wado` | JPEG | true |
| **VISORIMAGENLOGIA**  | **Orthanc** | `192.168.2.17` | `4242` | `ESCULAPIO_ORTHANC` | WADO | `:8080/wado` | JPEG | true |

- **AE llamante (calling AET) de Oviyam:** `OVIYAM2`, listener en puerto `1025`.
- **Verificación C-ECHO real y exitosa:**
  `Echo.do?dicomURL=DICOM://DCM4CHEE:OVIYAM2@172.16.10.100:11112` → `EchoSuccess`.

> **Corrección importante para el enjambre:** el AE Title real es **`DCM4CHEE`**, no
> `PACS_SERVER`, y el WADO está en el puerto **8080** contexto **`/wado`** (no en el `172.16.10`
> genérico que aparecía en la config de ejemplo del prompt). Usar SIEMPRE los valores de la tabla.

## Cómo expone los datos el PACS

1. **DIMSE (DICOM clásico, TCP)** — consulta y recuperación:
   - `C-FIND` → búsqueda (Patient ID, Study Date, Modality, etc.)
   - `C-MOVE` → recuperación de estudios/series hacia un AE receptor.
   - Puerto **`11112`** en dcm4chee; **`4242`** en el nodo Orthanc.
2. **WADO-URI (HTTP)** — recuperación de la imagen ya rasterizada:
   - `http://<host>:8080/wado?requestType=WADO&studyUID=...&seriesUID=...&objectUID=...&contentType=image/jpeg`
   - Es lo que Oviyam usa hoy (`imageType=JPEG`). Sirve para vista rápida, **no** trae el DICOM crudo con toda la metadata para un visor avanzado (MPR, window-level dinámico, etc.).

> **Limitación:** dcm4chee 2.x expone WADO-URI clásico (imagen JPEG/PNG por objeto), pero
> **no DICOMweb REST moderno** (QIDO-RS / WADO-RS / STOW-RS) que necesitan OHIF/Cornerstone
> para un visor de calidad diagnóstica. De ahí la estrategia del gateway (abajo).

## Estrategia recomendada: Orthanc como gateway DICOMweb

Ya hay nodo Orthanc en la red (`192.168.2.17:4242`, AE `ESCULAPIO_ORTHANC`). El enfoque que
el enjambre ya empezó a andamiar (ver `ActualizacionCodigo/`) es:

```
[Radiólogo] → HTTPS → IIS /PortalImagenologia (.NET 8)
                          │  (emite token corto, audita, autoriza por caso/identificación)
                          ▼
                    Orthanc (gateway DICOMweb)  ── C-MOVE/C-FIND (DIMSE) ──► dcm4chee (172.16.10.100 / .50.100)
                    localhost:8042
                    /PortalImagenologia/dicomweb  (QIDO-RS · WADO-RS · STOW-RS)
                          ▲
                          │  DICOMweb moderno (JSON + DICOM-P10)
                    [Visor OHIF / Cornerstone en el navegador]
```

- **Orthanc** actúa como C-MOVE SCU hacia dcm4chee, cachea los estudios y los re-expone como
  **DICOMweb** moderno en `http://localhost:8042/PortalImagenologia/dicomweb`.
- El **visor** (OHIF/Cornerstone) consume DICOMweb, no toca directamente el dcm4chee.
- La **app .NET 8** hace de broker de seguridad: valida sesión del radiólogo, resuelve
  caso/identificación → StudyInstanceUID, emite un **token corto (JWT ~10 min)** y audita.

Config ya presente en el repo (`ActualizacionCodigo/appsettings.Visor.json`):
- `OrthancRestBaseUrl`: `http://localhost:8042`
- `OrthancDicomWebBaseUrl`: `http://localhost:8042/PortalImagenologia/dicomweb`
- `OrthancAet`: `ESCULAPIO_ORTHANC`
- `TokenMinutos`: `10`  · `TokenSecret`: **no subir al repo** (User-Secrets / variable de entorno)

## Flujo de búsqueda (2 llaves de acceso)

El estudio se localiza por **(a) Número de Caso/Cuenta** o **(b) Número de Identificación**:

```
Caso/Cuenta  ─┐
              ├─► [.NET] resuelve → PatientID / StudyInstanceUID ─► QIDO-RS (Orthanc) ─► lista de estudios ─► OHIF abre StudyInstanceUID
Identificación┘
```

- La app ya tiene la relación clínica caso↔paciente en su BD; el visor solo necesita el
  **StudyInstanceUID** (o PatientID + filtros) para el `QIDO-RS`.

## Datos aún por confirmar en el servidor

1. **¿Orthanc ya está desplegado y con el plugin DICOMweb activo** en `192.168.2.17:8042`,
   o hay que instalarlo/configurarlo? (el `:4242` es su puerto DICOM; el `:8042` es el REST).
2. **Ruta de red desde el servidor web (IIS) hacia** `172.16.10.100`, `172.16.50.100` y
   `192.168.2.17` (¿mismo segmento? ¿firewall/VLAN entre intranet y PACS?).
3. **Credenciales de Orthanc** (`OrthancUser`/`OrthancPassword`) para producción.
4. **Volumen/retención**: ¿Orthanc cachea todo o hace fetch-on-demand y purga? (impacta disco).

---
_Fuente de datos: `ActualizacionCodigo/localhost.har`, `F12DOM.txt` — Última actualización: 2026-08-07_
