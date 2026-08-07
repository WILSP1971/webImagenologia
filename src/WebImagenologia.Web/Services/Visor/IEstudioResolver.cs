using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Capa de mapeo Caso/Cuenta ↔ AccessionNumber/PatientID/StudyInstanceUID (SPEC-002 §4.5).
/// No asume relación 1:1 entre Caso/Cuenta y AccessionNumber (0008,0050); implementa
/// fallback a búsqueda por PatientID cuando el AccessionNumber no matchea en el PACS.
/// </summary>
public interface IEstudioResolver
{
    /// <summary>
    /// Resuelve estudios a partir de un criterio de búsqueda. Exactamente uno de
    /// <paramref name="caso"/> / <paramref name="identificacion"/> debe tener valor.
    /// </summary>
    /// <param name="caso">Caso/Cuenta clínico (se intenta mapear a AccessionNumber, con fallback a PatientID).</param>
    /// <param name="identificacion">Identificación (PatientID) del paciente; búsqueda directa por QIDO.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task<ResolverResponse> ResolverAsync(
        string? caso,
        string? identificacion,
        CancellationToken cancellationToken = default);
}
