# AGENTS.md — Contrato Global de Agentes
# Plataforma Web Imagenología Esculapio · webImagenologia

> Este archivo es leído por Cursor, el orquestador (`agentic/orchestrator.py`) y por
> cada sub-agente antes de ejecutar cualquier tarea. Define roles, permisos de archivo,
> modelo sugerido y gate de salida por agente. NUNCA modificar sin aprobación del Lead.

---

## Modelo por defecto

```
model: composer-2.5
runtime: local
cwd: <raíz del repositorio webImagenologia>
```

---

## Reglas universales (aplican a TODOS los agentes)

1. Leer `docs/specs.md` antes de editar cualquier vista o controlador.
2. Leer `docs/validation-rules.md` y asegurarse de cumplir los gates antes de reportar PASS.
3. Nunca escribir credenciales, passwords ni connection strings en código fuente.
4. Toda llamada HTTP a la API Esculapio debe realizarse **únicamente** a través de
   `src/WebImagenologia.Web/Services/EsculapioApiClient.cs`.
5. Commits en formato Conventional Commits (`feat:`, `fix:`, `chore:`, etc.).
6. Siempre terminar el turno emitiendo el bloque YAML de reporte definido en
   `agentic/prompts/meta_prompt_costar.md § [R]`.

---

## Catálogo de Agentes

### Orchestrator
- **Tipo**: Python script (Cursor SDK) — no es un sub-agente Cursor
- **Archivo**: `agentic/orchestrator.py`
- **Rol**: Invocar sub-agentes por fase, parsear reportes YAML, evaluar gates,
  notificar al Lead, detener pipeline en FAIL/BLOCKED.
- **Puede tocar**: `agentic/reports/`, `agentic/phases.py` (solo lectura en runtime)
- **No puede tocar**: `src/`, `docs/`, `.cursor/`

---

### Scaffolder (Fase 01)
- **Prompt**: `agentic/prompts/phase_01_scaffold.md`
- **Rol**: Crear la solución .NET 8, proyectos Web + Tests, `Program.cs` base,
  layout Bootstrap 5, DI de `HttpClient`/`EsculapioApiClient`.
- **Puede tocar**: `src/` (todo), `db/` (solo lectura), `.editorconfig`, `.gitignore`
- **No puede tocar**: `agentic/`, `docs/`, `.cursor/rules/`
- **Gate de salida**: `dotnet build` exit 0

---

### AuthAgent (Fase 02)
- **Prompt**: `agentic/prompts/phase_02_auth_login.md`
- **Rol**: Login con dropdown de servidores (API `obtener-servidores`), validación
  de credenciales (API `obtener-validaconexion`), sesión cifrada con DataProtection.
- **Puede tocar**: `src/WebImagenologia.Web/Controllers/AccountController.cs`,
  `src/WebImagenologia.Web/Views/Account/`, `src/WebImagenologia.Web/Models/ViewModels/`,
  `src/WebImagenologia.Web/Services/`
- **Gate de salida**: build verde + test de login mock PASS

---

### ApiClientAgent (Fase 02, apoyo a todas las fases)
- **Prompt**: parte de `agentic/prompts/phase_02_auth_login.md`
- **Rol**: Implementar `IEsculapioApiClient` con todos los endpoints del spec.
  Es la **única clase** autorizada para instanciar `HttpClient`.
- **Puede tocar**: `src/WebImagenologia.Web/Services/EsculapioApiClient.cs`,
  `src/WebImagenologia.Web/Models/ApiDtos/`
- **Gate de salida**: cada endpoint del `docs/specs.md` tiene un método correspondiente

---

### ParamsRadiologosAgent (Fase 03)
- **Prompt**: `agentic/prompts/phase_03_param_radiologos.md`
- **Rol**: Vista Radiólogos por Empresa — dropdown médicos, textboxes, multicheckbox
  empresas, grid con editar/eliminar.
- **Puede tocar**: `src/.../Controllers/ParametrosController.cs`,
  `src/.../Views/Parametros/Radiologos.*`, `src/.../Models/ViewModels/RadiologosViewModel.cs`
- **Gate de salida**: build verde + UI renderiza sin errores de servidor

---

### ParamsOperadoresAgent (Fase 04)
- **Prompt**: `agentic/prompts/phase_04_param_operadores.md`
- **Rol**: Vista Operadores por Empresa — misma estructura que Radiólogos.
- **Puede tocar**: mismo scope que Fase 03 pero archivos `Operadores*`
- **Gate de salida**: build verde

