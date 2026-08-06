using System.Text.Json.Serialization;

namespace WebImagenologia.Web.Models.ApiDtos;

internal sealed class LoginErrorApiResponse
{
    [JsonPropertyName("Message")]
    public string? Message { get; set; }
}
