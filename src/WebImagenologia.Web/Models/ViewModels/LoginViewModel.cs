using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es requerido")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un servidor")]
    [Display(Name = "Servidor")]
    public string ServidorSeleccionado { get; set; } = string.Empty;

    public List<ServidorDto> Servidores { get; set; } = [];
}
