# specs.md — Especificación Funcional por Módulo
# Plataforma webImagenologia · Esculapio

> Contrato funcional de cada vista. Los sub-agentes DEBEN leer este documento
> antes de implementar cualquier módulo. Los criterios de aceptación (Given/When/Then)
> son los que valida el QAAgent en Fase 12.

---

## MÓDULO 1 — Login de Acceso

### Ruta: `GET /Account/Login`, `POST /Account/Login`

### Campos UI
| Campo | Tipo HTML | Validación | Descripción |
|-------|-----------|-----------|-------------|
| Usuario | `input[type=text]` | Required | Usuario del sistema Esculapio |
| Contraseña | `input[type=password]` | Required | Contraseña del usuario |
| Servidor | `select` | Required | Dropdown con servidores disponibles |
| Botón Acceder | `button[type=submit]` | — | Dispara el login |

### Endpoint API consumido
```
GET /Usuarios/obtener-servidores
→ Retorna: [ { descripcion, ipConexion, bdConexion, portConexion } ]

GET /Usuarios/obtener-validaconexion?IpConexion=&Usuario=&PasswordUsu=
→ Retorna: { nombreUsuario, rol, empresasAsignadas: [...] }
```

### ViewModel C#: `LoginViewModel`
```csharp
public class LoginViewModel
{
    [Required] public string Usuario { get; set; } = string.Empty;
    [Required][DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Required] public string ServidorSeleccionado { get; set; } = string.Empty;
    public List<ServidorDto> Servidores { get; set; } = new();
}
```

### Criterios de aceptación
```gherkin
Given el usuario accede a /Account/Login
When el sistema carga la página
Then el dropdown de servidores se llena con los datos de la API

Given el usuario selecciona un servidor, digita usuario y contraseña válidos
When hace clic en "Acceder"
Then es redirigido al Home con su rol asignado
And el ConnectionString del servidor queda cifrado en la sesión

Given el usuario digita credenciales inválidas
When hace clic en "Acceder"
Then ve un mensaje de error "Usuario o contraseña incorrectos"
And permanece en la página de login
```

---

## MÓDULO 2 — Parámetros > Radiólogos por Empresa

### Ruta: `GET /Parametros/Radiologos`, `POST /Parametros/Radiologos/Registrar`
### Acceso: Solo Administrador

### Campos UI
| Campo | Tipo HTML | Descripción |
|-------|-----------|-------------|
| Médico | `select` | Dropdown con médicos radiólogos disponibles |
| Nombre Médico | `input[type=text]` readonly | Se rellena al seleccionar el dropdown |
| Usuario Esculapio | `input[type=text]` | Se digita manualmente |
| Empresas | `input[type=checkbox]` múltiple | Multicheckbox con empresas activas |
| Nombre Empresa | `input[type=text]` readonly | Se rellena al seleccionar |
| Botón Agregar | `button[type=submit]` | Registra el radiólogo |
| Grid Médicos | `table` | Lista de radiólogos con Editar y Eliminar |

### Endpoints API
```
GET /Medicos/obtener-medicos?empresa=XX    → lista de médicos
GET /Radiologos/obtener-registrados?empresa=XX  → grid existente
POST /Radiologos/registrar                → registrar nuevo
DELETE /Radiologos/eliminar/{cedula}      → eliminar
```

### Criterios de aceptación
```gherkin
Given el administrador accede a /Parametros/Radiologos
When carga la página
Then el multicheckbox de empresas muestra las empresas asignadas al usuario en sesión
And el dropdown de médicos muestra los médicos disponibles

Given el administrador selecciona un médico y al menos una empresa
When hace clic en "Agregar"
Then el radiólogo aparece en el grid
And un mensaje de éxito es visible

Given el administrador hace clic en "Eliminar" en el grid
When confirma la acción en el modal
Then el registro desaparece del grid
```

---

## MÓDULO 3 — Parámetros > Operadores por Empresa

