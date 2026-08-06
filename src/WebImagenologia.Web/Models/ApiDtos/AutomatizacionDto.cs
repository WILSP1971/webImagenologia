namespace WebImagenologia.Web.Models.ApiDtos;

public record AutomatizacionDto(
    int IdAutomatizacion,
    string TipoProgramacion,
    string Frecuencia,
    string HoraAutomatizacion,
    string Estado);
