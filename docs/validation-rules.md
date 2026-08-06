# validation-rules.md — Reglas de Validación por Fase
# Plataforma webImagenologia · Esculapio

> Los gates definidos aquí son evaluados por `agentic/orchestrator.py`
> al finalizar cada fase. Un sub-agente reporta PASS **solo cuando todos
> los gates aplicables están en verde**.

---

## Gates Universales (aplican a TODAS las fases)

| Gate ID | Comando / Verificación | Criterio PASS |
|---------|------------------------|---------------|
| `build` | `dotnet build src/WebImagenologia.sln` | exit code 0, 0 errores |
| `secrets` | Regex en `src/`: `(?i)(password\|pwd\|apikey\|api_key)\s*=\s*"[^"]{4,}"` | 0 coincidencias |
| `no-sql-inline` | Regex en `src/**/Controllers/`: `(?i)(SELECT\|INSERT\|UPDATE\|DELETE\|FROM\s+\w+)` | 0 coincidencias |
| `no-direct-http` | `new HttpClient()` en archivos distintos de `EsculapioApiClient.cs` | 0 coincidencias |

---

## Gates de Código (fases con archivos .cs nuevos o modificados)

| Gate ID | Comando / Verificación | Criterio PASS |
|---------|------------------------|---------------|
| `tests` | `dotnet test src/WebImagenologia.Tests/ --no-build` | 0 failed, 0 skipped |
| `format` | `dotnet format --verify-no-changes src/WebImagenologia.sln` | exit code 0 |
| `lint-cshtml` | Regex en `src/**/Views/`: `@\{[^}]*await\s` | 0 coincidencias |
| `api-client-unicity` | `new HttpClient()` solo en `EsculapioApiClient.cs` | verificado |
| `endpoints-cubiertos` | Cada endpoint de `docs/specs.md` tiene método en `IEsculapioApiClient` | verificado |
| `authorize-attrs` | Rutas de admin tienen `[Authorize(Roles = "Administrador")]` | verificado |
| `accessibility` | Todo `<input>` con `<label>` o `aria-label` en vistas | verificado |

---

## Gates por Fase

### Fase 01 — Scaffold
| Gate | Criterio |
|------|---------|
| `build` | Solución compila desde cero |
| `tests` | Mínimo 2 tests PASS (startup + redirect a login) |
| `di-config` | `EsculapioApiClient` registrado en Program.cs con `AddHttpClient` |

### Fase 02 — Login
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `tests` | Login mock PASS + Logout PASS + ObtenerServidores PASS |
| `session-encrypted` | ConnectionString en sesión cifrada (no en cookie en claro) |
| `endpoints-cubiertos` | `ObtenerServidoresAsync`, `ValidarConexionAsync` |

### Fase 03 — Radiólogos
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `authorize-attrs` | `[Authorize(Roles = "Administrador")]` en ParametrosController |
| `lint-cshtml` | ok |

### Fase 04 — Operadores
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `authorize-attrs` | ok |

### Fase 05 — Estudios
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `ajax-endpoint` | `GET /Parametros/Estudios/ServiciosPorDependencia` retorna JSON válido |

### Fase 06 — Asignación
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `ajax-endpoint` | `GET /Condicional/Asignacion/MedicosPorEmpresa` retorna JSON válido |

### Fase 07 — Automatización
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `tests` | Test de invocación webhook N8N (mock) PASS |

### Fase 08 — Portal Radiólogos
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `tests` | Audio upload con mime válido PASS + con mime inválido rechazado PASS |
| `authorize-attrs` | `[Authorize(Roles = "Radiologo")]` en PortalRadiologosController |
| `audio-size` | Archivo > 25 MB es rechazado con error claro |
| `audio-mime` | Solo se aceptan: audio/mpeg, audio/wav, audio/ogg, audio/mp4, audio/x-m4a |
| `blocked-if-no-upload-endpoint` | Si el endpoint de upload no existe → BLOCKED (no FAIL) |

### Fase 09 — Lecturas
| Gate | Criterio |
|------|---------|
| `build` | ok |
| `authorize-attrs` | `[Authorize(Roles = "Administrador,Operador")]` |

### Fase 10 — Reportes
| Gate | Criterio |
|------|---------|
| `build` | ok (incluye ClosedXML en .csproj) |
| `excel-export` | Endpoint `GET /Reportes/ExportarExcel` retorna `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |

### Fase 11 — N8N
| Gate | Criterio |
|------|---------|
| `json-n8n` | `n8n/workflows/programacion-estudios.json` parseable con `json.loads` sin error |
| `sp-syntax` | SPs en `db/stored_procedures/` sin errores de sintaxis MySQL |

### Fase 12 — QA Release
| Gate | Criterio |
|------|---------|
| Todos los anteriores | En PASS |
| `regression-tests` | Suite de regresión completa verde |
| `readme-deploy` | `README.md` actualizado con instrucciones de deploy |

---

## Comandos de validación automática

El orquestador ejecuta estos comandos antes de evaluar el reporte YAML:

```powershell
# Build
dotnet build src\WebImagenologia.sln --no-restore -v quiet

# Tests
dotnet test src\WebImagenologia.Tests\ --no-build --logger "trx;LogFileName=results.trx"

# Format check
dotnet format --verify-no-changes src\WebImagenologia.sln

# Secrets check (PowerShell)
$files = Get-ChildItem src -Recurse -Include *.cs,*.json,*.cshtml
$found = $files | Select-String -Pattern '(?i)(password|pwd|apikey)\s*=\s*"[^"]{4,}"'
if ($found) { Write-Error "Secrets encontrados en código"; exit 1 }

# HttpClient unicity check
$violations = Get-ChildItem src -Recurse -Include *.cs |
  Where-Object { $_.Name -ne "EsculapioApiClient.cs" } |
  Select-String -Pattern "new HttpClient\(\)"
if ($violations) { Write-Error "HttpClient instanciado fuera de EsculapioApiClient"; exit 1 }
```
