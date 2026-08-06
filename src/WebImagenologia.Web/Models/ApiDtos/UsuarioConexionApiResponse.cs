using System.Text.Json.Serialization;
using WebImagenologia.Web.Models.Domain;

namespace WebImagenologia.Web.Models.ApiDtos;

/// <summary>
/// Formato JSON real de GET /Usuarios/obtener-validaconexion (éxito: array; error: string u objeto).
/// </summary>
internal sealed class UsuarioConexionApiResponse
{
    [JsonPropertyName("Username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("Rol")]
    public string? Rol { get; set; }

    [JsonPropertyName("Perfil")]
    public string? Perfil { get; set; }

    public string ResolveRawRole() =>
        !string.IsNullOrWhiteSpace(Role) ? Role
        : !string.IsNullOrWhiteSpace(Rol) ? Rol
        : Perfil ?? string.Empty;

    public UsuarioConexionDto ToUsuarioConexionDto(IEnumerable<EmpresaDto> empresas)
    {
        var usuario = !string.IsNullOrWhiteSpace(Username) ? Username.Trim() : Nombre.Trim();
        var nombreCompleto = !string.IsNullOrWhiteSpace(Nombre) ? Nombre.Trim() : usuario;
        var rolNormalizado = RoleNormalizer.TryNormalize(ResolveRawRole())
            ?? ResolveRawRole().Trim();

        return new UsuarioConexionDto(usuario, nombreCompleto, rolNormalizado, empresas);
    }
}
