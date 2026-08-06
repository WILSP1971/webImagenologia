using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class OperadorApiResponse
{
    [JsonPropertyName("Cedula")]
    public string? Cedula { get; set; }

    [JsonPropertyName("Cedula_Operador")]
    public string? CedulaOperador { get; set; }

    [JsonPropertyName("Nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("Nombre_Operador")]
    public string? NombreOperador { get; set; }

    public OperadorDto ToOperadorDto()
    {
        var cedula = FirstNonEmpty(Cedula, CedulaOperador);
        var nombre = FirstNonEmpty(Nombre, NombreOperador);
        return new OperadorDto(cedula, nombre);
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
