# F0 — Validación de entorno (Orthanc / PACS) — Resultado

- **Spec:** SPEC-001-precondiciones-entorno.md
- **Fecha:** 2026-08-07
- **Ejecutor de este intento:** IRON MAN (orquestador), desde el entorno de desarrollo/sandbox de este proyecto.
- **Resultado global:** **NO EJECUTABLE desde este entorno.** Requiere re-ejecución por QUICKSILVER o el Lead desde una máquina con ruta de red real a la intranet clínica.

## Motivo

Este entorno de desarrollo no tiene ruta de red hacia los hosts de producción del PACS/Orthanc.
Prueba de conectividad TCP (sin credenciales, sin tocar datos clínicos):

| Destino | Verificación | Resultado |
|---|---|---|
| `192.168.2.17:8042` (Orthanc REST/DICOMweb) | conexión TCP | **NO ALCANZABLE** |
| `192.168.2.17:4242` (Orthanc DIMSE) | conexión TCP | **NO ALCANZABLE** |
| `172.16.10.100:11112` (dcm4chee CAMPBELL/FUNDACION) | conexión TCP | **NO ALCANZABLE** |
| `172.16.50.100:11112` (dcm4chee SANTAMARTA) | conexión TCP | **NO ALCANZABLE** |

Consistente con lo esperado: son direcciones de la red interna/clínica (`172.16.x`, `192.168.2.x`), no accesibles desde este sandbox de desarrollo.

## Estado de las 6 verificaciones de SPEC-001

| # | Verificación | Estado |
|---|---|---|
| V1 | Orthanc desplegado + plugin DICOMweb en `192.168.2.17:8042` | **PENDIENTE** — requiere ejecución desde red real |
| V2 | Ruta de red bidireccional IIS↔Orthanc y **PACS→Orthanc:4242** | **PENDIENTE** |
| V3 | Credenciales de Orthanc de producción disponibles fuera del repo | **PENDIENTE** |
| V4 | Política de retención/caché de Orthanc | **PENDIENTE** |
| V5 | AET `ESCULAPIO_ORTHANC` registrado en dcm4chee como destino C-MOVE | **PENDIENTE** |
| V6 | Prueba de humo con Stone Web Viewer sobre estudio real de test | **PENDIENTE** |

## Decisión A/B (provisional)

**No se puede tomar la decisión A/B todavía** — ver SPEC-001 §6. Sin evidencia real, **no se compromete implementación de F2/F3 contra el Orthanc real** (regla dura de SPEC-001).

**Vía de avance sin bloquear el resto del plan:** F1 (SPEC-002, broker de seguridad) se implementa contra **interfaces + mocks**, tal como prevé SPEC-002 §2. F2/F3 se implementan con el mismo enfoque de interfaces, dejando la implementación real de `DicomWebClient`/`OrthancGatewayService` lista para validar en cuanto alguien con acceso a la red del cliente (QUICKSILVER o el Lead) ejecute las verificaciones V1–V6 de este documento.

## Siguiente paso

1. Alguien con acceso a la red `172.16.x` / `192.168.2.x` (QUICKSILVER o el Lead) debe ejecutar los comandos de SPEC-001 §4 (`curl`, `Test-NetConnection`) y completar la tabla de arriba con evidencia real.
2. Actualizar este documento con los resultados y firmar la **Decisión A/B**.
3. Si el resultado es **A** (Orthanc/OHIF viable): continuar SPEC-003/SPEC-004 contra el entorno real.
4. Si el resultado es **B** (Plan B): rediseñar SPEC-003/SPEC-004 sobre WADO-URI JPEG de dcm4chee, según ADR-001 §5.
