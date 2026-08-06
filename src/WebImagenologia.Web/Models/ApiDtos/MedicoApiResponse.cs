using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class MedicoApiResponse
{
    [JsonPropertyName("Cedula")]
    public string? Cedula { get; set; }

    [JsonPropertyName("Cedula_Medico")]
    public string? CedulaMedico { get; set; }

    [JsonPropertyName("Nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("Nombre_Medico")]
    public string? NombreMedico { get; set; }

    public MedicoDto ToMedicoDto()
    {
        var cedula = ApiMappingHelpers.FirstNonEmpty(Cedula, CedulaMedico);
        var nombre = ApiMappingHelpers.FirstNonEmpty(Nombre, NombreMedico);
        return new MedicoDto(cedula, nombre);
    }
}
