using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class PortalRadiologosViewModel
{
    public string EmpresaSeleccionada { get; set; } = string.Empty;

    public string NombreEmpresa { get; set; } = string.Empty;

    public DateTime FechaHora { get; set; } = DateTime.Now;

    public List<EstudioProgramadoDto> EstudiosProgramados { get; set; } = [];

    public EstudioProgramadoDto? EstudioSeleccionado { get; set; }

    public List<DiagnosticoDto> Diagnosticos { get; set; } = [];

    public List<NotaMedicaDto> NotasMedicas { get; set; } = [];

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public bool TieneAudio { get; set; }
}
