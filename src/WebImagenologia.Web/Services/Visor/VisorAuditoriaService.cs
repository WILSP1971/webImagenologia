namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Implementación F1: solo capa de log estructurado vía <see cref="ILogger"/>.
/// TODO(SPEC-005/F4): añadir persistencia en BD (tabla/SP de auditoría) sin romper el contrato
/// de <see cref="IVisorAuditoriaService"/>.
/// </summary>
public sealed class VisorAuditoriaService : IVisorAuditoriaService
{
    private readonly ILogger<VisorAuditoriaService> _logger;

    public VisorAuditoriaService(ILogger<VisorAuditoriaService> logger)
    {
        _logger = logger;
    }

    public Task RegistrarAsync(
        string usuario,
        string cedula,
        string studyInstanceUID,
        string accion,
        string? detalle,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditoriaVisor: usuario={Usuario} cedula={Cedula} studyInstanceUID={StudyInstanceUID} accion={Accion} detalle={Detalle}",
            usuario,
            cedula,
            studyInstanceUID,
            accion,
            detalle);

        return Task.CompletedTask;
    }
}
