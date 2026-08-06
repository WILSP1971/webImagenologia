namespace WebImagenologia.Web.Models.ApiDtos;

public record RadiologoRegistradoDto(
    string CedulaMedico,
    string NombreMedico,
    string UsuarioEsculapio,
    string CodDependencia,
    string NombreDependencia,
    decimal Cantidad,
    IReadOnlyList<string> Empresas,
    string NombreEmpresas = "");
