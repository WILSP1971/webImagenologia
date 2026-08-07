namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Orquesta la búsqueda (C-FIND) y la traída bajo demanda (C-MOVE) de un estudio desde el
/// PACS clásico (dcm4chee) hacia el Orthanc-gateway, antes de que el cliente DICOMweb pueda
/// consumirlo. Implementación real (protocolo DIMSE) es de SPEC-003, tras verificar en F0
/// la ruta de red PACS→Orthanc.
/// </summary>
public interface IOrthancGatewayService
{
    /// <summary>
    /// Solicita a Orthanc que traiga (cache) el estudio indicado desde el PACS de origen,
    /// si aún no está disponible localmente. Devuelve <c>true</c> si el estudio queda
    /// disponible (ya estaba o se trajo con éxito).
    /// </summary>
    Task<bool> AsegurarEstudioDisponibleAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default);
}
