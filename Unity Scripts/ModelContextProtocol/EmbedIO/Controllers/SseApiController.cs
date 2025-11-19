using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol;
using System;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using static HttpResponseHelpers;
using static SessionHelpers;
#nullable enable

public sealed class SseApiController : WebApiController
{
    private readonly EndpointSessionState _state;
    private readonly string _routePrefix;

    public SseApiController(string routePrefix, EndpointSessionState state)
    {
        _routePrefix = routePrefix;
        _state = state;
    }

    [Route(HttpVerbs.Get, "/sse")]
    public async Task HandleSseRequestAsync()
    {
        var response = HttpContext.Response;
        ConfigureSseHeaders(response);

        var sessionId = GenerateSessionId();
        var messageEndpoint = $"{_routePrefix}/message?sessionId={sessionId}";
        var outputstream = new AutoFlushingStream(response.OutputStream);

        var user = HttpContext.User as ClaimsPrincipal ?? new ClaimsPrincipal();
        await using var transport = new SseResponseStreamTransport(outputstream, messageEndpoint);
        await using var session = new HttpMcpSession<SseResponseStreamTransport>(sessionId, transport, user);

        if (!_state.SseSessions.TryAdd(sessionId, session))
            throw new Exception($"Session ID collision: {sessionId}");

        var serverOptions = McpServerFactoryHelper.CreateServerOptions(_state.Tools, session.Subscriptions, _state.Resources, _state.logFunction);
        await using var mcpServer = McpServerFactory.Create(transport, serverOptions);
        session.Server = mcpServer;

        session.StartPushLoop(HttpContext.CancellationToken);

        var transportTask = transport.RunAsync(HttpContext.CancellationToken);

        try
        {
            MainThreadLogger.Log($"[MCP SSE] Session {session.Id} started.");
            await mcpServer.RunAsync(HttpContext.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            MainThreadLogger.Log($"[MCP SSE] Session {session.Id} cancelled.");
        }
        finally
        {
            _state.SseSessions.TryRemove(session.Id, out _);

            await transport.DisposeAsync();
            await transportTask;

            MainThreadLogger.Log($"[MCP SSE] Session {session.Id} ended.");
            await session.DisposeAsync(); // this will cancel and await the push loop

        }
    }


    [Route(HttpVerbs.Post, "/message")]
    public async Task HandleMessageRequestAsync()
    {
        try
        {
            var sessionId = HttpContext.Request.QueryString["sessionId"];
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                await SendErrorAsync(HttpContext, 400, "Missing sessionId.");
                return;
            }

            if (!_state.SseSessions.TryGetValue(sessionId, out var session))
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

            var json = await HttpContext.GetRequestBodyAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                await SendErrorAsync(HttpContext, 400, "Empty message body.");
                return;
            }

            JsonRpcMessage? message = JsonSerializer.Deserialize<JsonRpcMessage>(json, McpJsonUtilities.DefaultOptions);
            if (message is null)
            {
                await SendErrorAsync(HttpContext, 400, "Invalid JSON-RPC message.");
                return;
            }
            await session.Transport.OnMessageReceivedAsync(message, HttpContext.CancellationToken);
            HttpContext.Response.StatusCode = 202;
            await HttpContext.SendStringAsync("Accepted", "text/plain", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            MainThreadLogger.LogWarning($"[SSE Controller] {ex}");
            await SendErrorAsync(HttpContext, 500, "Internal Server Error");
        }
    }
}
