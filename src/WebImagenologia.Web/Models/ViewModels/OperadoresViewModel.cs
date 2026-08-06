using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class OperadoresViewModel
{
    [Required(ErrorMessage = "Seleccione un operador")]
    [Display(Name = "Operador")]
    public string CedulaOperador { get; set; } = string.Empty;

    [Display(Name = "Nombre operador")]
    public string NombreOperador { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese el usuario Esculapio")]
    [Display(Name = "Usuario Esculapio")]
    public string UsuarioEsculapio { get; set; } = string.Empty;

    [Display(Name = "Empresas")]
    public List<string> EmpresasSeleccionadas { get; set; } = [];

    public bool ModoEdicion { get; set; }

    public List<OperadorDto> OperadoresDisponibles { get; set; } = [];

    public List<EmpresaDto> EmpresasDisponibles { get; set; } = [];

    public List<OperadorRegistradoDto> OperadoresRegistrados { get; set; } = [];
}
