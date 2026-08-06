using WebImagenologia.Web.Models.ApiDtos;

namespace WebImagenologia.Web.Services;

public interface IN8nWebhookClient
{
    Task<bool> NotifyScheduleUpdateAsync(
        N8nSchedulePayload payload,
        CancellationToken cancellationToken = default);
}
