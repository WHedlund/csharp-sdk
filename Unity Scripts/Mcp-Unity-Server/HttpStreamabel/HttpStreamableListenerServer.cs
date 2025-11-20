using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityEngine;

/// <summary>
/// Minimal HttpListener-based MCP host for Unity that serves /{serverId}/mcp using the streamable HTTP transport.
/// Each incoming session spins up its own McpServer instance; tools/resources are marshalled to the main thread via UnityMcpDispatcher.
/// </summary>
[Serializable]
public sealed class McpServerBinding
{
    [Tooltip("Path segment used for this server instance, e.g. /{serverId}/mcp")]
    public string ServerId = "default";

    [Tooltip("Provider that exposes the tools/resources/prompts for this server id.")]
    public McpObjectDefinitionProvider Provider;

    [Tooltip("If true, disables unsolicited server->client messages and GET endpoint (stateless mode).")]
    public bool Stateless;
}

/// <summary>
/// Minimal Streamable HTTP host using HttpListener for Unity 2021. Mirrors client expectations
/// from StreamableHttpClientSessionTransport: POST/GET/DELETE on /{serverId}/mcp, SSE replies,
/// and Mcp-Session-Id handling. Runs on a background thread and forwards errors to the main thread.
/// </summary>
public sealed class HttpStreamableListenerServer
{
    private const string SessionHeader = "Mcp-Session-Id";

    private readonly HttpListener _listener = new HttpListener();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpServerBinding> _bindings;
    private readonly TimeSpan _idleTimeout;

    private Task _listenTask;

    public HttpStreamableListenerServer(string prefix, IEnumerable<McpServerBinding> bindings, TimeSpan? idleTimeout = null)
    {
        if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentNullException(nameof(prefix));
        if (bindings == null) throw new ArgumentNullException(nameof(bindings));

        _bindings = bindings
            .Where(b => b != null && !string.IsNullOrWhiteSpace(b.ServerId) && b.Provider != null)
            .GroupBy(b => b.ServerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First());

        if (_bindings.Count == 0)
        {
            throw new ArgumentException("At least one binding with a provider is required", nameof(bindings));
        }

        _idleTimeout = idleTimeout ?? TimeSpan.FromMinutes(10);

        var normalizedPrefix = prefix.EndsWith("/") ? prefix : prefix + "/";
        _listener.Prefixes.Add(normalizedPrefix);
    }

    public void Start()
    {
        _listener.Start();
        _listenTask = Task.Run(ListenLoopAsync);
        LogInfo($"HTTP streamable MCP server listening on: {string.Join(", ", _listener.Prefixes)}");
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogError("Listener loop error", ex);
                continue;
            }

