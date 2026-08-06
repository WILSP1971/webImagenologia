# skills.md — Catálogo de Capacidades Reutilizables
# Plataforma webImagenologia · Esculapio

> Cada skill es una capacidad técnica reutilizable por los sub-agentes.
> Cuando un agente necesita implementar algo de esta lista, debe seguir
> el patrón definido aquí para garantizar consistencia.

---

## SKILL: `consume-api`

**Descripción**: Llamada HTTP a la API Esculapio desde un servicio .NET.

**Patrón**:
```csharp
// En EsculapioApiClient.cs — ÚNICO lugar autorizado
public async Task<IEnumerable<T>> GetAsync<T>(
    string relativeUrl, CancellationToken ct = default)
{
    var response = await _http.GetAsync(relativeUrl, ct);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<IEnumerable<T>>(_jsonOptions, ct)
        ?? Enumerable.Empty<T>();
}
```

**Reglas**:
- Siempre `async/await`
- Siempre pasar `CancellationToken`
- Siempre capturar `HttpRequestException` → loguear y relanzar como `EsculapioApiException`
- Usar `_jsonOptions` con `PropertyNameCaseInsensitive = true`

---

## SKILL: `bootstrap-grid`

**Descripción**: Tabla de datos Bootstrap 5 con acciones.

**Patrón HTML**:
```html
<div class="table-responsive">
  <table class="table table-striped table-hover table-sm align-middle">
    <thead class="table-dark">
      <tr>
        <th scope="col">Campo 1</th>
        <th scope="col">Campo 2</th>
        <th scope="col" class="text-center">Acciones</th>
      </tr>
    </thead>
    <tbody>
      @foreach (var item in Model.Items)
      {
        <tr>
          <td>@item.Campo1</td>
          <td>@item.Campo2</td>
          <td class="text-center">
            <a asp-action="Editar" asp-route-id="@item.Id"
               class="btn btn-warning btn-sm" title="Editar"
               aria-label="Editar @item.Campo1">
              <i class="bi bi-pencil"></i>
            </a>
            <button type="button" class="btn btn-danger btn-sm"
                    data-bs-toggle="modal" data-bs-target="#modalEliminar"
                    data-id="@item.Id" data-nombre="@item.Campo1"
                    title="Eliminar" aria-label="Eliminar @item.Campo1">
              <i class="bi bi-trash"></i>
            </button>
          </td>
        </tr>
      }
    </tbody>
  </table>
</div>
```

---

## SKILL: `multicheckbox-empresas`

**Descripción**: Grupo de checkboxes para selección de empresas asignadas al usuario.

**Patrón HTML (Razor)**:
```html
<div class="mb-3">
  <label class="form-label fw-bold">Empresas</label>
  <div class="d-flex flex-wrap gap-2 border rounded p-2" id="empresasGroup">
    @foreach (var emp in Model.EmpresasDisponibles)
    {
      <div class="form-check">
        <input class="form-check-input" type="checkbox"
               name="EmpresasSeleccionadas" value="@emp.Codigo"
               id="emp_@emp.Codigo"
               @(Model.EmpresasSeleccionadas.Contains(emp.Codigo) ? "checked" : "") />
        <label class="form-check-label" for="emp_@emp.Codigo">
          @emp.Nombre
        </label>
      </div>
    }
  </div>
</div>
```

**Binding en el ViewModel**: `public List<string> EmpresasSeleccionadas { get; set; } = new();`

---

## SKILL: `audio-upload-blob`

**Descripción**: Upload de audio al servidor, validación de tipo y tamaño,
almacenamiento como BLOB vía API externa.

**Patrón JS** (`portalRadiologos.js`):
```javascript
async function subirAudio(file, consecutivo, empresa) {
  const ALLOWED_TYPES = ['audio/mpeg', 'audio/wav', 'audio/ogg', 'audio/mp4', 'audio/x-m4a'];
  const MAX_SIZE = 25 * 1024 * 1024; // 25 MB

  if (!ALLOWED_TYPES.includes(file.type)) {
    mostrarError('Formato de audio no permitido. Use mp3, wav, ogg o m4a.');
    return;
  }
  if (file.size > MAX_SIZE) {
    mostrarError('El archivo supera el límite de 25 MB.');
    return;
  }

  const formData = new FormData();
  formData.append('archivo', file);
  formData.append('consecutivo', consecutivo);
  formData.append('empresa', empresa);

  const resp = await fetch('/PortalRadiologos/SubirAudio', {
    method: 'POST',
    body: formData,
    headers: { 'RequestVerificationToken': getAntiForgeryToken() }
  });

  if (!resp.ok) {
    const error = await resp.text();
    mostrarError(`Error al subir audio: ${error}`);
    return;
  }
  mostrarExito('Audio guardado correctamente.');
}
```

