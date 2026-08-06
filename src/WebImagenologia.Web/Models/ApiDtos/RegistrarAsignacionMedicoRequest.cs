namespace WebImagenologia.Web.Models.ApiDtos;

public record RegistrarAsignacionMedicoRequest(
    string Empresa,
    string CedulaMedico,
    string CodDependencia,
    string CodServicio,
    decimal Cantidad,
    string Estado);
