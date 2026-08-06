namespace WebImagenologia.Web.Models.ApiDtos;

public record EstudioSinResultadoDto(
    string Empresa,
    decimal NoCuenta,
    string NoOrden,
    string Servicio,
    string Dependencia,
    DateOnly FechaOrden);
