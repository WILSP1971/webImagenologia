namespace WebImagenologia.Web.Models.Visor;

/// <summary>Un estudio resuelto (proveniente de QIDO/C-FIND vía broker).</summary>
public sealed class EstudioDicomDto
{
    public string StudyInstanceUID { get; init; } = "";
    public string? AccessionNumber { get; init; }
    public string? PatientId { get; init; }
    public string? Modality { get; init; }
    public string? StudyDate { get; init; }
    public string? StudyDescription { get; init; }
    public int? NumberOfSeries { get; init; }
    public int? NumberOfInstances { get; init; }
}
