# PROMPT PROFESIONAL — Visor DICOM PortalImagenologia (RESET, insumo de PLAN-002)

> XAVIER → DOCTOR STRANGE. Autocontenido. PLAN-001 y specs previas ARCHIVADOS: NO continuarlos.
> Fuentes autoritativas: `docs/PROMPT-visor-diagnostico.md` y `docs/PACS-exposicion.md` (datos HAR reales; prioridad sobre placeholders).

## 1. Objetivo medible ("terminado")
Entregar el DISEÑO técnico completo del visor DICOM integrado en el módulo **Portal Web Radiólogos** de la app .NET 8, materializado en `PLAN-002.md` + specs, que cubra las 9 tareas del prompt maestro con decisiones cerradas (no abiertas), datos reales del PACS, y una Guía PDF de instalación. "Terminado" = las 9 tareas tienen entregable verificable, las 8 ambigüedades del andamiaje están decididas explícitamente, y el repo refleja el progreso en GitHub sin exponer secretos ni PHI.

## 2. Alcance
**Dentro:** diseño+integración del visor SOLO en Portal Web Radiólogos; broker de seguridad en .NET 8 (resolución caso/identificación→StudyInstanceUID, token corto, auditoría); consumo del PACS vía la estrategia que STRANGE decida; APIs REST del broker; Guía PDF.
**Fuera (salvo orden expresa del Lead):** botón "Ver imágenes" en LecturasController/ReportesController; modificar código operativo; despliegue a producción; instalar/configurar Orthanc en el servidor real (queda como precondición a confirmar, no como tarea de este plan).

## 3. Ambigüedades que STRANGE DEBE cerrar con decisión explícita en PLAN-002
1. Namespace `WebImagenologia` (andamiaje) vs `WebImagenologia.Web.*` (real): definir el definitivo.
2. Sesión: usar `ISessionService` inyectado (real), NO `HttpContext.Session` directo del andamiaje.
3. DI: registrar `OrthancGatewayService` y demás servicios en Program.cs real.
4. Supuesto NO validado: ¿"Número de Caso/Cuenta" == AccessionNumber DICOM (0008,0050)? Definir cómo se verifica contra el PACS real antes de comprometer el mapeo.
5. OHIF no compilado en repo: decidir build y ubicación de despliegue (wwwroot vs app IIS separada).
6. Ruta `DicomWeb.Root` de Orthanc (`/PortalImagenologia/dicomweb/`): marcar como "a confirmar contra servidor real".
7. Persistencia de auditoría en BD (hoy solo ILogger): decidir SP/tabla o diferir con justificación.
8. Ubicación exacta del punto de integración UI dentro de Portal Web Radiólogos.
9. **Elección de visor** (OHIF completo vs Cornerstone vs Weasis u otro): decisión de STRANGE, argumentada, considerando que dcm4chee 2.x NO expone DICOMweb moderno y que "Orthanc-como-gateway" es propuesta NO validada contra el servidor real, no un hecho cerrado.

## 4. Criterios de aceptación por entregable (9 tareas)
1. **Arquitectura**: diagrama + justificación; datos reales del PACS; decisión de gateway razonada.
2. **Flujo integración** App↔IIS↔PACS↔Visor↔BD: secuencia extremo a extremo trazable.
3. **Comparativa** OHIF/Cornerstone/Weasis/otras: tabla ventajas/desventajas/complejidad/costo/licencia/rendimiento/integración + recomendación final argumentada.
4. **Flujo búsqueda** por (a) Caso/Cuenta y (b) Identificación hasta ver estudio: pasos + resolución a StudyInstanceUID vía QIDO.
5. **Conversión vs nativo**: decisión (WADO-URI JPEG vs DICOMweb+Cornerstone nativo) con trade-off calidad diagnóstica vs peso.
6. **Diseño funcional**: cubre mínimos (Zoom, Pan, Window Level, Medición, Rotación, Invertir, Series, navegación, MPR si aplica, comparación, descarga, impresión) con estado por-función.
7. **APIs REST**: contratos (verbo, ruta, payload, respuesta, códigos) del broker .NET 8.
8. **Seguridad**: HTTPS, auth cookie existente + roles (Admin/Radiologo), token corto JWT ~10min, autorización por caso/identificación, protección de imágenes, auditoría/trazabilidad.
9. **GitHub + PDF**: commits incrementales (no monolítico) sin secretos/PHI; Guía PDF paso a paso con carpetas/archivos implicados.

## 5. Restricciones DURAS (textuales, no relajar)
- NO modificar código operativo/funcional del repo real; todo lo nuevo en módulos/carpetas propios del visor.
- Usar SIEMPRE datos reales del PACS: dcm4chee AE=`DCM4CHEE` DIMSE `11112` hosts `172.16.10.100`/`172.16.50.100` WADO-URI `:8080/wado`; Orthanc AE=`ESCULAPIO_ORTHANC` DIMSE `192.168.2.17:4242` REST/DICOMweb `:8042`. Nunca placeholders tipo `PACS_SERVER` o `wadoUrl=http://172.16.10`.
- `TokenSecret` y credenciales NUNCA al repo (User-Secrets / variables de entorno).
- `ActualizacionCodigo/` es solo REFERENCIA local: nunca subir a GitHub; ningún `.har` ni PHI se sube (ya en .gitignore). Leerla como inspiración, no como código correcto.
- Repo se actualiza en GitHub durante el proceso (commits incrementales) y se notifica avance por Telegram.
- Entregable final incluye Guía PDF paso a paso de instalación/configuración/implementación.
- PLAN-002 autocontenido: NO decir "continuar desde PLAN-001".

## 6. Fases sugeridas (STRANGE las convierte en plan; NO son arquitectura)
F0 Precondiciones/datos por confirmar (Orthanc desplegado, red IIS→PACS, credenciales, retención caché). F1 Decisión de arquitectura + visor (cierra ambigüedad 9). F2 Broker .NET 8 (resolución+token+auditoría+APIs). F3 Integración UI en Portal Web Radiólogos. F4 Seguridad. F5 Guía PDF + cierre GitHub.

> XAVIER no decide arquitectura ni OHIF-vs-Cornerstone-vs-Weasis: eso es de DOCTOR STRANGE en PLAN-002.
