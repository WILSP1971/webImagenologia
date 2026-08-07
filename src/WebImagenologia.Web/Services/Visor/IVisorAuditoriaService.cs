namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Registra eventos de auditoría del módulo Visor (ABRIR, DESCARGAR, IMPRIMIR, MEDICION, EVENTO...).
/// En F1 la implementación es solo capa de log estructurado (ILogger). La persistencia en BD
/// (tabla/SP dedicados) se añade en SPEC-005/F4 sin romper este contrato.
/// </summary>
public interface IVisorAuditoriaService
{
    /// <summary>
    /// Registra un evento de auditoría.
    /// </summary>
    /// <param name="usuario">Login del usuario autenticado (nunca desde HttpContext.Session directo).</param>
    /// <param name="cedula">Cédula del radiólogo, para trazabilidad.</param>
    /// <param name="studyInstanceUID">Estudio DICOM asociado al evento.</param>
    /// <param name="accion">"ABRIR"|"DESCARGAR"|"IMPRIMIR"|"MEDICION"|"EVENTO"...</param>
    /// <param name="detalle">Detalle adicional opcional, sin PHI en claro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task RegistrarAsync(
        string usuario,
        string cedula,
        string studyInstanceUID,
        string accion,
        string? detalle,
        CancellationToken cancellationToken = default);
}
