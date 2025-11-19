using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using ModelContextProtocol.Server;
using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static HttpResponseHelpers;
using static SessionHelpers;

public sealed class StreamableHttpApiController : WebApiController
{
    private readonly EndpointSessionState _state;
    private readonly string _routePrefix;

    public StreamableHttpApiController(string routePrefix, EndpointSessionState state)
    {
        _routePrefix = routePrefix;
        _state = state;
    }

    [Route(HttpVerbs.Post, "/")]
    public async Task HandleStreamablePostAsync()
    {
        var request = HttpContext.Request;
        var response = HttpContext.Response;

        var acceptHeader = request.Headers["Accept"];
        if (string.IsNullOrWhiteSpace(acceptHeader) ||
            !(acceptHeader.Contains("application/json") && acceptHeader.Contains("text/event-stream")))
        {
            await SendErrorAsync(HttpContext, 406, "Client must accept both application/json and text/event-stream.");
            return;
        }

        var sessionId = request.Headers["mcp-session-id"] ?? GenerateSessionId();

        if (!_state.HttpSessions.TryGetValue(sessionId, out var session))
        {
            var user = HttpContext.User as ClaimsPrincipal ?? new ClaimsPrincipal();
            var transport = new StreamableHttpServerTransport();

            session = new HttpMcpSession<StreamableHttpServerTransport>(sessionId, transport, user);

            var serverOptions = McpServerFactoryHelper.CreateServerOptions(
                _state.Tools,
                session.Subscriptions,
                _state.Resources,
                _state.logFunction);

            var server = McpServerFactory.Create(transport, serverOptions);

            session.Server = server;
            session.ServerRunTask = server.RunAsync(HttpContext.CancellationToken);

            _state.HttpSessions.TryAdd(sessionId, session);
            session.StartPushLoop(HttpContext.CancellationToken);
        }

        response.Headers["mcp-session-id"] = sessionId;

        // Proceed to stream response if applicable
        var httpBodies = new EmbedIODuplexPipe(request.InputStream, new AutoFlushingStream(response.OutputStream));
        ConfigureSseHeaders(HttpContext.Response);

        try
        {
            var wroteResponse = await session.Transport.HandlePostRequest(httpBodies, HttpContext.CancellationToken);
            if (!wroteResponse)
            {
                response.StatusCode = 202;
                await HttpContext.SendStringAsync("Accepted", "text/plain", Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            MainThreadLogger.LogError($"[MCP POST Error] {ex}");
            await SendErrorAsync(HttpContext, 500, "Internal Server Error");
        }
    }

    [Route(HttpVerbs.Get, "/")]
    public async Task HandleStreamableGetAsync()
    {
        var sessionId = HttpContext.Request.Headers["mcp-session-id"];
        if (string.IsNullOrWhiteSpace(sessionId) || !_state.HttpSessions.TryGetValue(sessionId, out var session))
        {
            await SendErrorAsync(HttpContext, 404, "Session not found.");
            return;
        }

        var user = HttpContext.User as ClaimsPrincipal ?? new ClaimsPrincipal();
        if (!TryValidateUser(session, user, out var userError))
        {
            await SendErrorAsync(HttpContext, 403, userError);
            return;
        }

        if (!session.TryStartGetRequest())
        {
            await SendErrorAsync(HttpContext, 400, "Only one GET request allowed per session.");
            return;
        }

        ConfigureSseHeaders(HttpContext.Response);
        var flushingStream = new AutoFlushingStream(HttpContext.Response.OutputStream);

        try
        {
            await flushingStream.FlushAsync(HttpContext.CancellationToken);
            await session.Transport.HandleGetRequest(flushingStream, HttpContext.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected in some cases (e.g., request canceled), so handle quietly or log minimally.
            MainThreadLogger.Log("[MCP GET] Request was canceled.");
        }
        catch (Exception ex)
        {
            MainThreadLogger.LogWarning($"[MCP GET Error] {ex}");
        }
    }

    [Route(HttpVerbs.Delete, "/")]
    public async Task HandleStreamableDeleteAsync()
    {
        var sessionId = HttpContext.Request.Headers["mcp-session-id"];
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            await SendErrorAsync(HttpContext, 400, "Missing mcp-session-id.");
            return;
        }

        if (_state.HttpSessions.TryRemove(sessionId, out var session))
        {
            try
            {
                await session.DisposeAsync();
                MainThreadLogger.Log($"[MCP DELETE] Connection closed for session {sessionId}.");
                HttpContext.Response.StatusCode = 204;
                await HttpContext.Response.OutputStream.FlushAsync();
                HttpContext.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                MainThreadLogger.LogError($"[MCP DELETE] Error disposing session {sessionId}: {ex}");
                await SendErrorAsync(HttpContext, 500, "Failed to dispose session.");
            }
        }
        else
        {
            await SendErrorAsync(HttpContext, 404, "Session not found.");
        }
    }

}
