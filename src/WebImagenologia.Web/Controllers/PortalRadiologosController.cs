using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[Authorize(Roles = RoleNames.Policies.AdministradorOrRadiologo)]
public class PortalRadiologosController : Controller
{
    private const string EstadoLeido = "LEIDO";

    private readonly IEsculapioApiClient _apiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<PortalRadiologosController> _logger;

    public PortalRadiologosController(
        IEsculapioApiClient apiClient,
        ISessionService sessionService,
        ILogger<PortalRadiologosController> logger)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? empresa, CancellationToken cancellationToken)
    {
        var model = await BuildViewModelAsync(empresa, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EstudiosPorEmpresa(string empresa, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(empresa))
        {
            return Json(Array.Empty<EstudioProgramadoDto>());
        }

        try
        {
            var estudios = await LoadEstudiosProgramadosAsync(empresa, cancellationToken);
            return Json(estudios);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar estudios para empresa {Empresa}", empresa);
            return Json(Array.Empty<EstudioProgramadoDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> DetalleEstudio(
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
            var estudios = await LoadEstudiosProgramadosAsync(empresa, cancellationToken);
            var estudio = estudios.FirstOrDefault(e => e.Consecutivo == consecutivo);

            if (estudio is null)
            {
                return NotFound("Estudio no encontrado.");
            }

            var diagnosticos = await LoadDiagnosticosAsync(empresa, estudio.NoCuenta, cancellationToken);
            var notasMedicas = await _apiClient.ObtenerNotasMedicasCuentaAsync(
                empresa,
                estudio.NoCuenta,
                cancellationToken);
            var hasAudio = await HasAudioAsync(empresa, consecutivo, estudio.TieneAudio, cancellationToken);

            return Json(new
            {
                estudio,
                diagnosticos,
                notasMedicas,
                hasAudio
            });
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al cargar detalle del estudio {Consecutivo}", consecutivo);
            return StatusCode(502, "No fue posible cargar el detalle del estudio.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(AudioValidation.MaxSizeBytes + (512 * 1024))]
    public async Task<IActionResult> SubirAudio(
        IFormFile archivo,
        long consecutivo,
        string empresa,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest("Debe seleccionar un archivo de audio.");
        }

        if (consecutivo <= 0 || string.IsNullOrWhiteSpace(empresa))
        {
            return BadRequest("Parámetros inválidos.");
        }

        if (!AudioValidation.IsAllowedFile(archivo.FileName, archivo.ContentType))
        {
            return BadRequest("Formato de audio no permitido.");
        }

        if (!AudioValidation.IsAllowedSize(archivo.Length))
        {
            return BadRequest("El archivo supera el límite de 25 MB.");
        }

        try
        {
            await using var stream = archivo.OpenReadStream();
            await _apiClient.SubirAudioProgramacionAsync(
                empresa,
                consecutivo,
                stream,
                archivo.ContentType,
                archivo.FileName,
                cancellationToken);

            return Ok(new { message = "Audio guardado correctamente." });
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al subir audio para estudio {Consecutivo}", consecutivo);
            return StatusCode(502, "No fue posible guardar el audio.");
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarAudio(
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
            await _apiClient.EliminarAudioProgramacionAsync(empresa, consecutivo, cancellationToken);
            return Ok(new { message = "Audio eliminado correctamente." });
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "Error al eliminar audio del estudio {Consecutivo}", consecutivo);
            return StatusCode(502, "No fue posible eliminar el audio.");
        }
    }

    private async Task<PortalRadiologosViewModel> BuildViewModelAsync(
        string? empresaSeleccionada,
        CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        var empresas = usuario?.EmpresasAsignadas?.ToList() ?? [];

        var model = new PortalRadiologosViewModel
        {
            FechaHora = DateTime.Now,
            EmpresasDisponibles = empresas
        };

        if (empresas.Count == 0)
        {
            return model;
        }

        var empresa = string.IsNullOrWhiteSpace(empresaSeleccionada)
            ? empresas[0].Codigo
            : empresaSeleccionada;

        model.EmpresaSeleccionada = empresa;
        model.NombreEmpresa = empresas
            .FirstOrDefault(e => e.Codigo.Equals(empresa, StringComparison.OrdinalIgnoreCase))
            ?.Nombre ?? empresa;

        try
        {
            model.EstudiosProgramados = await LoadEstudiosProgramadosAsync(empresa, cancellationToken);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "No fue posible cargar estudios programados para empresa {Empresa}", empresa);
            model.EstudiosProgramados = [];
        }

        return model;
    }

    private async Task<List<EstudioProgramadoDto>> LoadEstudiosProgramadosAsync(
        string empresa,
        CancellationToken cancellationToken)
    {
        var cedulaMedico = await ResolveCedulaMedicoAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(cedulaMedico))
        {
            _logger.LogWarning("No se pudo resolver la cédula del radiólogo en sesión.");
            return [];
        }

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var estudios = await _apiClient.ObtenerEstudiosProgramadosAsync(
            empresa,
            cedulaMedico,
            hoy,
            cancellationToken);

        return estudios
            .Where(e => e.FechaProgramacion <= hoy)
            .Where(e => !e.Estado.Equals(EstadoLeido, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.FechaProgramacion)
            .ThenBy(e => e.NoCuenta)
            .ToList();
    }

    private async Task<string?> ResolveCedulaMedicoAsync(CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return null;
        }

        var empresas = usuario.EmpresasAsignadas?.ToList() ?? [];
        foreach (var empresa in empresas)
        {
            try
            {
                var radiologos = await _apiClient.ObtenerRadiologosRegistradosAsync(
                    codigoEmpresa: empresa.Codigo,
                    cancellationToken: cancellationToken);

                var match = radiologos.FirstOrDefault(r =>
                    r.UsuarioEsculapio.Equals(usuario.Usuario, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    return match.CedulaMedico;
                }
            }
            catch (EsculapioApiException ex)
            {
                _logger.LogDebug(ex, "No se pudo consultar radiólogos para empresa {Empresa}", empresa.Codigo);
            }
        }

        return usuario.Usuario;
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
}
