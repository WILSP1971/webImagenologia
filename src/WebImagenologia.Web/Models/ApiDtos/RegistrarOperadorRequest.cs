namespace WebImagenologia.Web.Models.ApiDtos;

public record RegistrarOperadorRequest(
    string CedulaOperador,
    string NombreOperador,
    string UsuarioEsculapio,
    IReadOnlyList<string> Empresas);
