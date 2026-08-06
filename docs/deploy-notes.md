# Notas de despliegue — webImagenologia

Plataforma ASP.NET Core MVC 8 para IIS en Windows Server.
Última revisión: Mayo 2026 (Fase 12 — Release).

---

## Requisitos del servidor

| Componente | Versión mínima |
|------------|----------------|
| Windows Server | 2019+ |
| IIS | 10+ con ASP.NET Core Hosting Bundle 8.x |
| .NET Runtime | 8.0 (incluido en Hosting Bundle) |
| N8N | Instancia accesible desde el servidor web |
| Red | Salida HTTPS hacia API Esculapio y webhook N8N |

Instalar el **ASP.NET Core Hosting Bundle 8.x** desde:
https://dotnet.microsoft.com/download/dotnet/8.0

Reiniciar IIS después de instalar el bundle:

```powershell
net stop was /y
net start w3svc
```

---

## 1. Publicar la aplicación

Desde la raíz del repositorio:

```powershell
dotnet publish src/WebImagenologia.Web/ -c Release -o publish/
```

La carpeta `publish/` contiene el binario listo para copiar al servidor IIS.

---

## 2. Crear sitio en IIS

1. Copiar el contenido de `publish/` a una ruta persistente, por ejemplo:
   `C:\inetpub\webImagenologia\`
2. En IIS Manager → **Sites** → **Add Website**:
   - **Site name**: `webImagenologia`
   - **Physical path**: `C:\inetpub\webImagenologia\`
   - **Binding**: HTTPS en el puerto deseado (recomendado) o HTTP para entornos internos
3. **Application Pool**:
   - Nombre: `webImagenologiaPool`
   - **.NET CLR version**: **No Managed Code**
   - **Start Mode**: AlwaysRunning (opcional, recomendado en producción)
   - **Identity**: cuenta de servicio con permisos de lectura en la carpeta del sitio

---

## 3. Variables de entorno

Configurar en el Application Pool (Environment Variables) o en `web.config` dentro de `<aspNetCore>`:

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `EsculapioApi__BaseUrl` | URL base de la API REST Esculapio | `https://appsintranet.esculapiosis.com/ApiCampbell/api` |
| `EsculapioApi__TimeoutSeconds` | Timeout HTTP hacia la API (segundos) | `30` |
| `N8n__WebhookUrl` | Webhook N8N para automatización de programación | `https://n8n.esculapiosis.com/webhook/actualizar-schedule` |
| `N8n__TimeoutSeconds` | Timeout HTTP hacia N8N (segundos) | `15` |
| `Session__TimeoutMinutes` | Expiración de sesión e idle timeout | `30` |
| `DataProtection__KeysPath` | Carpeta persistente para claves DataProtection (sesión cifrada) | `C:\ProgramData\webImagenologia\keys` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

Ejemplo en `web.config`:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\WebImagenologia.Web.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="EsculapioApi__BaseUrl"
                         value="https://appsintranet.esculapiosis.com/ApiCampbell/api" />
    <environmentVariable name="N8n__WebhookUrl"
                         value="https://n8n.esculapiosis.com/webhook/actualizar-schedule" />
    <environmentVariable name="DataProtection__KeysPath"
                         value="C:\ProgramData\webImagenologia\keys" />
  </environmentVariables>
</aspNetCore>
```

> **Importante**: No incluir credenciales de usuario MySQL ni contraseñas de API en
> `appsettings.json` ni en `web.config`. Las credenciales de conexión al servidor
> Esculapio las ingresa cada usuario en el formulario de login y se almacenan
> cifradas en sesión vía `IDataProtector`.

Crear la carpeta de claves y otorgar permisos de escritura al identity del App Pool:

```powershell
New-Item -ItemType Directory -Force -Path "C:\ProgramData\webImagenologia\keys"
icacls "C:\ProgramData\webImagenologia\keys" /grant "IIS AppPool\webImagenologiaPool:(OI)(CI)M"
```

---

## 4. Configuración post-deploy

### 4.1 Verificar conectividad API

Desde el servidor, confirmar acceso a:

```
GET https://appsintranet.esculapiosis.com/ApiCampbell/api/Usuarios/obtener-servidores
```

### 4.2 Importar workflow N8N

1. Acceder a la instancia N8N: `https://n8n.esculapiosis.com`
2. **Workflows** → **Import from File**
3. Seleccionar: `n8n/workflows/programacion-estudios.json`
4. Activar el workflow importado

### 4.3 Credenciales MySQL en N8N

Los Stored Procedures se ejecutan **desde N8N**, no desde la aplicación web:

| SP | Propósito |
|----|-----------|
| `ConsOrdenesResultados` | Obtiene estudios sin resultado |
| `Get_ProgramacionEstudiosDiagnosticos` | Distribuye estudios a radiólogos |

Configurar en N8N Credentials Store la conexión MySQL 5.6 con permisos de
ejecución sobre estos SPs y las tablas `estudiosdiagnosticos_*`.

Scripts SQL de referencia en `db/stored_procedures/`.

### 4.4 Webhook de automatización

El módulo **Condicionales > Automatización** envía POST al webhook configurado
en `N8n__WebhookUrl` al registrar o desactivar una programación. Verificar que
el endpoint responda HTTP 200 desde el servidor IIS.

---

## 5. Checklist de smoke test post-deploy

- [ ] `GET /Account/Login` carga el dropdown de servidores
- [ ] Login como Administrador → acceso a Parámetros y Condicionales
- [ ] Login como Operador → acceso a Lecturas, sin acceso a Reportes
- [ ] Login como Radiólogo → acceso a Portal Radiólogos únicamente
- [ ] Subida de audio mp3 en portal radiólogos (≤ 25 MB)
- [ ] Exportación Excel en Reportes descarga `.xlsx`
- [ ] Workflow N8N activo y cron ejecutándose según `estudiosdiagnosticos_automatizacionwf`

---

## 6. Logs y diagnóstico

| Ubicación | Contenido |
|-----------|-----------|
| `publish/logs/stdout_*.log` | Salida estándar de la app (si `stdoutLogEnabled=true`) |
| Event Viewer → Application | Errores del módulo ASP.NET Core Module |
| IIS Failed Request Tracing | Diagnóstico HTTP detallado (habilitar por sitio) |

Para habilitar logs detallados temporalmente:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"   # solo diagnóstico, revertir en producción
```

---

## 7. Seguridad — recordatorios

- ConnectionString del servidor seleccionado: **solo en sesión cifrada** (`SessionService` + `IDataProtector`)
- Un único `HttpClient` tipado: `EsculapioApiClient` — sin conexión directa a MySQL desde el web
- Rutas admin protegidas con `[Authorize(Roles = "Administrador")]`
- Portal radiólogos: `[Authorize(Roles = "Radiologo")]`
- Audio: máximo 25 MB, MIME types `audio/mpeg`, `audio/wav`, `audio/ogg`, `audio/mp4`, `audio/x-m4a`
