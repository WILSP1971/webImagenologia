namespace WebImagenologia.Web.Models.Domain;

public class EsculapioApiOptions
{
    public const string SectionName = "EsculapioApi";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}
