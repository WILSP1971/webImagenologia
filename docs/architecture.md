# architecture.md — Diagramas de Arquitectura
# Plataforma webImagenologia · Esculapio

---

## 1. Vista General del Sistema

```mermaid
flowchart TD
    subgraph usuarios [Usuarios]
        Admin[Administrador]
        Radiologo[Médico / Radiólogo]
        Operador[Operador]
    end

    subgraph web [WebImagenologia - IIS / .NET 8]
        MVC[ASP.NET Core MVC]
        Services[Services Layer]
        ApiClient[EsculapioApiClient]
    end

    subgraph externos [Sistemas Externos]
        API[API Esculapio\nappsintranet.esculapiosis.com\nApiCampbell/api]
        MySQL[(MySQL 5.6\nesculapio_db)]
        N8N[N8N Workflows\nn8n.esculapiosis.com]
    end

    Admin -->|HTTPS| MVC
    Radiologo -->|HTTPS| MVC
    Operador -->|HTTPS| MVC

    MVC --> Services
    Services --> ApiClient
    ApiClient -->|REST HTTP| API
    API -->|Queries| MySQL

    N8N -->|CALL SP| MySQL
    MVC -->|webhook| N8N
```

---

## 2. Arquitectura del Pipeline Agentico

```mermaid
flowchart TD
    Lead[Lead del Proyecto] -->|python orchestrator.py| Orch[orchestrator.py\nCursor SDK Python]

    Orch -->|lee| MetaPrompt[meta_prompt_costar.md\nCO-STAR]
    Orch -->|lee| PhasePrompt[phase_NN_*.md]
    Orch -->|lee| Specs[docs/specs.md]

    Orch -->|Agent.create + send| Agent[Sub-agente Cursor\ncomposer-2.5 local]

    Agent -->|edita archivos| Repo[Repositorio\nwebImagenologia]
    Agent -->|YAML report| Orch

    Orch -->|gate PASS| Notifier[notifier.py]
    Orch -->|gate FAIL| Notifier

    Notifier -->|consola rich| Lead
    Notifier -->|reports/phase_NN.md| Lead
    Notifier -->|webhook JSON| Lead

    Lead -->|aprueba/rechaza| Orch
```

---

## 3. Flujo de Login

```mermaid
sequenceDiagram
    participant U as Usuario
    participant MVC as AccountController
    participant API as EsculapioApiClient
    participant ExtAPI as API Externa
    participant Session as Sesión Cifrada

    U->>MVC: GET /Account/Login
    MVC->>API: ObtenerServidoresAsync()
    API->>ExtAPI: GET /Usuarios/obtener-servidores
    ExtAPI-->>API: [ { descripcion, ip, bd, port } ]
    API-->>MVC: IEnumerable ServidorDto
    MVC-->>U: Vista con dropdown poblado

    U->>MVC: POST /Account/Login (usuario, pwd, servidor)
    MVC->>API: ValidarConexionAsync(ip, usuario, pwd)
    API->>ExtAPI: GET /Usuarios/obtener-validaconexion
    ExtAPI-->>API: { nombreUsuario, rol, empresas }
    API-->>MVC: UsuarioConexionDto

    MVC->>Session: GuardarConnectionString(cifrado)
    MVC->>MVC: HttpContext.SignInAsync(claims)
    MVC-->>U: Redirect /Home
```

---

## 4. Flujo del Portal Radiólogos (Audio)

```mermaid
sequenceDiagram
    participant R as Radiólogo
    participant Portal as PortalRadiologosController
    participant API as EsculapioApiClient
    participant ExtAPI as API Externa

    R->>Portal: GET /PortalRadiologos?empresa=XX
    Portal->>API: ObtenerEstudiosProgramadosAsync(empresa, cedula, fecha)
    API->>ExtAPI: GET /Programacion/obtener-programados
    ExtAPI-->>Portal: [ EstudioProgramadoDto ]
    Portal-->>R: Vista con grid

    R->>Portal: Click en estudio → AJAX DetalleEstudio
    Portal->>API: ObtenerDiagnosticoCuentaAsync(empresa, noCuenta)
    Portal->>API: ObtenerNotasMedicasCuentaAsync(empresa, noCuenta)
    Portal-->>R: JSON con diagnóstico y notas

    R->>Portal: POST /PortalRadiologos/SubirAudio (multipart)
    Portal->>Portal: Validar mime + tamaño
    Portal->>API: SubirAudioAsync(consecutivo, empresa, bytes)
    API->>ExtAPI: POST /Programacion/subir-audio
    ExtAPI-->>Portal: OK
    Portal-->>R: 200 OK
```

---

## 5. Flujo de Automatización N8N

```mermaid
flowchart LR
    subgraph webApp [Web .NET 8]
        UI[UI Automatización]
        Controller[CondicionalController]
        N8NClient[HttpClient N8N]
    end

    subgraph n8n [N8N - n8n.esculapiosis.com]
        Cron[Schedule Trigger\nDiario HH:mm]
        ReadConfig[MySQL - Leer Config]
        IfActivo{Estado ACT?}
        SPOrdenes[MySQL - ConsOrdenesResultados]
        SPProgramar[MySQL - Get_ProgramacionEstudiosDiagnosticos]
        Notify[HTTP - Notificar Web]
    end

    MySQL[(MySQL 5.6)]

    UI -->|Configurar schedule| Controller
    Controller -->|POST webhook/actualizar-schedule| N8NClient
    N8NClient -->|HTTP POST| Cron

    Cron --> ReadConfig
    ReadConfig -->|query| MySQL
    ReadConfig --> IfActivo
    IfActivo -->|Sí| SPOrdenes
    SPOrdenes -->|CALL SP| MySQL
    SPOrdenes --> SPProgramar
    SPProgramar -->|CALL SP| MySQL
    SPProgramar --> Notify
    Notify -->|POST callback| Controller
```

---

## 6. Estructura de Capas (MVC)

```mermaid
flowchart TD
    subgraph presentation [Presentación]
        Views[Razor Views .cshtml\nBootstrap 5]
        JS[vanilla JS + fetch\nwwwroot/js/]
    end

    subgraph application [Aplicación]
        Controllers[Controllers\nAccountController\nParametrosController\nCondicionalController\nPortalRadiologosController\nLecturasController\nReportesController]
        ViewModels[ViewModels\nLoginViewModel\nRadiologosViewModel\n...]
    end

    subgraph infrastructure [Infraestructura]
        ApiClient[EsculapioApiClient\nIHttpClientFactory]
        Session[SessionService\nIDataProtector]
        ApiDtos[API DTOs records\nServidorDto\nEmpresaDto\n...]
    end

    subgraph external [Externos]
        ExtAPI[API Esculapio REST]
        N8NAPI[N8N Webhook]
    end

    Views <--> Controllers
    JS <-->|fetch AJAX| Controllers
    Controllers --> ViewModels
    Controllers --> ApiClient
    Controllers --> Session
    ApiClient --> ApiDtos
    ApiClient --> ExtAPI
    Controllers --> N8NAPI
```
