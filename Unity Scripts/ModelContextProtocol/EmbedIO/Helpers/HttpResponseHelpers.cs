using EmbedIO;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;

public static class HttpResponseHelpers
{
    public static async Task SendErrorAsync(IHttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.SendStringAsync(message, "text/plain", Encoding.UTF8);
    }

    public static void ConfigureSseHeaders(IHttpResponse response)
    {
        response.ContentType = "text/event-stream";
        response.ContentEncoding = Encoding.UTF8;
        response.SendChunked = true;
        response.KeepAlive = true;
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Content-Encoding"] = "identity";
    }
    public static bool IsJsonRpcRequest(JToken token)
    {
        return token.Type == JTokenType.Object
            && token["jsonrpc"]?.ToString() == "2.0"
            && token["method"] != null
            && token["id"] != null;
    }

}
