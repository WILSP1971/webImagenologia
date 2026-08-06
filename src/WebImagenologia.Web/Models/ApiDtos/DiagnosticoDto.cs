namespace WebImagenologia.Web.Models.ApiDtos;

public record DiagnosticoDto(
    string Empresa,
    decimal NoCuenta,
    string? Descripcion);
