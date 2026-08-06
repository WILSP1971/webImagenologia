using System.Net;

namespace WebImagenologia.Web.Services;

public class EsculapioApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }

    public EsculapioApiException(HttpStatusCode statusCode, string? responseBody)
        : base($"La API Esculapio respondió con {(int)statusCode} ({statusCode}).")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
