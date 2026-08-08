# Guía de instalación — Visor diagnóstico MedDream (Portal Imagenología)

**Producto:** PortalImagenologia (.NET 8)  
**Visor clínico:** MedDream (ADR-002)  
**Gateway DICOM:** Orthanc (`ESCULAPIOORTHANC`) → PACS dcm4chee (`DCM4CHEE`)  
**Ámbito UI:** Portal Web Radiólogos — botón **Ver Imágenes**

> Esta guía no contiene secretos ni PHI. Use placeholders `<...>` para credenciales.

---

## 1. Arquitectura

```
Radiólogo → Portal .NET 8 (broker)
         → Resolver (QIDO Orthanc) / Token / Auditoría
         → MedDream (?token=...)
         → Storage Orthanc / PACS
Orthanc ← C-FIND/C-MOVE → dcm4chee 2.x
```

- **No** se usa OHIF ni Stone como visor de diagnóstico.
- Stone puede seguir instalado solo como prueba de humo de DICOMweb.

## 2. Prerrequisitos

| Componente | Notas |
|---|---|
| Windows Server 2012 R2 + IIS | App en `/PortalImagenologia` |
| .NET 8 Hosting Bundle | Publicación actual |
| Orthanc 1.12.x + DICOMweb | AE `ESCULAPIOORTHANC` (≤16 chars) |
| dcm4chee 2.x | AE `DCM4CHEE`, DIMSE `11112` |
| Licencia MedDream diagnóstica | Softneta Premium/Enterprise (o equivalente) |
| Java 8+ | TokenService MedDream (si aplica) |

## 3. Orthanc (resumen)

1. Servicio Orthanc con SQLite/DB en ruta válida (p.ej. `C:\Orthanc`).
2. Plugin DICOMweb activo. Ruta nativa: **`/dicom-web`** (con guion).
3. `DicomModalities.pacs` → host PACS real (`172.16.10.100:11112`, AET `DCM4CHEE`).
4. Registrar `ESCULAPIOORTHANC` en dcm4chee como destino C-MOVE.
5. Proxy IIS opcional hacia `/PortalImagenologia/dicomweb` (mapear a Orthanc `/dicom-web/`).
6. Credenciales Orthanc **fuera del repo** (`Visor:OrthancUser` / `Visor:OrthancPassword`).

## 4. MedDream

1. Instalar MedDream Application Server (manual Softneta).
2. Configurar storage apuntando a Orthanc (DICOMweb/WADO) o conector PACS soportado.
3. Instalar **TokenService** (`POST /v4/generate`, `GET /v4/validate`).
4. Modo **token enabled** (obligatorio en producción).
5. Exponer visor bajo HTTPS (mismo host o sub-app IIS).

## 5. Configuración .NET 8 (`appsettings` + secrets)

Sección `Visor` (valores no secretos en repo):

```json
"Visor": {
  "OrthancRestBaseUrl": "http://localhost:8042",
  "OrthancDicomWebBaseUrl": "http://localhost:8042/dicom-web",
  "OrthancAet": "ESCULAPIOORTHANC",
  "OrthancPacsModality": "pacs",
  "TokenMinutos": 10,
  "ViewerBasePath": "/PortalImagenologia/Visor",
  "MedDreamEnabled": true,
  "MedDreamViewerBaseUrl": "https://<host>/meddream",
  "MedDreamTokenServiceBaseUrl": "http://localhost:8080",
  "MedDreamTokenApiVersion": "v4",
  "MedDreamStorageId": "Orthanc",
  "MedDreamAllowStudyQueryString": false
}
```

User-Secrets / variables de entorno IIS:

- `Visor:TokenSecret`
- `Visor:OrthancUser` / `Visor:OrthancPassword`
- `Visor:MedDreamTokenServiceUser` / `Visor:MedDreamTokenServicePassword` (si TokenService usa Basic)

## 6. Cambios en la aplicación (inventario)

### Nuevos / actualizados (módulo Visor)

| Ruta | Rol |
|---|---|
| `Controllers/VisorController.cs` | Broker + launch MedDream |
| `Services/Visor/DicomWebClient.cs` | QIDO/WADO Orthanc |
| `Services/Visor/OrthancGatewayService.cs` | C-MOVE idempotente |
| `Services/Visor/MedDreamLaunchService.cs` | TokenService Softneta |
| `Views/Visor/Abrir.cshtml` | Puente / mensajes |
| `Views/PortalRadiologos/_VisorBotonPartial.cshtml` | Partial aditiva |
| `wwwroot/js/visor-ver-imagenes.js` | Botón Ver Imágenes |
| `wwwroot/css/visor.css` | Estilos aditivos |
| `.swarm/adrs/ADR-002-meddream-diagnostico.md` | Decisión MedDream |

### Cambio mínimo operativo

- `Views/PortalRadiologos/Index.cshtml` — **1 línea** en Scripts: partial del botón.
- `Program.cs` — ya tenía `AddVisorModule()` (sin cambio adicional requerido).

## 7. Flujo de uso

1. Radiólogo abre Portal Web Radiólogos.
2. Pulsa **Ver Imágenes** en una fila (`data-no-cuenta`).
3. `GET /Visor/Resolver?caso={NoCuenta}` → QIDO AccessionNumber (+ fallback PatientID).
4. `POST /Visor/Token` → C-MOVE best-effort + auditoría + URL MedDream.
5. Se abre MedDream con `?token=...`.

## 8. Pruebas

- [ ] QIDO Orthanc por AccessionNumber con cuenta de prueba.
- [ ] C-MOVE trae estudio a Orthanc.
- [ ] TokenService `POST /v4/generate` responde token.
- [ ] Botón abre MedDream con el estudio correcto.
- [ ] Token inválido/expirado → vista TokenInvalido / rechazo.
- [ ] Resto del portal (audio, seleccionar) sin regresión.

## 9. Troubleshooting

| Síntoma | Causa probable |
|---|---|
| Resolver 404 | Caso ≠ AccessionNumber/PatientID en PACS |
| QIDO vacío | URL con `dicomweb` sin guion; usar `dicom-web` |
| C-MOVE falla | AET no registrado / firewall PACS→Orthanc:4242 |
| MedDream sin imágenes | StorageId incorrecto o estudio no en Orthanc |
| 400 en POST Token | Falta header `RequestVerificationToken` |

## 10. Rollback

1. `MedDreamEnabled=false` en configuración.
2. Quitar la línea partial de `Index.cshtml` si se debe ocultar el botón.
3. Orthanc/Stone pueden permanecer; no afectan al portal sin el botón.

## 11. Referencias

- ADR-002, SPEC-002/003/004, PLAN-002 (enmienda MedDream)
- `docs/PACS-exposicion.md`
- Manual de integración MedDream (Softneta)
