namespace WebImagenologia.Web.Models.ApiDtos;

public record LecturaDto(
    string Empresa,
    long Consecutivo,
    decimal NoCuenta,
    string CedulaMedico,
    string NombreMedico,
    DateOnly FechaProgramacion,
    DateOnly FechaAsignacion,
    string CodServicio,
    string NombreServicio,
    string Estado,
    bool TieneAudio);
