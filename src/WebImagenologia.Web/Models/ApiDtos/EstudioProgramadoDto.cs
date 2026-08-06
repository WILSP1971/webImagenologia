namespace WebImagenologia.Web.Models.ApiDtos;

public record EstudioProgramadoDto(
    string Empresa,
    long Consecutivo,
    decimal NoCuenta,
    string CedulaMedico,
    DateOnly FechaProgramacion,
    string Servicio,
    string CodServicio,
    string Dependencia,
    decimal NoOrden,
    string UsuarioOperador,
    DateOnly FechaAsignacion,
    string Estado,
    string? Paciente = null,
    bool TieneAudio = false);
