namespace WebImagenologia.Web.Models.Visor;

/// <summary>
/// Sección "Visor" de appsettings (SIN secretos).
/// TokenSecret, OrthancUser, OrthancPassword, MedDreamTokenServiceUser/Password
/// viven en User-Secrets / variables de entorno.
/// </summary>
public sealed class VisorOptions
{
    /// <summary>REST Orthanc (C-FIND/C-MOVE), p.ej. http://localhost:8042</summary>
    public string OrthancRestBaseUrl { get; init; } = "http://localhost:8042";

    /// <summary>
    /// Base DICOMweb QIDO/WADO. En Orthanc nativo suele ser /dicom-web (con guion).
    /// Tras proxy IIS puede ser /PortalImagenologia/dicomweb.
    /// </summary>
    public string OrthancDicomWebBaseUrl { get; init; } = "http://localhost:8042/dicom-web";

    /// <summary>AET Orthanc (máx. 16 chars en producción: ESCULAPIOORTHANC).</summary>
    public string OrthancAet { get; init; } = "ESCULAPIOORTHANC";

    /// <summary>Nombre de modality en orthanc.json apuntando al PACS (dcm4chee).</summary>
    public string OrthancPacsModality { get; init; } = "pacs";

    public int TokenMinutos { get; init; } = 10;

    /// <summary>Ruta del puente Abrir en la app (fallback si MedDream no está listo).</summary>
    public string ViewerBasePath { get; init; } = "/PortalImagenologia/Visor";

    /// <summary>Si true, POST /Visor/Token intenta generar URL MedDream.</summary>
    public bool MedDreamEnabled { get; init; }

    /// <summary>URL base del visor MedDream, p.ej. https://appsintranet.../meddream</summary>
    public string MedDreamViewerBaseUrl { get; init; } = "";

    /// <summary>URL base del TokenService, p.ej. http://localhost:8080</summary>
    public string MedDreamTokenServiceBaseUrl { get; init; } = "";

    /// <summary>Versión API TokenService: v1|v2|v3|v4 (default v4).</summary>
    public string MedDreamTokenApiVersion { get; init; } = "v4";

    /// <summary>Identificador de storage configurado en MedDream (Orthanc/PACS).</summary>
    public string MedDreamStorageId { get; init; } = "Orthanc";

    /// <summary>
    /// Solo laboratorio: permite Abrir con ?study=&amp;storage= si no hay TokenService.
    /// En producción debe ser false.
    /// </summary>
    public bool MedDreamAllowStudyQueryString { get; init; }
}
