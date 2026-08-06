namespace WebImagenologia.Web.Models.ApiDtos;

public record EstudioEmpresaDto(
    string Empresa,
    string CodDependencia,
    decimal Cantidad,
    string Estado,
    string NombreDependencia = "",
    string NombreEmpresa = "");
