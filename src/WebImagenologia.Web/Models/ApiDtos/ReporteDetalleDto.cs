namespace WebImagenologia.Web.Models.ApiDtos;

public record ReporteDetalleDto(
    string Empresa,
    long Consecutivo,
    decimal NoCuenta,
    string NombreMedico,
    string NombreServicio,
    string NombreDependencia,
    DateOnly FechaProgramacion,
    string Estado,
    bool TieneAudio);
