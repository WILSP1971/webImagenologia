namespace WebImagenologia.Web.Models.Visor;

/// <summary>Respuesta de GET /Visor/Resolver.</summary>
public sealed class ResolverResponse
{
    /// <summary>"caso" | "identificacion".</summary>
    public string CriterioBusqueda { get; init; } = "";
    public IReadOnlyList<EstudioDicomDto> Estudios { get; init; } = Array.Empty<EstudioDicomDto>();
}
