namespace WebImagenologia.Web.Models.ApiDtos;

public record ToggleAutomatizacionEstadoRequest(
    int IdAutomatizacion,
    string Estado);
