namespace WebImagenologia.Web.Models.Visor;

/// <summary>Contenido firmado del token (parte "payload" antes del HMAC).</summary>
public sealed class TokenPayload
{
    public string Usuario { get; init; } = "";
    public string Cedula { get; init; } = "";
    public string StudyInstanceUID { get; init; } = "";
    public long IssuedAtUnix { get; init; }
    public long ExpiresAtUnix { get; init; }

    /// <summary>Valor aleatorio anti-replay.</summary>
    public string Nonce { get; init; } = "";
}