            _ = Task.Run(() => HandleContextAsync(context));
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context)
    {
        PruneIdleSessions();

        var request = context.Request;
        var response = context.Response;

        var segments = request.Url?.AbsolutePath?.Trim('/').Split('/') ?? Array.Empty<string>();
        if (segments.Length < 2)
        {
            await WriteJsonRpcErrorAsync(response, "Not Found", (int)HttpStatusCode.NotFound).ConfigureAwait(false);
            return;
        }

        var serverId = segments[0];
        if (!_bindings.TryGetValue(serverId, out var binding))
        {
            await WriteJsonRpcErrorAsync(response, "Unknown server id", (int)HttpStatusCode.NotFound).ConfigureAwait(false);
            return;
        }

        if (!string.Equals(segments[1], "mcp", StringComparison.OrdinalIgnoreCase))
        {
            await WriteJsonRpcErrorAsync(response, "Not Found", (int)HttpStatusCode.NotFound).ConfigureAwait(false);
            return;
        }

        var tail = segments.Skip(2).ToArray();

        try
        {
            if (request.HttpMethod == "POST" && tail.Length == 0)
            {
                await HandlePostAsync(binding, context).ConfigureAwait(false);
            }
            else if (request.HttpMethod == "GET" && tail.Length == 0)
            {
                await HandleGetAsync(binding, context).ConfigureAwait(false);
            }
            else if (request.HttpMethod == "DELETE" && tail.Length == 0)
            {
                await HandleDeleteAsync(context).ConfigureAwait(false);
            }
            else if (request.HttpMethod == "GET" && tail.Length == 1 && string.Equals(tail[0], "sse", StringComparison.OrdinalIgnoreCase))
            {
                // Legacy SSE endpoint if a client falls back to /sse
                await HandleGetAsync(binding, context).ConfigureAwait(false);
            }
            else if (request.HttpMethod == "POST" && tail.Length == 1 && string.Equals(tail[0], "message", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePostAsync(binding, context).ConfigureAwait(false);
            }
            else
            {
                await WriteJsonRpcErrorAsync(response, "Not Found", (int)HttpStatusCode.NotFound).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            LogError($"Unhandled transport error for {request.RawUrl}", ex);
            await WriteJsonRpcErrorAsync(response, "Server error", (int)HttpStatusCode.InternalServerError).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                response.OutputStream?.Flush();
                response.Close();
            }
            catch
            {
                // ignored
            }
        }
    }

    private async Task HandlePostAsync(McpServerBinding binding, HttpListenerContext context)
    {
        var request = context.Request;
        if (!Accepts(request, "application/json") || !Accepts(request, "text/event-stream"))
        {
            await WriteJsonRpcErrorAsync(context.Response,
                "Not Acceptable: Client must accept both application/json and text/event-stream",
                (int)HttpStatusCode.NotAcceptable).ConfigureAwait(false);
            return;
        }

        if (!ValidateAuthPlaceholder(request))
        {
            await WriteJsonRpcErrorAsync(context.Response, "Unauthorized", (int)HttpStatusCode.Unauthorized).ConfigureAwait(false);
            return;
        }

        var session = await GetOrCreateSessionAsync(binding, context).ConfigureAwait(false);
        if (session == null)
        {
            return;
        }

        var message = await ReadJsonRpcMessageAsync(request).ConfigureAwait(false);
        if (message == null)
        {
            await WriteJsonRpcErrorAsync(context.Response,
                "Bad Request: The POST body did not contain a valid JSON-RPC message.",
                (int)HttpStatusCode.BadRequest).ConfigureAwait(false);
            return;
        }

        PrepareSseResponse(context.Response);

        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, session.SessionCancellation).Token;
        var wrote = await session.Transport.HandlePostRequestAsync(message, context.Response.OutputStream, cancellation).ConfigureAwait(false);

        if (!wrote)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            context.Response.ContentType = null;
        }
    }

    private async Task HandleGetAsync(McpServerBinding binding, HttpListenerContext context)
    {
        var request = context.Request;
        if (!Accepts(request, "text/event-stream"))
        {
            await WriteJsonRpcErrorAsync(context.Response,
                "Not Acceptable: Client must accept text/event-stream",
                (int)HttpStatusCode.NotAcceptable).ConfigureAwait(false);
            return;
        }

        var sessionId = request.Headers[SessionHeader];
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            await WriteJsonRpcErrorAsync(context.Response,
                "Bad Request: Mcp-Session-Id header is required",
                (int)HttpStatusCode.BadRequest).ConfigureAwait(false);
            return;
        }

        if (!_sessions.TryGetValue(sessionId, out var session) || !string.Equals(session.ServerId, binding.ServerId, StringComparison.OrdinalIgnoreCase))
        {
            await WriteJsonRpcErrorAsync(context.Response, "Session not found", (int)HttpStatusCode.NotFound, -32001).ConfigureAwait(false);
            return;
        }

        if (session.Transport.Stateless)
        {
            await WriteJsonRpcErrorAsync(context.Response, "GET not supported in stateless mode", (int)HttpStatusCode.BadRequest).ConfigureAwait(false);
            return;
        }

        PrepareSseResponse(context.Response);
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        try
        {
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, session.SessionCancellation).Token;
            session.Touch();
            await context.Response.OutputStream.FlushAsync(cancellation).ConfigureAwait(false);
            await session.Transport.HandleGetRequestAsync(context.Response.OutputStream, cancellation).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or server shutting down; ok to swallow.
        }
    }

    private async Task HandleDeleteAsync(HttpListenerContext context)
    {
        var sessionId = context.Request.Headers[SessionHeader];
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        if (_sessions.TryRemove(sessionId, out var session))
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }

        context.Response.StatusCode = (int)HttpStatusCode.Accepted;
    }

    private async Task<Session> GetOrCreateSessionAsync(McpServerBinding binding, HttpListenerContext context)
    {
        var sessionId = context.Request.Headers[SessionHeader];

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            if (_sessions.TryGetValue(sessionId, out var existing) && string.Equals(existing.ServerId, binding.ServerId, StringComparison.OrdinalIgnoreCase))
            {
                existing.Touch();
                context.Response.Headers[SessionHeader] = existing.Id;
                return existing;
            }

            await WriteJsonRpcErrorAsync(context.Response, "Session not found", (int)HttpStatusCode.NotFound, -32001).ConfigureAwait(false);
            return null;
        }

        var newSessionId = MakeSessionId();
        var session = await CreateSessionAsync(binding, newSessionId).ConfigureAwait(false);
        if (!_sessions.TryAdd(newSessionId, session))
        {
            await session.DisposeAsync().ConfigureAwait(false);
            await WriteJsonRpcErrorAsync(context.Response, "Unable to create session", (int)HttpStatusCode.InternalServerError).ConfigureAwait(false);
            return null;
        }

        context.Response.Headers[SessionHeader] = newSessionId;
        return session;
    }

    private async Task<Session> CreateSessionAsync(McpServerBinding binding, string sessionId)
    {
        var transport = new StreamableHttpServerTransport
        {
            SessionId = sessionId,
            Stateless = binding.Stateless,
            FlowExecutionContextFromRequests = true,
        };

        var options = await BuildOptionsAsync(binding.Provider).ConfigureAwait(false);
        var server = McpServer.Create(transport, options);

        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var runTask = Task.Run(async () =>
        {
            try
            {
                await server.RunAsync(sessionCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError($"MCP session {sessionId} run loop failed", ex);
            }
        }, sessionCts.Token);

        return new Session(binding.ServerId, sessionId, transport, server, sessionCts, runTask);
    }

    /// <summary>
    /// Sends a notification to all active sessions. Useful for server-driven events.
    /// </summary>
    public async Task BroadcastNotificationAsync(string method, object payload = null, CancellationToken cancellationToken = default)
    {
        var snapshot = _sessions.Values.ToArray();

        foreach (var session in snapshot)
        {
            try
            {
                await session.Server.SendNotificationAsync(method, payload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError($"Sending notification '{method}' to session {session.Id} failed", ex);
            }
        }
    }

    private static Task<McpServerOptions> BuildOptionsAsync(McpObjectDefinitionProvider provider)
    {
        // Unity components must be touched on the main thread.
        return UnityMcpDispatcher.Run(() =>
        {
            var options = new McpServerOptions
            {
                ToolCollection = new McpServerPrimitiveCollection<McpServerTool>(),
                ResourceCollection = new McpServerResourceCollection(),
                PromptCollection = new McpServerPrimitiveCollection<McpServerPrompt>()
            };

            var tools = provider.GetTools();
            var resources = provider.GetResources();
            var prompts = provider.GetPrompts();

            foreach (var tool in tools)
            {
                options.ToolCollection.Add(tool);
            }

            foreach (var resource in resources)
            {
                options.ResourceCollection.Add(resource);
            }

            foreach (var prompt in prompts)
            {
                options.PromptCollection.Add(prompt);
            }
            // TOOLS PASS Through dispatcher
            options.Filters.CallToolFilters.Add(next =>
            async (context, cancellationToken) =>
            {
                CallToolResult result = null;
                await UnityMcpDispatcher.Run(() =>
                {
                    result = next(context, cancellationToken).GetAwaiter().GetResult();
                });
                return result;
            });

            // RESOURCES PASS Through dispatcher (needed when resource handlers touch Unity objects)
            options.Filters.ReadResourceFilters.Add(next =>
            async (context, cancellationToken) =>
            {
                ReadResourceResult result = null;
                await UnityMcpDispatcher.Run(() =>
                {
                    result = next(context, cancellationToken).GetAwaiter().GetResult();
                });
                return result;
            });


            return options;
        });
    }

    private static async Task<JsonRpcMessage> ReadJsonRpcMessageAsync(HttpListenerRequest request)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<JsonRpcMessage>(
                request.InputStream,
                McpJsonUtilities.DefaultOptions,
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            Debug.LogWarning($"Failed to parse JSON-RPC message: {ex.Message}");
            return null;
        }
    }

    private static void PrepareSseResponse(HttpListenerResponse response)
    {
        response.ContentType = "text/event-stream";
        response.SendChunked = true;
        response.Headers["Cache-Control"] = "no-cache,no-store";
        response.Headers["Content-Encoding"] = "identity";
    }

    private static bool Accepts(HttpListenerRequest request, string mediaType)
    {
        var accept = request.Headers["Accept"] ?? string.Empty;
        return accept.IndexOf(mediaType, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static async Task WriteJsonRpcErrorAsync(HttpListenerResponse response, string message, int statusCode, int errorCode = -32000)
    {
        var error = new JsonRpcError
        {
            Id = default,
            Error = new JsonRpcErrorDetail
            {
                Code = errorCode,
                Message = message
            }
        };

        var payload = JsonSerializer.Serialize(error, McpJsonUtilities.DefaultOptions);
        var buffer = Encoding.UTF8.GetBytes(payload);
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
    }

    private static string MakeSessionId() => Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');

    private void PruneIdleSessions()
    {
        if (_idleTimeout <= TimeSpan.Zero)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var kv in _sessions)
        {
            if (now - kv.Value.LastActive > _idleTimeout && _sessions.TryRemove(kv.Key, out var session))
            {
                _ = session.DisposeAsync();
            }
        }
    }

    private static bool ValidateAuthPlaceholder(HttpListenerRequest request)
    {
        // Placeholder for auth; accept all requests for now.
        var token = request.Headers["Authorization"];
        return true;
    }

    private static void LogError(string message, Exception ex)
    {
        UnityMcpDispatcher.Run(() => Debug.LogError($"[MCP HTTP] {message}: {ex}"));
    }

    private static void LogInfo(string message)
    {
        UnityMcpDispatcher.Run(() => Debug.Log($"[MCP HTTP] {message}"));
    }

    public async ValueTask DisposeAsync()
    {
            _cts.Cancel();

        try
        {
            _listener.Stop();
        }
        catch
        {
        }

        if (_listenTask != null)
        {
            try
            {
                await _listenTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }

        foreach (var session in _sessions.Values)
        {
            try
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        _cts.Dispose();
    }

    private sealed class Session
    {
        public string ServerId { get; }
        public string Id { get; }
        public StreamableHttpServerTransport Transport { get; }
        public McpServer Server { get; }
        public CancellationToken SessionCancellation { get; private set; }
        public DateTimeOffset LastActive { get; private set; } = DateTimeOffset.UtcNow;

        private readonly CancellationTokenSource _sessionCts;
        private readonly Task _serverRunTask;

        public Session(string serverId, string id, StreamableHttpServerTransport transport, McpServer server, CancellationTokenSource sessionCts, Task serverRunTask)
        {
            ServerId = serverId;
            Id = id;
            Transport = transport;
            Server = server;
            _sessionCts = sessionCts;
            SessionCancellation = sessionCts.Token;
            _serverRunTask = serverRunTask;
        }

        public void Touch()
        {
            LastActive = DateTimeOffset.UtcNow;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _sessionCts.Cancel();
            }
            catch
            {
            }

            try
            {
                await Transport.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await Server.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await _serverRunTask.ConfigureAwait(false);
            }
            catch
            {
            }

            _sessionCts.Dispose();
        }
    }
}
