# PROMPT — Diseño e implementación del Visor DICOM (PortalImagenologia)

> **Prompt maestro para el enjambre.** Este documento REEMPLAZA cualquier prompt/plan/spec
> anterior sobre el visor. Ver `docs/PACS-exposicion.md` para los datos reales del PACS
> (extraídos del HAR de producción — tienen prioridad sobre cualquier valor de ejemplo).

---

## ROL

Actúa como **Arquitecto Senior de Software** especializado en: Sistemas PACS, estándar DICOM,
RIS/HIS, DICOMweb, WADO, QIDO, STOW, Windows Server 2012 R2, IIS, .NET 8 con C#, integración de
sistemas médicos, desarrollo de visores de imágenes diagnósticas y telemedicina.

Tienes experiencia implementando visores DICOM comerciales y open source: **OHIF Viewer,
CornerstoneJS, Weasis, Orthanc Viewer, RadiAnt, MicroDicom, Lumier, OsiriX, Horos**.

## CONTEXTO

Existe una aplicación web de Imagenología en **C# / .NET 8**, publicada y funcional en **IIS**
sobre **Windows Server 2012 R2**, en `https://appsintranet.esculapiosis.com/PortalImagenologia`.
Repo: `https://github.com/WILSP1971/webImagenologia`.

**Antes de proponer nada, lee y analiza** el código de la opción **Portal Web Radiólogos**
del repo (donde se implementará el visor) y el andamiaje ya existente en `ActualizacionCodigo/`
(`DicomWebClient.cs`, `OrthancGatewayService.cs`, `VisorController.cs`, `VisorTokenService.cs`,
`VisorAuditoriaService.cs`, `appsettings.Visor.json`, `orthanc.json`, READMEs).

La app administra hoy la información clínica de estudios de imágenes. Se requiere **incorporar
un visor de imágenes diagnósticas** para imágenes DICOM que residen en un servidor **PACS**.

### Datos reales del PACS (confirmados por HAR, ver `docs/PACS-exposicion.md`)

- **dcm4chee** — AE `DCM4CHEE`, DIMSE `11112`, hosts `172.16.10.100` y `172.16.50.100`;
  recuperación por **WADO-URI** en `:8080/wado` (JPEG). No expone DICOMweb REST moderno.
- **Orthanc** — AE `ESCULAPIO_ORTHANC`, DIMSE `192.168.2.17:4242`, REST/DICOMweb en `:8042`.
- Acceso al estudio por **(a) Número de Caso/Cuenta** o **(b) Número de Identificación**.

> Ignora los valores de ejemplo tipo `PACS_SERVER` / `wadoUrl=http://172.16.10`: son
> placeholders. Los válidos son los de la tabla anterior y de `docs/PACS-exposicion.md`.

## OBJETIVO GENERAL

Diseñar e implementar la **mejor solución técnica** para un visor de imágenes diagnósticas
**dentro de la aplicación .NET 8 existente**, priorizando calidad diagnóstica, seguridad de
datos clínicos e integración limpia con IIS y el PACS.

## INVESTIGACIÓN (solo referencia; NO copiar implementación)

Analiza el flujo funcional, UX, arquitectura observable, integración posible, fortalezas,
debilidades y buenas prácticas de:

- `https://lumierdigital.com:8443/paciente/consulta.lu`
- `https://0300.lumierdigital.com/lumier-viewer4/v3/viewer?...`
- `https://www.que-pasa.co/consulta-resultados`
- `https://vallesalud.qpasa.com.co/#/home_reception`

Extrae **únicamente ideas de referencia**. No reproduzcas su código ni su UI.

## TAREAS (entregables esperados)

1. **Arquitectura recomendada** — justificada, con diagrama.
2. **Flujo de integración** entre: App Web ↔ IIS ↔ PACS ↔ Visor ↔ Base de datos.
3. **Alternativas de integración** (OHIF, Cornerstone, Weasis, otras) comparadas por:
   ventajas, desventajas, complejidad, costos, licenciamiento, rendimiento, facilidad de
   integración. Recomendación final argumentada.
4. **Flujo de búsqueda** por (a) Caso/Cuenta y (b) Identificación, hasta visualizar el estudio.
5. **Conversión de imágenes** — alternativas DICOM → JPG/PNG vs. renderizado DICOM nativo en
   navegador (Cornerstone). Indica qué sugieres y por qué (calidad diagnóstica vs. peso).
6. **Diseño funcional del visor** — mínimos: Zoom, Pan, Window Level, Medición, Rotación,
   Invertir colores, Series, navegación entre imágenes, MPR (si aplica), comparación de
   estudios, descarga, impresión.
7. **APIs REST necesarias** para la integración con la app .NET 8 (contratos, verbos, payloads).
8. **Seguridad** — HTTPS, autenticación, autorización, protección de imágenes médicas,
   auditoría, trazabilidad y control de acceso (aprovechar el token corto + auditoría ya
   andamiados).
9. **Actualiza el repo en GitHub durante el proceso** y genera una **Guía PDF** con el paso a
   paso de instalación, configuración e implementación del visor, incluyendo el contenido del
   desarrollo y las carpetas/archivos implicados en la mejora.

## RESTRICCIONES

- La solución **debe integrarse** con la aplicación .NET 8 existente.
- **Respeta el código operativo y funcional del repo: NO lo modifiques.** Todo lo nuevo va en
  módulos/carpetas propios del visor.
- Usa los datos reales del PACS de `docs/PACS-exposicion.md`, no los placeholders.
- `TokenSecret` y credenciales **nunca** se suben al repo (User-Secrets / variables de entorno).
