namespace WebImagenologia.Web.Models.ApiDtos;

public record RegistrarAutomatizacionRequest(
    int? IdAutomatizacion,
    string TipoProgramacion,
    string Frecuencia,
    string HoraAutomatizacion,
    string Estado);
