using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class OperadorRegistradoApiResponse
{
    [JsonPropertyName("CedulaOperador")]
    public string? CedulaOperador { get; set; }

    [JsonPropertyName("Cedula_Operador")]
    public string? Cedula_Operador { get; set; }

    [JsonPropertyName("NombreOperador")]
    public string? NombreOperador { get; set; }

    [JsonPropertyName("Nombre_Operador")]
    public string? Nombre_Operador { get; set; }

    [JsonPropertyName("UsuarioEsculapio")]
    public string? UsuarioEsculapio { get; set; }

    [JsonPropertyName("Usuario_Esculapio")]
    public string? Usuario_Esculapio { get; set; }

    [JsonPropertyName("Empresas")]
    public List<string>? Empresas { get; set; }

    [JsonPropertyName("Empresa")]
    public string? Empresa { get; set; }

    public OperadorRegistradoDto ToOperadorRegistradoDto()
    {
        var cedula = FirstNonEmpty(CedulaOperador, Cedula_Operador);
        var nombre = FirstNonEmpty(NombreOperador, Nombre_Operador);
        var usuario = FirstNonEmpty(UsuarioEsculapio, Usuario_Esculapio);
        var empresas = Empresas?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(Empresa))
        {
            empresas.Add(Empresa.Trim());
        }

        return new OperadorRegistradoDto(cedula, nombre, usuario, empresas);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
