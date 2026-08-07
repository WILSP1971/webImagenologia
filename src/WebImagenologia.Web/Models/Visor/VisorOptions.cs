namespace WebImagenologia.Web.Models.Visor;

/// <summary>
/// Sección "Visor" de appsettings (SIN secretos).
/// TokenSecret, OrthancUser, OrthancPassword NO viven aquí -> User-Secrets/variable de entorno.
/// </summary>
public sealed class VisorOptions
{
    public string OrthancRestBaseUrl { get; init; } = "http://localhost:8042";
    public string OrthancDicomWebBaseUrl { get; init; } = "http://localhost:8042/PortalImagenologia/dicomweb";
    public string OrthancAet { get; init; } = "ESCULAPIO_ORTHANC";
    public int TokenMinutos { get; init; } = 10;
    public string ViewerBasePath { get; init; } = "/PortalImagenologia/visor";
}
