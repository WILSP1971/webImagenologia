using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class RadiologoRegistradoApiResponse
{
    [JsonPropertyName("CedulaMedico")]
    public string? CedulaMedico { get; set; }

    [JsonPropertyName("Cedula_Medico")]
    public string? Cedula_Medico { get; set; }

    [JsonPropertyName("NombreMedico")]
    public string? NombreMedico { get; set; }

    [JsonPropertyName("Nombre_Medico")]
    public string? Nombre_Medico { get; set; }

    [JsonPropertyName("UsuarioEsculapio")]
    public string? UsuarioEsculapio { get; set; }

    [JsonPropertyName("Usuario_Esculapio")]
    public string? Usuario_Esculapio { get; set; }

    [JsonPropertyName("Empresas")]
    public List<string>? Empresas { get; set; }

    [JsonPropertyName("Empresa")]
    public string? Empresa { get; set; }

    [JsonPropertyName("CodigoEmpresas")]
    public string? CodigoEmpresas { get; set; }

    [JsonPropertyName("codigoEmp")]
    public string? CodigoEmp { get; set; }

    [JsonPropertyName("nombre_empresa")]
    public string? NombreEmpresa { get; set; }

    [JsonPropertyName("CodDependencia")]
    public string? CodDependencia { get; set; }

    [JsonPropertyName("Cod_Dependencia")]
    public string? Cod_Dependencia { get; set; }

    [JsonPropertyName("NombreDependencia")]
    public string? NombreDependencia { get; set; }

    [JsonPropertyName("Nombre_Dependencia")]
    public string? Nombre_Dependencia { get; set; }

    [JsonPropertyName("Cantidad")]
    public decimal? Cantidad { get; set; }

    public RadiologoRegistradoDto ToRadiologoRegistradoDto()
    {
        var cedula = ApiMappingHelpers.FirstNonEmpty(CedulaMedico, Cedula_Medico);
        var nombre = ApiMappingHelpers.FirstNonEmpty(NombreMedico, Nombre_Medico);
        var usuario = ApiMappingHelpers.FirstNonEmpty(UsuarioEsculapio, Usuario_Esculapio);
        var codDep = ApiMappingHelpers.FirstNonEmpty(CodDependencia, Cod_Dependencia);
        var nombreDep = ApiMappingHelpers.FirstNonEmpty(NombreDependencia, Nombre_Dependencia);
        var cantidad = Cantidad ?? 0;
        var codigoEmpresa = ApiMappingHelpers.FirstNonEmpty(CodigoEmpresas, Empresa, CodigoEmp);
        var empresas = Empresas?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            empresas.Add(codigoEmpresa);
        }

        var nombreEmpresa = ApiMappingHelpers.FirstNonEmpty(NombreEmpresa);
        if (string.IsNullOrWhiteSpace(nombreEmpresa) && !string.IsNullOrWhiteSpace(codigoEmpresa))
        {
            nombreEmpresa = EmpresaDto.FormatoEtiqueta(codigoEmpresa, string.Empty);
        }

        return new RadiologoRegistradoDto(
            cedula,
            nombre,
            usuario,
            codDep,
            nombreDep,
            cantidad,
            empresas.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            nombreEmpresa);
    }
}
