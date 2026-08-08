namespace WebImagenologia.Web.Services.Visor;

/// <summary>Construye la URL de lanzamiento de MedDream (ADR-002 / SPEC-004).</summary>
public interface IMedDreamLaunchService
{
    /// <summary>
    /// Devuelve la URL absoluta o relativa para abrir el estudio en MedDream.
    /// Null si MedDream no está habilitado/configurado.
    /// </summary>
    Task<string?> BuildViewerUrlAsync(string studyInstanceUid, CancellationToken cancellationToken = default);
}
