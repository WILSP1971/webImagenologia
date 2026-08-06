# agents.md — Catálogo de Agentes
# Plataforma webImagenologia · Esculapio

> Referencia completa de todos los agentes del pipeline agentico.
> Ver también `AGENTS.md` en la raíz para el contrato global.

---

## Modelo de ejecución

```
Lead (humano)
  └── orchestrator.py  [Python, Cursor SDK — NO es agente Cursor]
        ├── Phase 01: Scaffolder       [Sub-agente Cursor local]
        ├── Phase 02: AuthAgent        [Sub-agente Cursor local]
        ├── Phase 03: ParamsRadAgent   [Sub-agente Cursor local]
        ├── ...
        └── Phase 12: QAAgent         [Sub-agente Cursor local]
```

Todos los sub-agentes corren en **runtime local** (`cwd = raíz del repositorio`),
con modelo `composer-2.5`.

---

## Agente 00 — Orchestrator

| Atributo | Valor |
|----------|-------|
| Tipo | Python script (cursor-sdk) |
| Archivo | `agentic/orchestrator.py` |
| Runtime | Local (ejecutado por el Lead con `python orchestrator.py`) |
| Modelo | No aplica (es el script Python, no un agente Cursor) |
| Rol | Coordinar, parsear reportes YAML, evaluar gates, notificar al Lead |
| Puede tocar | `agentic/reports/` |
| No puede tocar | `src/`, `docs/`, `.cursor/rules/` |

---

## Agente 01 — Scaffolder

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_01_scaffold.md` |
| Modelo | composer-2.5 |
| Rol | Crear solución .NET 8, estructura de carpetas, Bootstrap 5, DI base |
| Archivos permitidos | `src/` (todo), `.editorconfig`, `.gitignore` |
| Archivos prohibidos | `agentic/`, `docs/`, `.cursor/rules/` |
| Gate de salida | `dotnet build` exit 0 + 2 tests PASS |

---

## Agente 02 — AuthAgent + ApiClientAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_02_auth_login.md` |
| Rol | Login + sesión cifrada + `EsculapioApiClient` completo |
| Archivos permitidos | `src/Controllers/AccountController.cs`, `src/Services/`, `src/Models/ApiDtos/`, `src/Views/Account/` |
| Gate de salida | build verde + login mock PASS + `EsculapioApiClient` tiene 5 métodos |

---

## Agente 03 — ParamsRadiologosAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_03_param_radiologos.md` |
| Rol | Vista Radiólogos por Empresa — formulario + grid CRUD |
| Archivos permitidos | `src/Controllers/ParametrosController.cs`, `src/Views/Parametros/Radiologos*`, `src/Models/ViewModels/RadiologosViewModel.cs`, `src/Models/ApiDtos/Medico*.cs` |
| Gate de salida | build verde |

---

## Agente 04 — ParamsOperadoresAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_04_param_operadores.md` |
| Rol | Vista Operadores por Empresa — mismo patrón que Radiólogos |
| Archivos permitidos | `src/Controllers/ParametrosController.cs`, `src/Views/Parametros/Operadores*`, `src/Models/ViewModels/OperadoresViewModel.cs` |
| Gate de salida | build verde |

---

## Agente 05 — ParamsEstudiosAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_05_param_estudios.md` |
| Rol | Parametrización de cantidad de estudios por dependencia/servicio/empresa |
| Archivos permitidos | `src/Controllers/ParametrosController.cs`, `src/Views/Parametros/Estudios*`, `src/Models/ViewModels/EstudiosViewModel.cs`, `src/wwwroot/js/parametros.js` |
| Gate de salida | build verde + AJAX de servicios funcional |

---

## Agente 06 — CondAsignacionAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_06_cond_asignacion.md` |
| Rol | Asignación de No. de Estudios por Empresa a médicos |
| Archivos permitidos | `src/Controllers/CondicionalController.cs`, `src/Views/Condicional/Asignacion*` |
| Gate de salida | build verde |

---

## Agente 07 — CondAutomatizacionAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_07_cond_automatizacion.md` |
| Rol | CRUD automatizacionwf + disparo de webhook N8N |
| Archivos permitidos | `src/Controllers/CondicionalController.cs`, `src/Views/Condicional/Automatizacion*`, `src/Models/ViewModels/AutomatizacionViewModel.cs` |
| Gate de salida | build verde + webhook mock test PASS |

---

## Agente 08 — PortalRadiologosAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_08_portal_radiologos.md` |
| Rol | Portal médico: grid estudios, panel diagnóstico, audio upload/play |
| Archivos permitidos | `src/Controllers/PortalRadiologosController.cs`, `src/Views/PortalRadiologos/`, `src/wwwroot/js/portalRadiologos.js` |
| Gate de salida | build verde + audio upload test (mime + tamaño) PASS |
| Condición BLOCKED | Si el endpoint de upload no existe en la API externa |

---

## Agente 09 — PortalLecturasAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_09_portal_lecturas.md` |
| Rol | Portal de lecturas y resultados para operadores/admin |
| Archivos permitidos | `src/Controllers/LecturasController.cs`, `src/Views/Lecturas/` |
| Gate de salida | build verde |

---

## Agente 10 — ReportesAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_10_reportes.md` |
| Rol | Consultas y reportes con exportación a Excel (ClosedXML) |
| Archivos permitidos | `src/Controllers/ReportesController.cs`, `src/Views/Reportes/` |
| Gate de salida | build verde |

---

## Agente 11 — N8NAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_11_n8n_workflow.md` |
| Rol | Generar workflow N8N exportable + stubs de SPs MySQL |
| Archivos permitidos | `n8n/workflows/`, `db/stored_procedures/` |
| Archivos prohibidos | `src/` |
| Gate de salida | JSON N8N válido |

---

## Agente 12 — QAAgent

| Atributo | Valor |
|----------|-------|
| Prompt | `agentic/prompts/phase_12_qa_release.md` |
| Rol | Gates globales + README de deploy + tests de regresión |
| Archivos permitidos | `README.md`, `docs/`, `src/WebImagenologia.Tests/` |
| Gate de salida | TODOS los gates de `docs/validation-rules.md` en PASS |