**Validación servidor** (C#):
```csharp
private static readonly string[] AllowedAudioTypes =
    ["audio/mpeg", "audio/wav", "audio/ogg", "audio/mp4", "audio/x-m4a"];
private const long MaxAudioSizeBytes = 25 * 1024 * 1024;

if (!AllowedAudioTypes.Contains(archivo.ContentType))
    return BadRequest("Tipo de archivo no permitido.");
if (archivo.Length > MaxAudioSizeBytes)
    return BadRequest("El archivo supera el límite de 25 MB.");
```

---

## SKILL: `session-encrypted-connstring`

**Descripción**: Almacenar y recuperar el ConnectionString cifrado en la sesión.

**Patrón C#**:
```csharp
// Program.cs
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeysPath"]!))
    .SetApplicationName("webImagenologia");

// SessionService.cs
private readonly IDataProtector _protector;

public void GuardarConnectionString(string connString)
{
    var cifrado = _protector.Protect(connString);
    _httpContextAccessor.HttpContext!.Session.SetString("ConnString", cifrado);
}

public string? ObtenerConnectionString()
{
    var cifrado = _httpContextAccessor.HttpContext!.Session.GetString("ConnString");
    return cifrado is null ? null : _protector.Unprotect(cifrado);
}
```

---

## SKILL: `n8n-cron-trigger`

**Descripción**: Configurar un trigger cron en N8N y actualizarlo desde el web.

**Configurar desde el web** (llamada al webhook N8N):
```csharp
// CondicionalController.cs
var payload = new {
    frecuencia = viewModel.Frecuencia,
    hora = viewModel.HoraAutomatizacion,
    activo = viewModel.Activo
};
await _n8nClient.PostAsJsonAsync("webhook/actualizar-schedule", payload);
```

**N8N Schedule Trigger** (en `programacion-estudios.json`):
- Nodo tipo `n8n-nodes-base.scheduleTrigger`
- Configurar `rule.interval` con horas o minutos según `Frecuencia`

---

## SKILL: `mysql-sp-invoke`

**Descripción**: Invocar un Stored Procedure MySQL desde N8N.

**Nodo MySQL en N8N**:
```json
{
  "type": "n8n-nodes-base.mysql",
  "parameters": {
    "operation": "executeQuery",
    "query": "CALL NombreDelProcedimiento(?, ?, ?)",
    "options": {}
  }
}
```

Los parámetros se pasan vía expresiones N8N: `{{ $json.campo }}` o `{{ $env.VARIABLE }}`.

---

## SKILL: `cascade-dropdown-ajax`

**Descripción**: Dropdown que filtra el contenido de otro dropdown vía AJAX.

**Patrón JS**:
```javascript
document.getElementById('ddlDependencia').addEventListener('change', async function () {
  const codDep = this.value;
  const ddlServicios = document.getElementById('ddlServicio');
  ddlServicios.innerHTML = '<option value="">Cargando...</option>';

  const resp = await fetch(`/Parametros/Estudios/ServiciosPorDependencia?codDependencia=${codDep}`);
  if (!resp.ok) { ddlServicios.innerHTML = '<option value="">Error</option>'; return; }

  const servicios = await resp.json();
  ddlServicios.innerHTML = '<option value="">-- Seleccione --</option>' +
    servicios.map(s => `<option value="${s.codServicio}">${s.nombreServicio}</option>`).join('');
});
```

**Endpoint C#** (retorna JSON):
```csharp
[HttpGet]
public async Task<IActionResult> ServiciosPorDependencia(string codDependencia)
{
    var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(codDependencia);
    return Json(servicios);
}
```

---

## SKILL: `modal-confirm-delete`

**Descripción**: Modal de confirmación Bootstrap antes de eliminar un registro.

**Patrón HTML** (en `_Layout.cshtml` o en la vista):
```html
<div class="modal fade" id="modalEliminar" tabindex="-1" aria-labelledby="modalEliminarLabel" aria-hidden="true">
  <div class="modal-dialog">
    <div class="modal-content">
      <div class="modal-header bg-danger text-white">
        <h5 class="modal-title" id="modalEliminarLabel">Confirmar eliminación</h5>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        ¿Está seguro que desea eliminar <strong id="modalNombreItem"></strong>?
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancelar</button>
        <form id="formEliminar" method="post">
          @Html.AntiForgeryToken()
          <button type="submit" class="btn btn-danger">Eliminar</button>
        </form>
      </div>
    </div>
  </div>
</div>
```

**JS para poblar el modal**:
```javascript
document.querySelectorAll('[data-bs-target="#modalEliminar"]').forEach(btn => {
  btn.addEventListener('click', () => {
    document.getElementById('modalNombreItem').textContent = btn.dataset.nombre;
    document.getElementById('formEliminar').action = `/Ruta/Eliminar/${btn.dataset.id}`;
  });
});
```