### Ruta: `GET /Parametros/Operadores`, `POST /Parametros/Operadores/Registrar`
### Acceso: Solo Administrador

### Campos UI
| Campo | Tipo HTML | Descripción |
|-------|-----------|-------------|
| Operador | `select` | Dropdown con operadores/funcionarios |
| Usuario Esculapio | `input[type=text]` | Se digita manualmente |
| Nombre Operador | `input[type=text]` readonly | Se rellena al seleccionar |
| Empresas | `input[type=checkbox]` múltiple | Multicheckbox empresas activas |
| Botón Agregar | `button[type=submit]` | |
| Grid Operadores | `table` | Lista con Editar y Eliminar |

### Criterios de aceptación
*(Idénticos al módulo Radiólogos, adaptados para Operadores)*

---

## MÓDULO 4 — Condicionales > Parametrización de Estudios

### Ruta: `GET /Parametros/Estudios`, `POST /Parametros/Estudios/Registrar`
### Acceso: Solo Administrador

### Campos UI
| Campo | Tipo HTML | Descripción |
|-------|-----------|-------------|
| Dependencia | `select` | Dropdown de dependencias |
| Nombre Dependencia | `input[type=text]` readonly | |
| Servicio | `select` | Dropdown filtrado por dependencia (AJAX) |
| Nombre Servicio | `input[type=text]` readonly | |
| Cantidad | `input[type=number]` | Cantidad entera de estudios |
| Lectura | `input[type=checkbox]` | Default: checked |
| Empresas | `input[type=checkbox]` múltiple | Multicheckbox |
| Botón Registrar | `button[type=submit]` | |
| Grid Servicios | `table` | Registros de `estudiosdiagnosticos_empresa` |

### Tabla afectada: `estudiosdiagnosticos_empresa`
```sql
Empresa, CodDependencia, CodServicio, CodEsquema, Cantidad, Estado
```

### Criterios de aceptación
```gherkin
Given el administrador selecciona una dependencia
When el dropdown cambia
Then el dropdown de servicios se actualiza vía AJAX con los servicios de esa dependencia

Given el administrador completa el formulario y selecciona empresas
When hace clic en "Registrar"
Then se crea un registro por cada empresa seleccionada en estudiosdiagnosticos_empresa
```

---

## MÓDULO 5 — Condicionales > Asignación No. Estudios por Empresa

### Ruta: `GET /Condicional/Asignacion`, `POST /Condicional/Asignacion/Registrar`
### Acceso: Solo Administrador

### Tabla afectada: `estudiosdiagnosticos_medicos`
```sql
Empresa, CedulaMedico, CodDependencia, CodServicio, Cantidad, Estado
```

### Criterios de aceptación
```gherkin
Given el administrador selecciona una empresa
When cambia el dropdown de empresa
Then el dropdown de médicos se filtra por esa empresa (AJAX)
And el dropdown de dependencias se filtra por esa empresa (AJAX)

Given el administrador completa todos los campos
When hace clic en "Registrar"
Then el registro aparece en el grid filtrado por empresa
```

---

## MÓDULO 6 — Condicionales > Automatización Programación

### Ruta: `GET /Condicional/Automatizacion`, `POST /Condicional/Automatizacion/Registrar`
### Acceso: Solo Administrador

### Tabla afectada: `estudiosdiagnosticos_automatizacionwf`
```sql
Id_Automatizacion int(2), Frecuencia char(3), Hora_Automatizacion varchar(15), Estado char(3)
```

### Criterios de aceptación
```gherkin
Given el administrador configura frecuencia "Diario" y hora "06:00"
When hace clic en "Registrar"
Then se guarda en estudiosdiagnosticos_automatizacionwf
And se envía un POST al webhook N8N con los parámetros de schedule

Given el administrador desactiva una automatización en el grid
When hace clic en el toggle de estado
Then el Estado cambia a 'INA' en la BD
And N8N recibe la notificación de desactivación
```

---

## MÓDULO 7 — Portal Web Radiólogos

