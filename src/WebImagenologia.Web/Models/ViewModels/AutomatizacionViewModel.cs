using System.ComponentModel.DataAnnotations;
using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Models.ViewModels;

public class AutomatizacionViewModel
{
    public int? IdAutomatizacion { get; set; }

    [Required(ErrorMessage = "Seleccione el tipo de programación")]
    [Display(Name = "Tipo de programación")]
    public string TipoProgramacion { get; set; } = "RAD";

    [Required(ErrorMessage = "Seleccione la frecuencia")]
    [Display(Name = "Frecuencia")]
    public string Frecuencia { get; set; } = "DIA";

    [Required(ErrorMessage = "Ingrese la hora de inicio")]
    [Display(Name = "Hora inicio")]
    public string HoraAutomatizacion { get; set; } = "06:00";

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public List<AutomatizacionDto> AutomatizacionesRegistradas { get; set; } = [];
}
