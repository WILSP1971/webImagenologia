# ADR-002 — MedDream como visor diagnóstico + Orthanc gateway

- **Estado:** **Aceptado** (supersede parcial de ADR-001)
- **Fecha:** 2026-08-08
- **Ámbito:** Módulo Visor diagnóstico de `PortalImagenologia` (.NET 8 / IIS / Windows Server 2012 R2).
- **Specs relacionadas:** SPEC-002, SPEC-003, SPEC-004 (revisadas), SPEC-005, SPEC-006.

---

## 1. Contexto

ADR-001 eligió **OHIF v3 + Orthanc gateway** para calidad diagnóstica open-source. El uso declarado del portal es **diagnóstico clínico primario**. OHIF (MIT) y Stone Web Viewer **no aportan clearance regulatorio** (FDA 510(k) / CE) para uso diagnóstico. MedDream (Softneta) es un visor HTML5 zero-footprint **certificado** para diagnóstico, con API de integración por URL/token diseñada para HIS/RIS/OEM.

Orthanc ya está desplegado en el servidor de aplicación (AE `ESCULAPIOORTHANC`, DICOMweb por HTTPS, Stone como prueba de humo). dcm4chee 2.x sigue siendo el PACS clínico (DIMSE/WADO-URI); Orthanc actúa de gateway.

## 2. Decisión

1. **Visor diagnóstico: MedDream** (licencia Premium/Enterprise u otra con derecho a uso diagnóstico), integrado por **token enabled** (TokenService Softneta o compatible).
2. **Gateway de datos: Orthanc** se mantiene (QIDO-RS/WADO-RS + C-FIND/C-MOVE hacia dcm4chee). MedDream se configura con storage Orthanc/DICOMweb o conector PACS según documentación Softneta.
3. **Broker .NET 8** se mantiene: sesión, `Resolver` (Caso/ID → StudyInstanceUID), token de aplicación, auditoría, botón aditivo en Portal Radiólogos.
4. **OHIF** deja de ser el motor diagnóstico. Stone permanece solo como herramienta de humo/infra, no como visor clínico.
5. **ADR-001** queda **superseded** en la elección de visor (OHIF → MedDream). La parte Orthanc-gateway de ADR-001 **sigue vigente**.

## 3. Flujo

```
Portal Radiólogos (.NET 8)
  → Resolver(caso|identificacion) via QIDO Orthanc
  → (opcional) Orthanc C-MOVE ensure study present
  → Token app + generate MedDream TokenService
  → Abrir MedDream ?token=...
  → MedDream lee storage (Orthanc / PACS)
  → Auditoría ABRIR en .NET
```

## 4. Consecuencias

### Positivas
- Uso diagnóstico alineado con certificación del producto.
- Integración OEM documentada (URL + token / Communication API).
- Reutiliza broker e Orthanc ya trabajados en el repo.

### Costos
- Licencia comercial Softneta y despliegue del Application Server + TokenService.
- Dependencia de contrato de integración Softneta (versión API v4 preferida).

## 5. Alternativas descartadas

| Opción | Motivo de descarte para diagnóstico |
|---|---|
| OHIF v3 | Sin clearance diagnóstico |
| Stone Web Viewer | Solo humo/infra |
| Visor propio C#/Blazor | Coste, codecs, regulación |
| 3D Slicer web | Desktop/investigación, no portal clínico |

## 6. Cumplimiento

- Sin secretos ni PHI en el repo.
- Cambios UI **aditivos** en Portal Radiólogos.
- AET PACS real: `DCM4CHEE` (no placeholder `PACS_SERVER`) según `docs/PACS-exposicion.md`.
- Orthanc AET en servidor: `ESCULAPIOORTHANC` (máx. 16 chars).
