using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class EstudiosViewModel
{
    [Required(ErrorMessage = "Seleccione una dependencia")]
    [Display(Name = "Dependencia")]
    public string CodDependencia { get; set; } = string.Empty;

    [Display(Name = "Nombre dependencia")]
    public string NombreDependencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la cantidad de estudios")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
    [Display(Name = "Cantidad")]
    public decimal Cantidad { get; set; }

    [Display(Name = "Lectura")]
    public bool EsLectura { get; set; } = true;

    [Display(Name = "Empresas")]
    public List<string> EmpresasSeleccionadas { get; set; } = [];

    [Display(Name = "Nombre empresa")]
    public string NombreEmpresa { get; set; } = string.Empty;

    public bool ModoEdicion { get; set; }

    public string EmpresaEdicion { get; set; } = string.Empty;

    public List<DependenciaDto> DependenciasDisponibles { get; set; } = [];

    //public List<ServicioDto> ServiciosDisponibles { get; set; } = [];

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<EstudioEmpresaDto> EstudiosRegistrados { get; set; } = [];
}
