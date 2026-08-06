using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

/// <summary>
/// Formato JSON real de GET /Usuarios/obtener-empresas.
/// Una sola propiedad por campo: PropertyNameCaseInsensitive enlaza variantes (Nombre_Empresa / nombre_empresa).
/// </summary>
internal sealed class EmpresaApiResponse
{
    [JsonPropertyName("Empresa")]
    public string? Empresa { get; set; }

    [JsonPropertyName("CodigoEmpresas")]
    public string? CodigoEmpresas { get; set; }

    [JsonPropertyName("nombre_empresa")]
    public string? NombreEmpresa { get; set; }

    public EmpresaDto ToEmpresaDto()
    {
        var codigo = ApiMappingHelpers.FirstNonEmpty(CodigoEmpresas, Empresa);
        return new EmpresaDto(codigo, NombreEmpresa ?? string.Empty);
    }
}
