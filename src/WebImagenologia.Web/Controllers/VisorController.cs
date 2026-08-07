using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.Visor;
using WebImagenologia.Web.Services;
using WebImagenologia.Web.Services.Visor;

namespace WebImagenologia.Web.Controllers;

/// <summary>
/// Broker de seguridad del módulo Visor (SPEC-002). Resuelve el estudio, autoriza por rol,
/// emite tokens cortos firmados, audita y expone las 5 APIs REST del broker.
/// Usuario/rol/cédula se obtienen exclusivamente de <see cref="ISessionService"/>
/// (prohibido <c>HttpContext.Session.GetString(...)</c> directo).
/// </summary>
[Authorize(Roles = RoleNames.Policies.AdministradorOrRadiologo)]
public class VisorController : Controller
{
    private static readonly HashSet<string> AccionesAuditoriaValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "MEDICION",
        "IMPRIMIR",
        "DESCARGAR",
        "EVENTO",
        "ABRIR"
    };

    private readonly IEstudioResolver _estudioResolver;
    private readonly IVisorTokenService _tokenService;
    private readonly IVisorAuditoriaService _auditoriaService;
    private readonly IDicomWebClient _dicomWebClient;
    private readonly ISessionService _sessionService;
    private readonly VisorOptions _visorOptions;
    private readonly ILogger<VisorController> _logger;

    public VisorController(
        IEstudioResolver estudioResolver,
        IVisorTokenService tokenService,
        IVisorAuditoriaService auditoriaService,
        IDicomWebClient dicomWebClient,
        ISessionService sessionService,
        IOptions<VisorOptions> visorOptions,
        ILogger<VisorController> logger)
    {
        _estudioResolver = estudioResolver;
        _tokenService = tokenService;
        _auditoriaService = auditoriaService;
        _dicomWebClient = dicomWebClient;
        _sessionService = sessionService;
        _visorOptions = visorOptions.Value;
        _logger = logger;
    }

    /// <summary>GET /Visor/Resolver?caso=... | ?identificacion=...</summary>
    [HttpGet]
    public async Task<IActionResult> Resolver(
        string? caso,
        string? identificacion,
        CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return Unauthorized();
        }

        var tieneCaso = !string.IsNullOrWhiteSpace(caso);
        var tieneIdentificacion = !string.IsNullOrWhiteSpace(identificacion);

        if (tieneCaso == tieneIdentificacion)
        {
            return BadRequest("Debe especificar exactamente uno de los criterios: 'caso' o 'identificacion'.");
        }

        try
        {
            var resultado = await _estudioResolver.ResolverAsync(caso, identificacion, cancellationToken);

            if (resultado.Estudios.Count == 0)
            {
                return NotFound("No se encontraron estudios para el criterio indicado.");
            }

            return Ok(resultado);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al resolver estudio (caso={Caso}, identificacion={Identificacion})", caso, identificacion);
            return StatusCode(StatusCodes.Status502BadGateway, "No fue posible consultar el PACS.");
        }
    }

    /// <summary>
    /// POST /Visor/Token — { studyInstanceUID }.
    /// Contrato con el cliente JS (SPEC-004): al llevar <see cref="ValidateAntiForgeryTokenAttribute"/>
    /// y recibir el cuerpo como JSON ([FromBody]), el filtro de antiforgery de ASP.NET Core NO lee
    /// el body — el cliente debe enviar el token en el header <c>RequestVerificationToken</c> con el
    /// valor obtenido de <c>@Html.AntiForgeryToken()</c> en cada POST, o la petición será rechazada
    /// con 400 antes de llegar a esta acción.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Token([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.StudyInstanceUID))
        {
            return BadRequest("StudyInstanceUID es obligatorio.");
        }

        try
        {
            // TODO(SPEC-005/F4): autorización por caso (validar que el radiólogo en sesión
            // tiene asignado el estudio/caso antes de emitir el token).
            var ahora = DateTimeOffset.UtcNow;

            var payload = new TokenPayload
            {
                Usuario = usuario.Usuario,
                // TODO(SPEC-005/F4): "Cedula" usa el login como placeholder — UsuarioConexionDto
                // no expone la cedula real del radiologo. Sustituir cuando la API clinica la exponga.
                Cedula = usuario.Usuario,
                StudyInstanceUID = request.StudyInstanceUID,
                IssuedAtUnix = ahora.ToUnixTimeSeconds(),
                ExpiresAtUnix = ahora.AddMinutes(_visorOptions.TokenMinutos).ToUnixTimeSeconds(),
                Nonce = Guid.NewGuid().ToString("N")
            };

            var token = _tokenService.Emitir(payload);
            var expira = DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAtUnix);

            await _auditoriaService.RegistrarAsync(
                usuario.Usuario,
                payload.Cedula,
                request.StudyInstanceUID,
                "ABRIR",
                detalle: null,
                cancellationToken);

            var respuesta = new TokenResponse
            {
                Token = token,
                Expira = expira,
                ViewerUrl = $"{_visorOptions.ViewerBasePath.TrimEnd('/')}/Abrir/{token}"
            };

            return Ok(respuesta);
        }
        catch (VisorNoConfiguradoException ex)
        {
            _logger.LogCritical(ex, "Módulo Visor mal configurado: falta Visor:TokenSecret.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "El módulo de visualización no está disponible temporalmente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al emitir token para estudio {StudyInstanceUID}", request?.StudyInstanceUID);
            return StatusCode(StatusCodes.Status502BadGateway, "No fue posible emitir el token de acceso.");
        }
    }

    /// <summary>GET /Visor/Abrir/{token}</summary>
    [HttpGet]
    public IActionResult Abrir(string token)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return Unauthorized();
        }

        TokenPayload? payload;
        try
        {
            if (!_tokenService.TryValidar(token, out payload) || payload is null)
            {
                return View("TokenInvalido");
            }
        }
        catch (VisorNoConfiguradoException ex)
        {
            _logger.LogCritical(ex, "Módulo Visor mal configurado: falta Visor:TokenSecret.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "El módulo de visualización no está disponible temporalmente.");
        }

        // Anti-uso cruzado: el usuario del payload debe coincidir con el de la sesión actual.
        if (!string.Equals(payload.Usuario, usuario.Usuario, StringComparison.OrdinalIgnoreCase))
        {
            return View("TokenInvalido");
        }

        // Vista placeholder de F1: el embebido real de OHIF es de SPEC-004.
        return View(payload);
    }

    /// <summary>GET /Visor/Preview?studyUid=&amp;seriesUid=&amp;instanceUid=&amp;frame=&amp;formato=</summary>
    [HttpGet]
    public async Task<IActionResult> Preview(
        string studyUid,
        string seriesUid,
        string instanceUid,
        int? frame,
        string? formato,
        CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(studyUid) ||
            string.IsNullOrWhiteSpace(seriesUid) ||
            string.IsNullOrWhiteSpace(instanceUid))
        {
            return BadRequest("studyUid, seriesUid e instanceUid son obligatorios.");
        }

        var formatoNormalizado = string.IsNullOrWhiteSpace(formato) ? "jpg" : formato.ToLowerInvariant();
        if (formatoNormalizado is not ("jpg" or "png"))
        {
            return BadRequest("formato debe ser 'jpg' o 'png'.");
        }

        try
        {
            var bytes = await _dicomWebClient.ObtenerRenderedInstanceAsync(
                studyUid,
                seriesUid,
                instanceUid,
                frame,
                formatoNormalizado,
                cancellationToken);

            if (bytes is null)
            {
                return NotFound();
            }

            // TODO(SPEC-005/F4): "Cedula" usa el login como placeholder — UsuarioConexionDto
            // no expone la cedula real del radiologo. Sustituir cuando la API clinica la exponga.
            await _auditoriaService.RegistrarAsync(
                usuario.Usuario,
                usuario.Usuario,
                studyUid,
                "DESCARGAR",
                detalle: $"series={seriesUid};instance={instanceUid}",
                cancellationToken);

            var contentType = formatoNormalizado == "png" ? "image/png" : "image/jpeg";
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener preview de instancia {InstanceUid}", instanceUid);
            return StatusCode(StatusCodes.Status502BadGateway, "No fue posible obtener la imagen.");
        }
    }

    /// <summary>
    /// POST /Visor/Auditoria — { studyInstanceUID, accion, detalle }.
    /// Contrato con el cliente JS (SPEC-004): al llevar <see cref="ValidateAntiForgeryTokenAttribute"/>
    /// y recibir el cuerpo como JSON ([FromBody]), el filtro de antiforgery de ASP.NET Core NO lee
    /// el body — el cliente debe enviar el token en el header <c>RequestVerificationToken</c> con el
    /// valor obtenido de <c>@Html.AntiForgeryToken()</c> en cada POST, o la petición será rechazada
    /// con 400 antes de llegar a esta acción.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Auditoria([FromBody] AuditoriaRequest request, CancellationToken cancellationToken)
    {
        var usuario = _sessionService.ObtenerUsuario();
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.Accion) ||
            !AccionesAuditoriaValidas.Contains(request.Accion))
        {
            return BadRequest("Acción de auditoría inválida.");
        }

        // TODO(SPEC-005/F4): "Cedula" usa el login como placeholder — UsuarioConexionDto
        // no expone la cedula real del radiologo. Sustituir cuando la API clinica la exponga.
        await _auditoriaService.RegistrarAsync(
            usuario.Usuario,
            usuario.Usuario,
            request.StudyInstanceUID,
            request.Accion,
            request.Detalle,
            cancellationToken);

        return NoContent();
    }
}
