using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

/// <summary>
/// Respuesta de GET Imagenologia/obtener-dependencias (y Diagnosticos/obtener-dependencias).
/// La API usa el campo "NomDependecia" (ortografía del backend).
/// </summary>
internal sealed class DependenciaApiResponse
{
    [JsonPropertyName("CodDependencia")]
    public string? CodDependencia { get; set; }

    [JsonPropertyName("cod_dependencia")]
    public string? CodDependenciaSnake { get; set; }

    [JsonPropertyName("NomDependecia")]
    public string? NomDependecia { get; set; }

    public string? NombreDependencia { get; set; }

    [JsonPropertyName("nombre_dependencia")]
    public string? NombreDependenciaSnake { get; set; }

    public DependenciaDto ToDependenciaDto()
    {
        var codigo = ApiMappingHelpers.FirstNonEmpty(CodDependencia, CodDependenciaSnake);
        var nombre = ApiMappingHelpers.FirstNonEmpty(
            NomDependecia,
            NombreDependencia,
            NombreDependenciaSnake);
        return new DependenciaDto(codigo, string.IsNullOrWhiteSpace(nombre) ? codigo : nombre);
    }
}
