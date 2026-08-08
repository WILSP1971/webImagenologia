# ADR-001 — Orthanc como gateway DICOMweb + OHIF v3 como visor

- **Estado:** **Superseded parcialmente por ADR-002 (2026-08-08)** — Orthanc-gateway vigente; elección de visor OHIF reemplazada por MedDream.
- **Fecha:** 2026-08-07
- **Autor:** DOCTOR STRANGE (Lead de Arquitectura — Avengers Swarm)
- **Ámbito:** Módulo Visor de imágenes DICOM diagnóstico de `PortalImagenologia` (.NET 8 / IIS / Windows Server 2012 R2).
- **Specs relacionadas:** SPEC-001 (condición dura), SPEC-002, SPEC-003, SPEC-004, SPEC-005.

---

## 1. Contexto

El PACS clínico es **dcm4chee** (backends `172.16.10.100` CAMPBELL/FUNDACION y `172.16.50.100` SANTAMARTA, AE `DCM4CHEE`, DIMSE `11112`). Datos reales confirmados por captura HAR de Oviyam en producción (`docs/PACS-exposicion.md`), con prioridad sobre cualquier placeholder.

**Restricción central:** dcm4chee solo expone **WADO-URI clásico** (`:8080/wado`, imagen JPEG por objeto) y **DIMSE**. **No expone DICOMweb REST moderno** (QIDO-RS / WADO-RS / STOW-RS). Un visor diagnóstico de nueva generación (OHIF v3, Cornerstone3D) **requiere DICOMweb**. Por tanto, la elección de visor y la de estrategia de acceso al PACS están **acopladas**.

Existe ya en la red un nodo **Orthanc** (`192.168.2.17`, AE `ESCULAPIO_ORTHANC`, DIMSE `4242`, REST/DICOMweb `8042`), lo que habilita una estrategia de gateway. El visor debe integrarse **mismo-origen** bajo `/PortalImagenologia/`, sin exponer el PACS ni Orthanc al navegador, con broker de seguridad .NET 8 (token corto, autorización por caso, auditoría) y sin tocar código operativo.

## 2. Decisión

1. **Visor: OHIF Viewer v3 (licencia MIT)**, empotrado como SPA estática servida mismo-origen bajo `/PortalImagenologia/visor/`, apuntando a una fuente **DICOMweb**.
2. **Acceso al PACS: Orthanc como gateway DICOMweb.** Orthanc hace **C-FIND** (búsqueda) y **C-MOVE bajo demanda** (fetch-on-demand + cache) contra dcm4chee, y **re-expone DICOMweb** (QIDO-RS/WADO-RS) que consume OHIF. OHIF **nunca** habla directo con dcm4chee.
3. **Condicionada a F0 (SPEC-001):** la estrategia **no se compromete en implementación** hasta confirmar, con evidencia real: Orthanc desplegado + plugin DICOMweb, ruta de red **PACS→Orthanc:4242** (C-STORE de vuelta del C-MOVE), credenciales, política de retención, AET receptor registrado en dcm4chee, y prueba de humo con Stone Web Viewer.
4. **Mismo-origen vía reverse proxy IIS (ARR + URL Rewrite)** en `/PortalImagenologia/dicomweb/` (evita CORS); Orthanc en `RemoteAccessAllowed=false` (solo localhost); proxy de **solo lectura**.

## 3. Alternativas consideradas (y por qué se descartaron)

- **Cornerstone3D embebido a medida (MIT):** control total, pero exige construir toolbar/MPR/comparación desde cero (alto costo y tiempo). **Descartada como base principal**; se conserva como *fallback conceptual* dado que OHIF ya usa Cornerstone3D por debajo (no se pierde capacidad).
- **Weasis (EPL/GPL):** aplicación de escritorio JNLP; requiere Java en el cliente, mala integración web embebida y fricción de licencia/distribución. **Descartada** para el flujo embebido.
- **Stone Web Viewer (atado a Orthanc, GPLv3/plugins):** integración muy baja, pero herramientas básicas-medias y acoplado a Orthanc. **No** como visor principal; **sí** como **prueba de humo** en F0/F2 para validar la cadena Orthanc→DICOMweb antes de invertir en el build de OHIF.
- **Acceso directo WADO-URI JPEG de dcm4chee sin gateway:** no habilita un visor diagnóstico moderno (sin window-level dinámico real ni MPR). **Descartada como camino principal**; se conserva como **Plan B** de contingencia (ver §5).
- **Exponer dcm4chee/Orthanc directamente al navegador:** inaceptable por seguridad (PHI, superficie de ataque). **Descartada**; se impone broker .NET + proxy solo-lectura mismo-origen.

## 4. Consecuencias

### Positivas
- Máxima calidad diagnóstica (2D/MPR/series/herramientas completas) con el **menor esfuerzo** de integración; OHIF es MIT (sin fricción comercial).
- SPA estática detrás de reverse proxy: encaja bien en IIS/WS2012R2 sin depender del SO del servidor.
- Mismo-origen elimina CORS; el navegador nunca toca el PACS; superficie de exposición mínima.
- Patrón fetch-on-demand + cache razonable para estudios pesados (TC/RM).

### Negativas / costos
- Introduce un **componente de infraestructura nuevo** (Orthanc) que debe desplegarse, configurarse y mantenerse (retención/caché, credenciales).
- Depende de una **ruta de red bidireccional** delicada (PACS→Orthanc:4242 para el C-STORE de vuelta): principal punto de fallo, verificado en F0.
- Latencia del primer acceso a un estudio (C-MOVE bajo demanda) hasta que esté cacheado; requiere feedback de progreso y revisión de performance (THOR).
- Build de OHIF fuera del repo añade un paso de empaquetado manual (mitigado documentándolo en la Guía).

## 5. Plan B (contingencia explícita)

Si F0 (SPEC-001) falla y Orthanc no puede desplegarse/alcanzarse a tiempo (p.ej. C-MOVE no vuelve por firewall/VLAN), se adopta un **visor 2D de fidelidad reducida sobre WADO-URI JPEG** de dcm4chee (`:8080/wado`), renderizado con un visor ligero. Es inferior (sin window-level dinámico real ni MPR) pero **no bloquea la entrega**. El camino A (Orthanc/OHIF) es el principal; B es contingencia documentada, **no** un diseño paralelo que se construya en simultáneo.

## 6. Cumplimiento y trazabilidad

- Datos del PACS **siempre** desde `docs/PACS-exposicion.md` (HAR real): AET `DCM4CHEE` (nunca `PACS_SERVER`), hosts `172.16.10.100`/`172.16.50.100`, DIMSE `11112`.
- Namespace `WebImagenologia.Web.*`; `ISessionService` real; DI aditiva `AddVisorModule()`; cero modificaciones de código operativo salvo cambios aditivos aprobados.
- Esta decisión se materializa en SPEC-002/003/004/005 y su viabilidad se sella en SPEC-001.
