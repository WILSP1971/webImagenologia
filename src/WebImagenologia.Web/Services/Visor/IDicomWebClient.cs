using WebImagenologia.Web.Models.Visor;

namespace WebImagenologia.Web.Services.Visor;

/// <summary>
/// Cliente DICOMweb (QIDO-RS / WADO-RS) contra el gateway Orthanc.
/// La implementación real (SPEC-003) requiere la cadena Orthanc→dcm4chee desplegada y
/// verificada en F0; hasta entonces se usa un stub inyectable vía DI para permitir que
/// el broker (Resolver/Token/Preview) compile y sea testeable con mocks.
/// </summary>
public interface IDicomWebClient
{
    /// <summary>Busca estudios (QIDO-RS) por AccessionNumber (0008,0050).</summary>
    Task<IReadOnlyList<EstudioDicomDto>> BuscarPorAccessionNumberAsync(
        string accessionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>Busca estudios (QIDO-RS) por PatientID (identificación del paciente).</summary>
    Task<IReadOnlyList<EstudioDicomDto>> BuscarPorPatientIdAsync(
        string patientId,
        CancellationToken cancellationToken = default);

    /// <summary>Busca un estudio (QIDO-RS) por StudyInstanceUID.</summary>
    Task<IReadOnlyList<EstudioDicomDto>> BuscarPorStudyInstanceUidAsync(
        string studyInstanceUid,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene el render (WADO-RS rendered) de una instancia como JPEG/PNG.</summary>
    Task<byte[]?> ObtenerRenderedInstanceAsync(
        string studyInstanceUid,
        string seriesInstanceUid,
        string sopInstanceUid,
        int? frame,
        string formato,
        CancellationToken cancellationToken = default);
}