### Ruta: `GET /PortalRadiologos`, `GET /PortalRadiologos/DetalleEstudio`
### Acceso: Solo Médico/Radiólogo

### Tabla fuente: `estudiosdiagnosticos_programacion`
```sql
Empresa, Consecutivo, NoCuenta, CedulaMedico, FechaProgramacion,
Servicio, CodServicio, Dependencia, NoOrden, AudioRadiologo,
UsuarioOperador, FechaAsignacion, Estado
```

### Criterios de aceptación
```gherkin
Given el radiólogo accede a /PortalRadiologos
When selecciona una empresa del dropdown
Then el grid muestra sus estudios programados (pendientes de hoy + días anteriores)

Given el radiólogo hace clic en una fila del grid
When se carga el panel de detalle
Then muestra los datos del caso, diagnósticos y notas médicas del tab correspondiente

Given el radiólogo sube un archivo de audio mp3
When el upload es exitoso
Then el registro en el grid muestra icono de audio
And puede reproducir el audio con el player

Given el radiólogo intenta subir un archivo .exe
When selecciona el archivo
Then ve un mensaje de error "Formato no permitido"
And el archivo no se sube
```

---

## MÓDULO 8 — Portal Web Lecturas de Estudios

### Ruta: `GET /Lecturas`, `POST /Lecturas/Consultar`
### Acceso: Administrador + Operador

### Criterios de aceptación
```gherkin
Given el operador filtra por empresa y rango de fechas
When hace clic en "Consultar"
Then el grid muestra los estudios programados con su estado (Pendiente/Leído)
And muestra el icono de audio cuando corresponde

Given el operador hace clic en "Ver Detalle"
When se carga la vista de detalle
Then muestra diagnóstico y notas médicas en tabs
And si hay audio, el player está disponible
```

---

## MÓDULO 9 — Consultas / Reportes

### Ruta: `GET /Reportes`, `POST /Reportes/Consultar`, `GET /Reportes/ExportarExcel`
### Acceso: Solo Administrador

### Criterios de aceptación
```gherkin
Given el administrador selecciona empresa, rango de fechas y tipo de reporte
When hace clic en "Consultar"
Then el grid muestra los resultados filtrados

Given el administrador hace clic en "Exportar Excel"
When el sistema genera el archivo
Then se descarga un archivo .xlsx con los mismos datos del grid
```

---

## URLs de la API Esculapio (referencia completa)

```
urlbase = https://appsintranet.esculapiosis.com/ApiCampbell/api

# Endpoints existentes:
GET {urlbase}/Usuarios/obtener-servidores
GET {urlbase}/Usuarios/obtener-empresas?Ipconexion=&BdConexion=&PortConexion=&Usuario=&PasswordUsu=
GET {urlbase}/Usuarios/obtener-validaconexion?IpConexion=&Usuario=&PasswordUsu=
GET {urlbase}/Diagnosticos/obtener-diagnosticocuenta?Empresa=&NoCuenta=
GET {urlbase}/Diagnosticos/obtener-notasmedicascuenta?Empresa=&NoCuenta=

# Endpoints pendientes de confirmar con el equipo Esculapio:
POST {urlbase}/Radiologos/registrar
DELETE {urlbase}/Radiologos/eliminar/{cedula}
POST {urlbase}/Operadores/registrar
DELETE {urlbase}/Operadores/eliminar/{cedula}
GET {urlbase}/Estudios/obtener-dependencias
GET {urlbase}/Estudios/obtener-servicios?codDependencia=
GET {urlbase}/Estudiosempresa/registrar
GET {urlbase}/Programacion/obtener-programados?empresa=&cedulaMedico=&fecha=
POST {urlbase}/Programacion/subir-audio
GET {urlbase}/Programacion/obtener-audio?consecutivo=&empresa=
DELETE {urlbase}/Programacion/eliminar-audio
```

> Endpoints sin confirmar → el sub-agente debe emitir `BLOCKED` con descripción
> del endpoint requerido, y el Lead debe solicitarlos al equipo API.
