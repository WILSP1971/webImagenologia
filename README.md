# webImagenologia — Plataforma Web Imagenología Esculapio

Plataforma web de gestión de estudios radiológicos para el sistema Esculapio.
Construida con **ASP.NET Core MVC 8**, Bootstrap 5 y orquestada automáticamente
vía **Cursor SDK (Python)** + sub-agentes Cursor.

---

## Stack

| Capa | Tecnología |
|------|-----------|
| Web | ASP.NET Core MVC 8, C#, Razor Pages |
| UI | Bootstrap 5, HTML5, vanilla JS |
| API externa | https://appsintranet.esculapiosis.com/ApiCampbell/api |
| BD | MySQL 5.6 (acceso sólo via API externa) |
| Automatización | N8N (n8n.esculapiosis.com) |
| Orquestación | Python + cursor-sdk |

---

## Módulos

1. **Login** — selección de servidor + validación de credenciales
2. **Parámetros** (Administrador)
   - Radiólogos por Empresa
   - Operadores por Empresa
3. **Condicionales**
   - Asignación de No. de Estudios por Empresa
   - Automatización / Programación de Estudios
4. **Portal Web Radiólogos**
5. **Portal Web Lecturas**
6. **Consultas / Reportes**

---

## Estructura de carpetas

```
webImagenologia/
├── AGENTS.md              # Contrato global de agentes
├── .cursor/rules/         # Reglas persistentes de Cursor
├── agentic/               # Orquestador Python (Cursor SDK)
│   ├── orchestrator.py
│   ├── phases.py
│   ├── notifier.py
│   ├── pyproject.toml
│   └── prompts/           # Meta-prompt CO-STAR + prompts por fase
├── docs/                  # Specs, arquitectura, validaciones
├── src/                   # Solución .NET 8
│   ├── WebImagenologia.Web/
│   └── WebImagenologia.Tests/
├── n8n/workflows/         # Workflows N8N exportados
└── db/                    # Scripts SQL y Stored Procedures
```

---

## Inicio rápido — Orquestador

```powershell
# 1. Instalar dependencias Python
cd agentic
pip install -e .

# 2. Configurar credenciales
copy .env.example .env
# editar .env con CURSOR_API_KEY y LEAD_WEBHOOK_URL

# 3. Ejecutar orquestador (todas las fases)
python orchestrator.py

# 4. Ejecutar solo una fase
python orchestrator.py --phase 01
```

---

## Prerequisitos

- .NET 8 SDK
- Python 3.11+
- Cuenta Cursor con API Key (Cursor Dashboard → Integrations)
- Acceso a red para la API Esculapio

---

## Desarrollo local

```powershell
# Restaurar y compilar
dotnet build src/WebImagenologia.sln

# Ejecutar la aplicación
dotnet run --project src/WebImagenologia.Web/

# Ejecutar tests
dotnet test src/WebImagenologia.Tests/

# Verificar formato
dotnet format --verify-no-changes src/WebImagenologia.sln
```

La aplicación escucha en `https://localhost:5001` (o el puerto configurado en
`launchSettings.json`). El login consume la API Esculapio en tiempo real; se
requiere conectividad de red hacia `appsintranet.esculapiosis.com`.

---

## Despliegue en IIS (Windows Server)

### Publicar

```powershell
dotnet publish src/WebImagenologia.Web/ -c Release -o publish/
```

Copiar el contenido de `publish/` al servidor IIS (ej. `C:\inetpub\webImagenologia\`).

### IIS — pasos resumidos

1. Instalar **ASP.NET Core Hosting Bundle 8.x** y reiniciar IIS.
2. Crear Application Pool con **.NET CLR: No Managed Code**.
3. Crear sitio apuntando a la carpeta publicada.
4. Configurar variables de entorno (ver tabla abajo).
5. Importar workflow N8N desde `n8n/workflows/programacion-estudios.json`.
6. Configurar credenciales MySQL en N8N Credentials Store.

Documentación detallada: [`docs/deploy-notes.md`](docs/deploy-notes.md).

### Variables de entorno requeridas

| Variable | Descripción |
|----------|-------------|
| `EsculapioApi__BaseUrl` | URL base API Esculapio |
| `N8n__WebhookUrl` | Webhook N8N para automatización de programación |
| `DataProtection__KeysPath` | Carpeta persistente para claves de cifrado de sesión |
| `Session__TimeoutMinutes` | Minutos de expiración de sesión (default: 30) |
| `ASPNETCORE_ENVIRONMENT` | `Production` en servidor |

> No incluir credenciales de usuario ni contraseñas MySQL en configuración.
> El usuario las ingresa en el login; se almacenan cifradas en sesión.

### N8N

| Recurso | Ubicación |
|---------|-----------|
| Instancia | `https://n8n.esculapiosis.com` |
| Workflow | `n8n/workflows/programacion-estudios.json` |
| SPs MySQL | `db/stored_procedures/` |

Los Stored Procedures (`ConsOrdenesResultados`, `Get_ProgramacionEstudiosDiagnosticos`)
se invocan **desde N8N**, nunca desde la aplicación web.

---

## Módulos implementados

| # | Módulo | Ruta | Rol |
|---|--------|------|-----|
| 1 | Login | `/Account/Login` | Todos |
| 2 | Radiólogos por Empresa | `/Parametros/Radiologos` | Administrador |
| 3 | Operadores por Empresa | `/Parametros/Operadores` | Administrador |
| 4 | Parametrización Estudios | `/Parametros/Estudios` | Administrador |
| 5 | Asignación Estudios | `/Condicional/Asignacion` | Administrador |
| 6 | Automatización Programación | `/Condicional/Automatizacion` | Administrador |
| 7 | Portal Radiólogos | `/PortalRadiologos` | Radiologo |
| 8 | Lecturas de Estudios | `/Lecturas` | Administrador, Operador |
| 9 | Consultas / Reportes | `/Reportes` | Administrador |

---

## Validación y tests

```powershell
dotnet build src/WebImagenologia.sln -c Release
dotnet test src/WebImagenologia.Tests/ -c Release
```

La suite incluye tests de regresión (`RegressionTests.cs`) que verifican login por
rol, acceso denegado en rutas protegidas y validación de upload de audio.

Gates completos definidos en `docs/validation-rules.md`.

---

## Documentación

- `docs/specs.md` — especificación funcional por módulo
- `docs/deploy-notes.md` — guía completa de despliegue IIS + N8N
- `docs/agents.md` — catálogo de agentes
- `docs/skills.md` — catálogo de capacidades reutilizables
- `docs/architecture.md` — diagramas de arquitectura
- `docs/validation-rules.md` — reglas de validación por fase
- `agentic/prompts/meta_prompt_costar.md` — prompt CO-STAR maestro
