namespace WebImagenologia.Web.Models.ApiDtos;

public record ServicioDto(
    string CodServicio,
    string NombreServicio,
    string CodDependencia,
    string CodEsquema);
