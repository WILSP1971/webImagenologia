using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebImagenologia.Web.Models.ApiDtos;
using WebImagenologia.Web.Models.Domain;
using WebImagenologia.Web.Models.ViewModels;
using WebImagenologia.Web.Services;

namespace WebImagenologia.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private const string LoginErrorMessage = "Usuario o contraseña incorrectos";
    private const string LoginRoleErrorMessage =
        "Su perfil de acceso no está autorizado en esta plataforma. Contacte al administrador.";

    private readonly IEsculapioApiClient _apiClient;
    private readonly ISessionService _sessionService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IEsculapioApiClient apiClient,
        ISessionService sessionService,
        ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
        _sessionService = sessionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        var model = new LoginViewModel();

        try
        {
            var servidores = await _apiClient.ObtenerServidoresAsync(cancellationToken);
            model.Servidores = servidores.ToList();
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "No se pudieron cargar los servidores de la API");
            ModelState.AddModelError(string.Empty, "No fue posible cargar la lista de servidores. Intente más tarde.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Servidores = (await LoadServidoresSafeAsync(cancellationToken)).ToList();
            return View(model);
        }

        var servidor = ServidorSelection.ParseKey(model.ServidorSeleccionado);
        if (servidor is null || string.IsNullOrWhiteSpace(servidor.IpConexion))
        {
            ModelState.AddModelError(nameof(model.ServidorSeleccionado), "El servidor seleccionado no es válido.");
            model.Servidores = (await LoadServidoresSafeAsync(cancellationToken)).ToList();
            return View(model);
        }

        try
        {
            var usuario = await _apiClient.ValidarConexionAsync(
                servidor.IpConexion,
                model.Usuario,
                model.Password,
                cancellationToken);

            var empresas = await _apiClient.ObtenerEmpresasAsync(
                servidor.IpConexion,
                servidor.BdConexion,
                servidor.PortConexion,
                model.Usuario,
                model.Password,
                cancellationToken);

            usuario = usuario with { EmpresasAsignadas = empresas };

            var rolNormalizado = RoleNormalizer.TryNormalize(usuario.Rol);
            if (rolNormalizado is null)
            {
                _logger.LogWarning(
                    "Rol no reconocido para usuario {Usuario}: {Rol}",
                    model.Usuario,
                    usuario.Rol);
                ModelState.AddModelError(string.Empty, LoginRoleErrorMessage);
                model.Servidores = (await LoadServidoresSafeAsync(cancellationToken)).ToList();
                return View(model);
            }

            var connectionInfo = new ServerConnectionInfo(
                servidor.IpConexion,
                servidor.BdConexion,
                servidor.PortConexion,
                model.Usuario,
                model.Password);

            var connectionJson = JsonSerializer.Serialize(connectionInfo);
            _sessionService.GuardarConnectionString(connectionJson);
            _sessionService.GuardarUsuario(usuario);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, usuario.NombreCompleto),
                new(ClaimTypes.NameIdentifier, usuario.Usuario),
                new(ClaimTypes.Role, rolNormalizado)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            return rolNormalizado switch
            {
                RoleNames.Radiologo => RedirectToAction("Index", "PortalRadiologos"),
                RoleNames.Operador => RedirectToAction("Index", "Lecturas"),
                _ => RedirectToAction("Index", "Home")
            };
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogWarning(ex, "Intento de login fallido para usuario {Usuario}", model.Usuario);
            ModelState.AddModelError(string.Empty, LoginErrorMessage);
            model.Servidores = (await LoadServidoresSafeAsync(cancellationToken)).ToList();
            return View(model);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        _sessionService.LimpiarSesion();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task<IEnumerable<ServidorDto>> LoadServidoresSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _apiClient.ObtenerServidoresAsync(cancellationToken);
        }
        catch (EsculapioApiException ex)
        {
            _logger.LogError(ex, "No se pudieron recargar los servidores");
            return [];
        }
    }

    internal static string NormalizeRole(string rol) =>
        RoleNormalizer.TryNormalize(rol) ?? rol;
}
