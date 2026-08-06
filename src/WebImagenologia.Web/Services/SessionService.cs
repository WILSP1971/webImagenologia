using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Services;

public class SessionService : ISessionService
{
    private const string ConnectionStringKey = "EncryptedConnectionString";
    private const string UsuarioKey = "EncryptedUsuario";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDataProtector _protector;

    public SessionService(
        IHttpContextAccessor httpContextAccessor,
        IDataProtectionProvider dataProtectionProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _protector = dataProtectionProvider.CreateProtector("WebImagenologia.Session");
    }

    private ISession Session =>
        _httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("No hay contexto HTTP disponible.");

    public void GuardarConnectionString(string connectionString)
    {
        var encrypted = _protector.Protect(connectionString);
        Session.SetString(ConnectionStringKey, encrypted);
    }

    public string? ObtenerConnectionString()
    {
        var encrypted = Session.GetString(ConnectionStringKey);
        return string.IsNullOrEmpty(encrypted) ? null : _protector.Unprotect(encrypted);
    }

    public void GuardarUsuario(UsuarioConexionDto usuario)
    {
        var json = JsonSerializer.Serialize(usuario);
        var encrypted = _protector.Protect(json);
        Session.SetString(UsuarioKey, encrypted);
    }

    public UsuarioConexionDto? ObtenerUsuario()
    {
        var encrypted = Session.GetString(UsuarioKey);
        if (string.IsNullOrEmpty(encrypted))
        {
            return null;
        }

        var json = _protector.Unprotect(encrypted);
        return JsonSerializer.Deserialize<UsuarioConexionDto>(json);
    }

    public void LimpiarSesion()
    {
        Session.Remove(ConnectionStringKey);
        Session.Remove(UsuarioKey);
    }
}
