namespace WebImagenologia.Web.Models.Visor;

/// <summary>Cuerpo de POST /Visor/Token.</summary>
public sealed class TokenRequest
{
    public string StudyInstanceUID { get; init; } = "";
}
