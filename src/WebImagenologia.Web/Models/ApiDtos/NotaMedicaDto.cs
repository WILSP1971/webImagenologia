namespace WebImagenologia.Web.Models.ApiDtos;

public record NotaMedicaDto(
    string Empresa,
    decimal NoCuenta,
    string? Nota,
    DateTime? Fecha);
