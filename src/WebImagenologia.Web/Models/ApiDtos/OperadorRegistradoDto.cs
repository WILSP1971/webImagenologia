namespace WebImagenologia.Web.Models.ApiDtos;

public record OperadorRegistradoDto(
    string CedulaOperador,
    string NombreOperador,
    string UsuarioEsculapio,
    IReadOnlyList<string> Empresas);
