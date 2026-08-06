namespace WebImagenologia.Web.Models.Domain;

public class N8nOptions
{
    public const string SectionName = "N8n";

    public string WebhookUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 15;
}
