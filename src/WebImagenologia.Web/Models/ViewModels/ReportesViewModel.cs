using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class ReportesViewModel
{
    [Display(Name = "Empresas")]
    public List<string> EmpresasSeleccionadas { get; set; } = [];

    [Display(Name = "Radiólogo")]
    public string CedulaMedico { get; set; } = string.Empty;

    [Display(Name = "Servicio")]
    public string CodServicio { get; set; } = string.Empty;

    [Display(Name = "Dependencia")]
    public string CodDependencia { get; set; } = string.Empty;

    [Display(Name = "Fecha Inicial")]
    [DataType(DataType.Date)]
    public DateOnly FechaInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-30));

    [Display(Name = "Fecha Final")]
    [DataType(DataType.Date)]
    public DateOnly FechaFinal { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Estado")]
    public string Estado { get; set; } = "TODO";

    [Display(Name = "Tipo de Reporte")]
    public string TipoReporte { get; set; } = "DETALLE";

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<MedicoDto> MedicosDisponibles { get; set; } = [];

    public List<ServicioDto> ServiciosDisponibles { get; set; } = [];

    public List<DependenciaDto> DependenciasDisponibles { get; set; } = [];

    public List<ReporteDetalleDto> DetalleEstudios { get; set; } = [];

    public List<ResumenRadiologoDto> ResumenRadiologos { get; set; } = [];

    public List<EstudioSinResultadoDto> EstudiosSinResultado { get; set; } = [];
}
