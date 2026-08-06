using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class AsignacionViewModel
{
    [Required(ErrorMessage = "Seleccione una empresa")]
    [Display(Name = "Empresa")]
    public string Empresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un médico")]
    [Display(Name = "Médico")]
    public string CedulaMedico { get; set; } = string.Empty;

    [Display(Name = "Nombre médico")]
    public string NombreMedico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione una dependencia")]
    [Display(Name = "Dependencia")]
    public string CodDependencia { get; set; } = string.Empty;

    [Display(Name = "Nombre dependencia")]
    public string NombreDependencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un servicio")]
    [Display(Name = "Servicio")]
    public string CodServicio { get; set; } = string.Empty;

    [Display(Name = "Nombre servicio")]
    public string NombreServicio { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la cantidad de estudios")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
    [Display(Name = "Cantidad")]
    public decimal Cantidad { get; set; }

    [Required(ErrorMessage = "Seleccione el estado")]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "ACT";

    public bool ModoEdicion { get; set; }

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<MedicoDto> MedicosDisponibles { get; set; } = [];

    public List<DependenciaDto> DependenciasDisponibles { get; set; } = [];

    public List<ServicioDto> ServiciosDisponibles { get; set; } = [];

    public List<AsignacionMedicoDto> AsignacionesRegistradas { get; set; } = [];
}