---

### ParamsEstudiosAgent (Fase 05)
- **Prompt**: `agentic/prompts/phase_05_param_estudios.md`
- **Rol**: Parametrización de Estudios — dropdown dependencias/servicios,
  cantidad, checkbox Lectura, multicheckbox empresas, grid.
- **Puede tocar**: `Controllers/ParametrosController.cs`, `Views/Parametros/Estudios.*`,
  `Models/ViewModels/EstudiosViewModel.cs`
- **Gate de salida**: build verde + CRUD via API funcional

---

### CondAsignacionAgent (Fase 06)
- **Prompt**: `agentic/prompts/phase_06_cond_asignacion.md`
- **Rol**: Asignación de No. de Estudios por Empresa (tabla `estudiosdiagnosticos_empresa`).
- **Puede tocar**: `Controllers/CondicionalController.cs`, `Views/Condicional/Asignacion.*`
- **Gate de salida**: build verde

---

### CondAutomatizacionAgent (Fase 07)
- **Prompt**: `agentic/prompts/phase_07_cond_automatizacion.md`
- **Rol**: CRUD de `estudiosdiagnosticos_automatizacionwf`, UI de programación
  (checkbox Radiologo/Operador, frecuencia, hora), disparo al webhook N8N.
- **Puede tocar**: `Controllers/CondicionalController.cs`,
  `Views/Condicional/Automatizacion.*`, `Models/ViewModels/AutomatizacionViewModel.cs`
- **Gate de salida**: build verde + webhook N8N invocado correctamente (mock test)

---

### PortalRadiologosAgent (Fase 08)
- **Prompt**: `agentic/prompts/phase_08_portal_radiologos.md`
- **Rol**: Portal médico — dropdown empresas, grid de programación, panel diagnóstico,
  notas médicas, upload/play/eliminar audio (BLOB).
- **Puede tocar**: `Controllers/PortalRadiologosController.cs`,
  `Views/PortalRadiologos/`, `Models/ViewModels/PortalRadiologosViewModel.cs`,
  `wwwroot/js/portalRadiologos.js`
- **Gate de salida**: build verde + audio upload mock test PASS

---

### PortalLecturasAgent (Fase 09)
- **Prompt**: `agentic/prompts/phase_09_portal_lecturas.md`
- **Rol**: Portal de lecturas/resultados de estudios.
- **Puede tocar**: `Controllers/LecturasController.cs`, `Views/Lecturas/`
- **Gate de salida**: build verde

---

### ReportesAgent (Fase 10)
- **Prompt**: `agentic/prompts/phase_10_reportes.md`
- **Rol**: Consultas y reportes por empresa, radiólogo, fecha.
- **Puede tocar**: `Controllers/ReportesController.cs`, `Views/Reportes/`
- **Gate de salida**: build verde

---

### N8NAgent (Fase 11)
- **Prompt**: `agentic/prompts/phase_11_n8n_workflow.md`
- **Rol**: Generar `n8n/workflows/programacion-estudios.json` con trigger cron,
  nodo MySQL para `ConsOrdenesResultados` y `Get_ProgramacionEstudiosDiagnosticos`.
- **Puede tocar**: `n8n/workflows/`, `db/stored_procedures/`
- **No puede tocar**: `src/`
- **Gate de salida**: JSON válido importable en N8N

---

### QAAgent (Fase 12)
- **Prompt**: `agentic/prompts/phase_12_qa_release.md`
- **Rol**: Ejecutar todos los gates globales, generar `README.md` final,
  notas de deploy.
- **Puede tocar**: `README.md`, `docs/`, `src/WebImagenologia.Tests/`
- **Gate de salida**: todos los gates de `docs/validation-rules.md` en PASS

---

## Protocolo de comunicación Orchestrator → Lead

```
NIVEL       ACCIÓN DEL ORQUESTADOR
info        Imprime en consola (verde) + escribe en reports/
start       Imprime banner de inicio de fase
success     Imprime PASS + artifacts + next_phase
warn        Imprime advertencia, continúa el pipeline
error       Imprime FAIL, detiene pipeline, espera input del Lead
needs_input Detiene pipeline, muestra prompt interactivo al Lead
```

El Lead responde vía consola (`input()`) o vía webhook HTTP (`LEAD_WEBHOOK_URL`).
