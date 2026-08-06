using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[Authorize(Roles = $"{RoleNames.Administrador},{RoleNames.Operador}")]
public class LecturasController : Controller
{
    private readonly IEsculapioApiClient _apiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<LecturasController> _logger;

    public LecturasController(
        IEsculapioApiClient apiClient,
        ISessionService sessionService,
        ILogger<LecturasController> logger)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(new LecturasViewModel(), cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consultar(LecturasViewModel model, CancellationToken cancellationToken)
    {
        BindDateFilters(model);
        await PopulateListsAsync(model, cancellationToken);

        if (string.IsNullOrWhiteSpace(model.EmpresaSeleccionada))
        {
            ModelState.AddModelError(nameof(model.EmpresaSeleccionada), "Debe seleccionar una empresa.");
            return View("Index", model);
        }

        if (model.FechaFinal < model.FechaInicial)
        {
            ModelState.AddModelError(nameof(model.FechaFinal), "La fecha final no puede ser anterior a la fecha inicial.");
            return View("Index", model);
        }

        try
        {
            var estado = string.IsNullOrWhiteSpace(model.EstadoFiltro) || model.EstadoFiltro == "TODO"
                ? null
                : model.EstadoFiltro;

            var cedulaMedico = string.IsNullOrWhiteSpace(model.CedulaMedicoFiltro)
                ? null
                : model.CedulaMedicoFiltro;

            var lecturas = await _apiClient.ObtenerLecturasAsync(
                model.EmpresaSeleccionada,
                model.FechaInicial,
                model.FechaFinal,
                cedulaMedico,
                estado,
                cancellationToken: cancellationToken);

            model.Lecturas = lecturas
                .OrderByDescending(l => l.FechaProgramacion)
                .ThenBy(l => l.NoCuenta)
                .ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible consultar lecturas para empresa {Empresa}", model.EmpresaSeleccionada);
            model.Lecturas = [];
            ViewData["ErrorMessage"] = "No fue posible consultar las lecturas. Intente nuevamente.";
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(long id, string empresa, CancellationToken cancellationToken)
    {
        var consecutivo = id;
        if (consecutivo <= 0 || string.IsNullOrWhiteSpace(empresa))
        {
            return BadRequest("Parámetros inválidos.");
        }

        try
        {
            var lecturas = await _apiClient.ObtenerLecturasAsync(
                empresa,
                DateOnly.MinValue,
                DateOnly.MaxValue,
                cedulaMedico: null,
                estado: null,
                consecutivo: consecutivo,
                cancellationToken: cancellationToken);

            var lectura = lecturas.FirstOrDefault(l => l.Consecutivo == consecutivo);
            if (lectura is null)
            {
                return NotFound("Estudio no encontrado.");
            }

            var diagnosticos = await LoadDiagnosticosAsync(empresa, lectura.NoCuenta, cancellationToken);
            var notasMedicas = (await _apiClient.ObtenerNotasMedicasCuentaAsync(
                empresa,
                lectura.NoCuenta,
                cancellationToken)).ToList();

            var tieneAudio = await HasAudioAsync(empresa, consecutivo, lectura.TieneAudio, cancellationToken);

            var model = new LecturasDetalleViewModel
            {
                Lectura = lectura,
                Diagnosticos = diagnosticos,
                NotasMedicas = notasMedicas,
                TieneAudio = tieneAudio
            };

            return View(model);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al cargar detalle de lectura {Consecutivo}", consecutivo);
            return StatusCode(502, "No fue posible cargar el detalle del estudio.");
        }
    }

    [HttpGet]
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

    [HttpGet]
    public async Task<IActionResult> ObtenerAudio(
        long consecutivo,
        string empresa,
        CancellationToken cancellationToken)
    {
        if (consecutivo <= 0 || string.IsNullOrWhiteSpace(empresa))
        {
            return BadRequest("Parámetros inválidos.");
        }

        try
        {
            var (content, contentType) = await _apiClient.ObtenerAudioProgramacionAsync(
                empresa,
                consecutivo,
                cancellationToken);

            return File(content, contentType);
        }
        catch (EsculapioApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al obtener audio del estudio {Consecutivo}", consecutivo);
            return StatusCode(502, "No fue posible obtener el audio.");
        }
    }

    private async Task<LecturasViewModel> BuildViewModelAsync(
        LecturasViewModel model,
        CancellationToken cancellationToken)
    {
        await PopulateListsAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateListsAsync(LecturasViewModel model, CancellationToken cancellationToken)
    {
        model.EmpresasDisponibles = await LoadEmpresasAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.EmpresaSeleccionada))
        {
            model.MedicosDisponibles = await LoadMedicosPorEmpresaAsync(model.EmpresaSeleccionada, cancellationToken);
        }
        else if (model.EmpresasDisponibles.Count > 0)
        {
            model.EmpresaSeleccionada = model.EmpresasDisponibles[0].Codigo;
            model.MedicosDisponibles = await LoadMedicosPorEmpresaAsync(model.EmpresaSeleccionada, cancellationToken);
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
            if (radiologos.Any())
            {
                return radiologos
                    .Select(r => new MedicoDto(r.CedulaMedico, r.NombreMedico))
                    .OrderBy(m => m.Nombre)
                    .ToList();
            }
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogDebug(ex, "No se pudo consultar radiólogos registrados para empresa {Empresa}", empresa);
        }

        try
        {
            var medicos = await _apiClient.ObtenerMedicosAsync(
                codigoEmpresa: empresa,
                cancellationToken: cancellationToken);
            return medicos.OrderBy(m => m.Nombre).ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar médicos para empresa {Empresa}", empresa);
            return [];
        }
    }

    private async Task<List<DiagnosticoDto>> LoadDiagnosticosAsync(
        string empresa,
        decimal noCuenta,
        CancellationToken cancellationToken)
    {
        try
        {
            var diagnostico = await _apiClient.ObtenerDiagnosticoCuentaAsync(
                empresa,
                noCuenta,
                cancellationToken);

            return [diagnostico];
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar diagnóstico para cuenta {NoCuenta}", noCuenta);
            return [];
        }
    }

    private async Task<bool> HasAudioAsync(
        string empresa,
        long consecutivo,
        bool tieneAudioFlag,
        CancellationToken cancellationToken)
    {
        if (tieneAudioFlag)
        {
            return true;
        }

        try
        {
            var (content, _) = await _apiClient.ObtenerAudioProgramacionAsync(
                empresa,
                consecutivo,
                cancellationToken);

            return content.Length > 0;
        }
        catch (EsculapioApiException)
        {
            return false;
        }
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

    private void BindDateFilters(LecturasViewModel model)
    {
        if (Request.Form.TryGetValue(nameof(LecturasViewModel.FechaInicial), out var fechaInicialValue)
            && DateOnly.TryParseExact(
                fechaInicialValue.ToString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fechaInicial))
        {
            model.FechaInicial = fechaInicial;
            ModelState.Remove(nameof(LecturasViewModel.FechaInicial));
        }

        if (Request.Form.TryGetValue(nameof(LecturasViewModel.FechaFinal), out var fechaFinalValue)
            && DateOnly.TryParseExact(
                fechaFinalValue.ToString(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fechaFinal))
        {
            model.FechaFinal = fechaFinal;
            ModelState.Remove(nameof(LecturasViewModel.FechaFinal));
        }
    }
}
