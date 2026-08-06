namespace WebImagenologia.Web.Models.ApiDtos;

public record N8nSchedulePayload(
    string Frecuencia,
    string Hora,
    bool Activo,
    string TipoProgramacion);
