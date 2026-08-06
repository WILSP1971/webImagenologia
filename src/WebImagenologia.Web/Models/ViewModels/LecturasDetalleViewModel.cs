using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class LecturasDetalleViewModel
{
    public LecturaDto Lectura { get; set; } = null!;

    public List<DiagnosticoDto> Diagnosticos { get; set; } = [];

    public List<NotaMedicaDto> NotasMedicas { get; set; } = [];

    public bool TieneAudio { get; set; }
}
