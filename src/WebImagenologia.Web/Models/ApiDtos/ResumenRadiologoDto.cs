namespace WebImagenologia.Web.Models.ApiDtos;

public record ResumenRadiologoDto(
    string CedulaMedico,
    string NombreMedico,
    int TotalAsignados,
    int TotalLeidos,
    int TotalPendientes);
