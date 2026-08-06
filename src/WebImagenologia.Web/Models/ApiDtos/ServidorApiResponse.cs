using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

/// <summary>
/// Formato JSON real de GET /Usuarios/obtener-servidores (API Esculapio).
/// </summary>
internal sealed class ServidorApiResponse
{
    [JsonPropertyName("Descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("Ip_Conexion")]
    public string Ip_Conexion { get; set; } = string.Empty;

    [JsonPropertyName("BaseDatos")]
    public string BaseDatos { get; set; } = string.Empty;

    [JsonPropertyName("Puerto")]
    public int Puerto { get; set; }

    public ServidorDto ToServidorDto() =>
        new(
            Descripcion,
            Ip_Conexion,
            BaseDatos,
            Puerto.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
