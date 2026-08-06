using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[Authorize(Roles = RoleNames.Administrador)]
[Route("[controller]")]
public class CondicionalController : Controller
{
    private readonly IEsculapioApiClient _apiClient;
    private readonly IN8nWebhookClient _n8nWebhookClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<CondicionalController> _logger;

    public CondicionalController(
        IEsculapioApiClient apiClient,
        IN8nWebhookClient n8nWebhookClient,
        ISessionService sessionService,
        ILogger<CondicionalController> logger)
    {
        _apiClient = apiClient;
        _n8nWebhookClient = n8nWebhookClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet("Asignacion")]
    public async Task<IActionResult> Asignacion(CancellationToken cancellationToken)
    {
        var model = await BuildAsignacionViewModelAsync(cancellationToken);
        ApplyTempDataMessages();
        return View(model);
    }

    [HttpGet("Asignacion/MedicosPorEmpresa")]
    public async Task<IActionResult> MedicosPorEmpresa(string empresa, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa))
        {
            return Json(Array.Empty<object>());
        }

        try
        {
            var medicos = await LoadMedicosPorEmpresaAsync(empresa, cancellationToken);
            var payload = medicos.Select(m => new
            {
                m.Cedula,
                m.Nombre
            });

            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos para empresa {Empresa}", empresa);
            return Json(Array.Empty<object>());
        }
    }

    [HttpGet("Asignacion/DependenciasPorEmpresa")]
    public async Task<IActionResult> DependenciasPorEmpresa(string empresa, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa))
        {
            return Json(Array.Empty<object>());
        }

        try
        {
            var dependencias = await LoadDependenciasPorEmpresaAsync(empresa, cancellationToken);
            var payload = dependencias.Select(d => new
            {
                d.CodDependencia,
                d.NombreDependencia
            });

            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar dependencias para empresa {Empresa}", empresa);
            return Json(Array.Empty<object>());
        }
    }

    [HttpGet("Asignacion/ServiciosPorDependencia")]
    public async Task<IActionResult> ServiciosPorDependencia(
        string codDependencia,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codDependencia))
        {
            return Json(Array.Empty<object>());
        }

        try
        {
            var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(codDependencia, cancellationToken);
            var payload = servicios.Select(s => new
            {
                s.CodServicio,
                s.NombreServicio
            });

            return Json(payload);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar servicios para dependencia {CodDependencia}", codDependencia);
            return Json(Array.Empty<object>());
        }
    }

    [HttpPost("Asignacion/Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAsignacion(AsignacionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAsignacionListsAsync(model, cancellationToken);
            if (!string.IsNullOrWhiteSpace(model.CodDependencia))
            {
                model.ServiciosDisponibles = await LoadServiciosAsync(model.CodDependencia, cancellationToken);
            }

            return View(model.ModoEdicion ? "AsignacionEditar" : "Asignacion", model);
        }

        await PopulateAsignacionListsAsync(model, cancellationToken);

        var medico = model.MedicosDisponibles.FirstOrDefault(m =>
            m.Cedula.Equals(model.CedulaMedico, StringComparison.OrdinalIgnoreCase));

        if (medico is null)
        {
            ModelState.AddModelError(nameof(model.CedulaMedico), "El médico seleccionado no es válido.");
            return View(model.ModoEdicion ? "AsignacionEditar" : "Asignacion", model);
        }

        var dependencias = await LoadDependenciasPorEmpresaAsync(model.Empresa, cancellationToken);
        var dependencia = dependencias.FirstOrDefault(d =>
            d.CodDependencia.Equals(model.CodDependencia, StringComparison.OrdinalIgnoreCase));

        if (dependencia is null)
        {
            ModelState.AddModelError(nameof(model.CodDependencia), "La dependencia seleccionada no es válida.");
            return View(model.ModoEdicion ? "AsignacionEditar" : "Asignacion", model);
        }

        var servicios = await LoadServiciosAsync(model.CodDependencia, cancellationToken);
        var servicio = servicios.FirstOrDefault(s =>
            s.CodServicio.Equals(model.CodServicio, StringComparison.OrdinalIgnoreCase));

        if (servicio is null)
        {
            ModelState.AddModelError(nameof(model.CodServicio), "El servicio seleccionado no es válido.");
            model.ServiciosDisponibles = servicios;
            return View(model.ModoEdicion ? "AsignacionEditar" : "Asignacion", model);
        }

        try
        {
            var request = new RegistrarAsignacionMedicoRequest(
                model.Empresa,
                model.CedulaMedico,
                model.CodDependencia,
                model.CodServicio,
                model.Cantidad,
                model.Estado);

            await _apiClient.RegistrarAsignacionMedicoAsync(request, cancellationToken);
            TempData["SuccessMessage"] = model.ModoEdicion
                ? "Asignación actualizada correctamente."
                : "Asignación registrada correctamente.";
            return RedirectToAction(nameof(Asignacion));
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(
                ex,
                "Error al registrar asignación {Empresa}/{CedulaMedico}/{CodServicio}",
                model.Empresa,
                model.CedulaMedico,
                model.CodServicio);
            ModelState.AddModelError(string.Empty, "No fue posible registrar la asignación. Intente nuevamente.");
            model.ServiciosDisponibles = servicios;
            return View(model.ModoEdicion ? "AsignacionEditar" : "Asignacion", model);
        }
    }

    [HttpGet("Asignacion/Editar")]
    public async Task<IActionResult> EditarAsignacion(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string codServicio,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa)
            || string.IsNullOrWhiteSpace(cedulaMedico)
            || string.IsNullOrWhiteSpace(codDependencia)
            || string.IsNullOrWhiteSpace(codServicio))
        {
            return RedirectToAction(nameof(Asignacion));
        }

        var model = await BuildAsignacionViewModelAsync(cancellationToken);
        var asignacion = model.AsignacionesRegistradas.FirstOrDefault(a =>
            a.Empresa.Equals(empresa, StringComparison.OrdinalIgnoreCase)
            && a.CedulaMedico.Equals(cedulaMedico, StringComparison.OrdinalIgnoreCase)
            && a.CodDependencia.Equals(codDependencia, StringComparison.OrdinalIgnoreCase)
            && a.CodServicio.Equals(codServicio, StringComparison.OrdinalIgnoreCase));

        if (asignacion is null)
        {
            TempData["ErrorMessage"] = "No se encontró la asignación solicitada.";
            return RedirectToAction(nameof(Asignacion));
        }

        model.Empresa = asignacion.Empresa;
        model.CedulaMedico = asignacion.CedulaMedico;
        model.NombreMedico = asignacion.NombreMedico;
        model.CodDependencia = asignacion.CodDependencia;
        model.NombreDependencia = asignacion.NombreDependencia;
        model.CodServicio = asignacion.CodServicio;
        model.NombreServicio = asignacion.NombreServicio;
        model.Cantidad = asignacion.Cantidad;
        model.Estado = asignacion.Estado;
        model.ModoEdicion = true;
        model.MedicosDisponibles = await LoadMedicosPorEmpresaAsync(model.Empresa, cancellationToken);
        model.DependenciasDisponibles = await LoadDependenciasPorEmpresaAsync(model.Empresa, cancellationToken);
        model.ServiciosDisponibles = await LoadServiciosAsync(model.CodDependencia, cancellationToken);

        return View("AsignacionEditar", model);
    }

    [HttpPost("Asignacion/Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAsignacion(
        string empresa,
        string cedulaMedico,
        string codDependencia,
        string codServicio,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa)
            || string.IsNullOrWhiteSpace(cedulaMedico)
            || string.IsNullOrWhiteSpace(codDependencia)
            || string.IsNullOrWhiteSpace(codServicio))
        {
            TempData["ErrorMessage"] = "Los datos de la asignación son requeridos.";
            return RedirectToAction(nameof(Asignacion));
        }

        try
        {
            await _apiClient.EliminarAsignacionMedicoAsync(
                empresa,
                cedulaMedico,
                codDependencia,
                codServicio,
                cancellationToken);
            TempData["SuccessMessage"] = "Asignación eliminada correctamente.";
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(
                ex,
                "Error al eliminar asignación {Empresa}/{CedulaMedico}/{CodServicio}",
                empresa,
                cedulaMedico,
                codServicio);
            TempData["ErrorMessage"] = "No fue posible eliminar la asignación. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Asignacion));
    }

    [HttpGet("Automatizacion")]
    public async Task<IActionResult> Automatizacion(CancellationToken cancellationToken)
    {
        var model = await BuildAutomatizacionViewModelAsync(cancellationToken);
        ApplyTempDataMessages();
        return View(model);
    }

    [HttpPost("Automatizacion/Registrar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarAutomatizacion(
        AutomatizacionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AutomatizacionesRegistradas = await LoadAutomatizacionesAsync(cancellationToken);
            return View("Automatizacion", model);
        }

        var estado = model.Activo ? "ACT" : "INA";

        try
        {
            var request = new RegistrarAutomatizacionRequest(
                model.IdAutomatizacion,
                model.TipoProgramacion,
                model.Frecuencia,
                model.HoraAutomatizacion,
                estado);

            await _apiClient.RegistrarAutomatizacionAsync(request, cancellationToken);

            var webhookNotified = await NotifyN8nScheduleAsync(
                model.Frecuencia,
                model.HoraAutomatizacion,
                model.Activo,
                model.TipoProgramacion,
                cancellationToken);

            TempData["SuccessMessage"] = webhookNotified
                ? "Automatización registrada correctamente."
                : "Automatización registrada. El webhook N8N no respondió; verifique la conectividad.";

            return RedirectToAction(nameof(Automatizacion));
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al registrar automatización {TipoProgramacion}", model.TipoProgramacion);
            ModelState.AddModelError(string.Empty, "No fue posible registrar la automatización. Intente nuevamente.");
            model.AutomatizacionesRegistradas = await LoadAutomatizacionesAsync(cancellationToken);
            return View("Automatizacion", model);
        }
    }

    [HttpPost("Automatizacion/ToggleEstado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleEstadoAutomatizacion(
        int idAutomatizacion,
        bool activo,
        string frecuencia,
        string horaAutomatizacion,
        string tipoProgramacion,
        CancellationToken cancellationToken)
    {
        if (idAutomatizacion <= 0)
        {
            TempData["ErrorMessage"] = "Identificador de automatización inválido.";
            return RedirectToAction(nameof(Automatizacion));
        }

        var estado = activo ? "ACT" : "INA";

        try
        {
            await _apiClient.ToggleAutomatizacionEstadoAsync(
                new ToggleAutomatizacionEstadoRequest(idAutomatizacion, estado),
                cancellationToken);

            var webhookNotified = await NotifyN8nScheduleAsync(
                frecuencia,
                horaAutomatizacion,
                activo,
                tipoProgramacion,
                cancellationToken);

            TempData["SuccessMessage"] = webhookNotified
                ? activo
                    ? "Automatización activada correctamente."
                    : "Automatización desactivada correctamente."
                : activo
                    ? "Automatización activada. El webhook N8N no respondió."
                    : "Automatización desactivada. El webhook N8N no respondió.";
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de automatización {Id}", idAutomatizacion);
            TempData["ErrorMessage"] = "No fue posible actualizar el estado. Intente nuevamente.";
        }

        return RedirectToAction(nameof(Automatizacion));
    }

    private async Task<AsignacionViewModel> BuildAsignacionViewModelAsync(CancellationToken cancellationToken)
    {
        var model = new AsignacionViewModel();
        await PopulateAsignacionListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateAsignacionListsAsync(AsignacionViewModel model, CancellationToken cancellationToken)
    {
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);
        model.AsignacionesRegistradas = await LoadAsignacionesRegistradasAsync(
            model.EmpresasDisponibles,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.Empresa))
        {
            model.MedicosDisponibles = await LoadMedicosPorEmpresaAsync(model.Empresa, cancellationToken);
            model.DependenciasDisponibles = await LoadDependenciasPorEmpresaAsync(model.Empresa, cancellationToken);

            if (!string.IsNullOrWhiteSpace(model.CodDependencia))
            {
                model.ServiciosDisponibles = await LoadServiciosAsync(model.CodDependencia, cancellationToken);
            }
        }
    }

    private async Task<List<EmpresaDto>> LoadEmpresasAsync(CancellationToken cancellationToken)
    {
        var connection = GetConnectionInfo();
        if (connection is not null)
        {
            try
            {
                var empresas = await _apiClient.ObtenerEmpresasAsync(
                    connection.IpConexion,
                    connection.BdConexion,
                    connection.PortConexion,
                    connection.Usuario,
                    connection.Password,
                    cancellationToken);

                return empresas.ToList();
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible obtener empresas desde la API; se usan empresas de sesión.");
            }
        }

        return _sessionService.ObtenerUsuario()?.EmpresasAsignadas.ToList() ?? [];
    }

    private async Task<List<MedicoDto>> LoadMedicosPorEmpresaAsync(
        string empresa,
        CancellationToken cancellationToken)
    {
        try
        {
            var radiologos = await _apiClient.ObtenerRadiologosRegistradosAsync(
                codigoEmpresa: empresa,
                cancellationToken: cancellationToken);
            var medicos = radiologos
                .Select(r => new MedicoDto(r.CedulaMedico, r.NombreMedico))
                .ToList();

            if (medicos.Count > 0)
            {
                return medicos.OrderBy(m => m.Nombre, StringComparer.OrdinalIgnoreCase).ToList();
            }

            var medicosApi = await _apiClient.ObtenerMedicosAsync(
                codigoEmpresa: empresa,
                cancellationToken: cancellationToken);
            return medicosApi.OrderBy(m => m.Nombre, StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos para empresa {Empresa}", empresa);
            return [];
        }
    }

    private async Task<List<DependenciaDto>> LoadDependenciasPorEmpresaAsync(
        string empresa,
        CancellationToken cancellationToken)
    {
        try
        {
            var estudios = await _apiClient.ObtenerEstudiosEmpresaAsync(empresa, cancellationToken: cancellationToken);
            var codigosDependencia = estudios
                .Select(e => e.CodDependencia)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (codigosDependencia.Count == 0)
            {
                var dependencias = await _apiClient.ObtenerDependenciasAsync(cancellationToken: cancellationToken);
                return dependencias.ToList();
            }

            var todasDependencias = await _apiClient.ObtenerDependenciasAsync(cancellationToken: cancellationToken);
            var dependenciasFiltradas = todasDependencias
                .Where(d => codigosDependencia.Contains(d.CodDependencia))
                .ToList();

            foreach (var estudio in estudios)
            {
                if (dependenciasFiltradas.Any(d =>
                        d.CodDependencia.Equals(estudio.CodDependencia, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                dependenciasFiltradas.Add(new DependenciaDto(
                    estudio.CodDependencia,
                    string.IsNullOrWhiteSpace(estudio.NombreDependencia)
                        ? estudio.CodDependencia
                        : estudio.NombreDependencia));
            }

            return dependenciasFiltradas
                .OrderBy(d => d.NombreDependencia, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar dependencias para empresa {Empresa}", empresa);
            return [];
        }
    }

    private async Task<List<ServicioDto>> LoadServiciosAsync(
        string codDependencia,
        CancellationToken cancellationToken)
    {
        try
        {
            var servicios = await _apiClient.ObtenerServiciosPorDependenciaAsync(codDependencia, cancellationToken);
            return servicios.ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar servicios para dependencia {CodDependencia}", codDependencia);
            return [];
        }
    }

    private async Task<List<AsignacionMedicoDto>> LoadAsignacionesRegistradasAsync(
        IReadOnlyList<EmpresaDto> empresas,
        CancellationToken cancellationToken)
    {
        var asignaciones = new List<AsignacionMedicoDto>();

        foreach (var empresa in empresas)
        {
            try
            {
                var registros = await _apiClient.ObtenerAsignacionesMedicoAsync(empresa.Codigo, cancellationToken);
                foreach (var registro in registros)
                {
                    asignaciones.Add(registro with
                    {
                        NombreEmpresa = string.IsNullOrWhiteSpace(registro.NombreEmpresa)
                            ? empresa.Nombre
                            : registro.NombreEmpresa
                    });
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogWarning(ex, "No fue posible cargar asignaciones para empresa {Empresa}", empresa.Codigo);
            }
        }

        return asignaciones
            .OrderBy(a => a.NombreEmpresa, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.NombreMedico, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.NombreDependencia, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.NombreServicio, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private ServerConnectionInfo? GetConnectionInfo()
    {
        var connectionJson = _sessionService.ObtenerConnectionString();
        if (string.IsNullOrEmpty(connectionJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ServerConnectionInfo>(connectionJson);
    }

    private void ApplyTempDataMessages()
    {
        if (TempData["SuccessMessage"] is string successMessage)
        {
            ViewData["SuccessMessage"] = successMessage;
        }

        if (TempData["ErrorMessage"] is string errorMessage)
        {
            ViewData["ErrorMessage"] = errorMessage;
        }
    }

    private async Task<AutomatizacionViewModel> BuildAutomatizacionViewModelAsync(
        CancellationToken cancellationToken)
    {
        return new AutomatizacionViewModel
        {
            AutomatizacionesRegistradas = await LoadAutomatizacionesAsync(cancellationToken)
        };
    }

    private async Task<List<AutomatizacionDto>> LoadAutomatizacionesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var automatizaciones = await _apiClient.ObtenerAutomatizacionesAsync(cancellationToken);
            return automatizaciones
                .OrderBy(a => a.TipoProgramacion, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.HoraAutomatizacion, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar automatizaciones registradas");
            return [];
        }
    }

    private async Task<bool> NotifyN8nScheduleAsync(
        string frecuencia,
        string hora,
        bool activo,
        string tipoProgramacion,
        CancellationToken cancellationToken)
    {
        var payload = new N8nSchedulePayload(frecuencia, hora, activo, tipoProgramacion);
        return await _n8nWebhookClient.NotifyScheduleUpdateAsync(payload, cancellationToken);
    }
}
