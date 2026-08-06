namespace WebImagenologia.Web.Models.ApiDtos;


public record RegistrarRadiologoRequest(
    string Empresa,
    string CedulaMedico,
    string UsuarioEsculapio,
    string CodDependencia,
    decimal Cantidad,
    string Estado,
    string tipo);

