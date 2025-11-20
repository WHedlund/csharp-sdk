# Streamable HTTP Host (Unity 2021)

This folder adds a Unity-compatible host for the MCP Streamable HTTP transport without ASP.NET Core. It uses `HttpListener` on a background thread and mirrors the client expectations from `StreamableHttpClientSessionTransport`.

## Setup
- Drop `UnityHttpMcpHost` on a GameObject in your scene.
- Set `Prefix` (e.g., `http://localhost:5005/`). Ensure Windows allows the listen prefix (run the Editor as admin or reserve the URL with `netsh http add urlacl url=http://+:5005/ user=Users`).
- Add one or more `McpServerBinding` entries:
  - `ServerId`: becomes the path segment for endpoints (`/{serverId}/mcp`).
  - `Provider`: reference a `McpObjectDefinitionProvider` in your scene to export tools/resources/prompts.
  - `Stateless`: true disables unsolicited messages and GET endpoint.
- Press Play; console logs `[MCP HTTP] HTTP streamable MCP server listening on: ...` when the listener is ready.

## Endpoints (per serverId)
- `POST /{serverId}/mcp` — send JSON-RPC; responds with SSE (`text/event-stream`) or JSON. Sets `Mcp-Session-Id` on first success.
- `GET /{serverId}/mcp` — SSE channel for unsolicited messages (disabled if `Stateless`).
- `DELETE /{serverId}/mcp` — optional cleanup.
- Legacy fallbacks: `GET /{serverId}/mcp/sse`, `POST /{serverId}/mcp/message` map to the same handlers.

## Client example (C#)
```csharp
var options = new HttpClientTransportOptions
{
    Endpoint = "http://localhost:5005/default/mcp",
    TransportMode = HttpTransportMode.StreamableHttp
};
var client = await McpClient.CreateAsync(new HttpClientTransport("unity-client", options));
```

## Notes
- Placeholder auth: all requests are accepted; wire real validation in `ValidateAuthPlaceholder`.
- Errors from background threads are forwarded to the main thread via `UnityMcpDispatcher` for visibility.
- Idle sessions prune after `IdleTimeoutSeconds`; set 0 to keep sessions alive until DELETE.
- Unity API calls in tools run on the main thread via a filter inside `HttpStreamableListenerServer`.
