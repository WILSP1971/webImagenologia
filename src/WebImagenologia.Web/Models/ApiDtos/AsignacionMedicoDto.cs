namespace WebImagenologia.Web.Models.ApiDtos;

public record AsignacionMedicoDto(
    string Empresa,
    string CedulaMedico,
    string NombreMedico,
    string CodDependencia,
    string CodServicio,
    decimal Cantidad,
    string Estado,
    string NombreDependencia = "",
    string NombreServicio = "",
    string NombreEmpresa = "");
