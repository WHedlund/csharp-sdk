using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

public class EndpointSessionState
{
    public ConcurrentDictionary<string, HttpMcpSession<SseResponseStreamTransport>> SseSessions { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, HttpMcpSession<StreamableHttpServerTransport>> HttpSessions { get; } = new(StringComparer.Ordinal);
    public List<McpServerTool> Tools { get; } = new();
    public List<Tuple<object, MethodInfo, Resource>> Resources { get; } = new();

    public Action<string> logFunction;
}
