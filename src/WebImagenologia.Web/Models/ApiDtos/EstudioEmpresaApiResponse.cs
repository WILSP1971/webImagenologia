using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class EstudioEmpresaApiResponse
{
    [JsonPropertyName("CodigoEmpresas")]
    public string? CodigoEmpresas { get; set; }

    [JsonPropertyName("Empresa")]
    public string? Empresa { get; set; }

    [JsonPropertyName("nombre_empresa")]
    public string? NombreEmpresa { get; set; }

    public string? CodDependencia { get; set; }

    [JsonPropertyName("cod_dependencia")]
    public string? CodDependenciaSnake { get; set; }

    public string? NombreDependencia { get; set; }

    [JsonPropertyName("nombre_dependencia")]
    public string? NombreDependenciaSnake { get; set; }

    [JsonPropertyName("Cantidad")]
    public decimal? Cantidad { get; set; }

    [JsonPropertyName("Estado")]
    public string? Estado { get; set; }

    public EstudioEmpresaDto ToEstudioEmpresaDto()
    {
        var codigoEmpresa = ApiMappingHelpers.FirstNonEmpty(CodigoEmpresas, Empresa);
        var codDependencia = ApiMappingHelpers.FirstNonEmpty(CodDependencia, CodDependenciaSnake);
        var nombreDependencia = ApiMappingHelpers.FirstNonEmpty(NombreDependencia, NombreDependenciaSnake);

        return new EstudioEmpresaDto(
            codigoEmpresa,
            codDependencia,
            Cantidad ?? 0,
            Estado ?? string.Empty,
            nombreDependencia,
            EmpresaDto.FormatoEtiqueta(codigoEmpresa, NombreEmpresa ?? string.Empty));
    }
}
