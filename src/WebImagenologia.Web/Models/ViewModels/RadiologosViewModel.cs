using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class RadiologosViewModel
{
    [Required(ErrorMessage = "Seleccione un médico")]
    [Display(Name = "Médico")]
    public string CedulaMedico { get; set; } = string.Empty;

    [Display(Name = "Nombre médico")]
    public string NombreMedico { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el usuario Esculapio")]
    [Display(Name = "Usuario Esculapio")]
    public string UsuarioEsculapio { get; set; } = string.Empty;

    [Display(Name = "Empresas")]
    public List<string> EmpresasSeleccionadas { get; set; } = [];

    [Display(Name = "Nombre empresa")]
    public string NombreEmpresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione una dependencia")]
    [Display(Name = "Dependencia")]
    public string CodDependencia { get; set; } = string.Empty;

    [Display(Name = "Nombre dependencia")]
    public string NombreDependencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la cantidad de estudios")]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
    [Display(Name = "Cantidad de estudios")]
    public decimal Cantidad { get; set; }

    public bool EsLectura { get; set; } = true;
    public bool Tipo { get; set; } = true;

    public bool ModoEdicion { get; set; }
    public string EmpresaEdicion { get; set; } = string.Empty;
    public List<MedicoDto> MedicosDisponibles { get; set; } = [];

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<DependenciaDto> DependenciasDisponibles { get; set; } = [];

    public List<RadiologoRegistradoDto> RadiologosRegistrados { get; set; } = [];
}
