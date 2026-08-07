namespace WebImagenologia.Web.Models.Visor;

/// <summary>Cuerpo de POST /Visor/Auditoria (eventos del cliente).</summary>
public sealed class AuditoriaRequest
{
    public string StudyInstanceUID { get; init; } = "";

    /// <summary>"MEDICION"|"IMPRIMIR"|"DESCARGAR"|"EVENTO"...</summary>
    public string Accion { get; init; } = "";
    public string? Detalle { get; init; }
}
