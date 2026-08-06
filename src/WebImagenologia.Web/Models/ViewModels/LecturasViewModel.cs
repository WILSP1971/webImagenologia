using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class LecturasViewModel
{
    [Display(Name = "Empresa")]
    public string EmpresaSeleccionada { get; set; } = string.Empty;

    [Display(Name = "Fecha Inicial")]
    [DataType(DataType.Date)]
    public DateOnly FechaInicial { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Fecha Final")]
    [DataType(DataType.Date)]
    public DateOnly FechaFinal { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Radiólogo")]
    public string CedulaMedicoFiltro { get; set; } = string.Empty;

    [Display(Name = "Estado")]
    public string EstadoFiltro { get; set; } = "TODO";

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<MedicoDto> MedicosDisponibles { get; set; } = [];

    public List<LecturaDto> Lecturas { get; set; } = [];
}
