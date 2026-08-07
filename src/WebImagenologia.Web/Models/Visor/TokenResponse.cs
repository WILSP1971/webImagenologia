namespace WebImagenologia.Web.Models.Visor;

/// <summary>Respuesta de POST /Visor/Token.</summary>
public sealed class TokenResponse
{
    public string Token { get; init; } = "";
    public DateTimeOffset Expira { get; init; }

    /// <summary>Ruta relativa /PortalImagenologia/Visor/Abrir/{token}.</summary>
    public string ViewerUrl { get; init; } = "";
}
