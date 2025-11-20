## Unity MCP Server Adapter

Adapters for the official MCP C# SDK to make it work inside Unity, including discovery, streamable HTTP hosting, and sample tests.

### Key Components
- `UnityMcpDiscovery`: Scans a GameObject hierarchy for `[McpServerTool]`, `[McpServerResource]`, and (stub) prompt attributes and produces server primitives.
- `McpObjectDefinitionProvider`: MonoBehaviour that caches the discovered tools/resources/prompts for a given GameObject tree. Prompts are not yet implemented.
- `HttpStreamableListenerServer`: Minimal `HttpListener`-based MCP host (streamable HTTP transport). Creates a session-specific `McpServer` per `Mcp-Session-Id`; tools/resources are dispatched to the Unity main thread via `UnityMcpDispatcher`.
- `UnityMcpDispatcher`: Simple main-thread dispatcher to safely run Unity API work coming from background threads (used by tool/resource filters).

### Samples and Tests
- `Samples/`: Example MonoBehaviours that define tools/resources (e.g., camera resources).
- `Tests/TestMcpDiscovery`: Logs discovered tools/resources/prompts for an assigned provider.
- `Tests/TestMcpInvocation`: In-memory transport test that exercises discovery, tool calls, and resource reads.
- `Tests/TestHttpNotifications`: Starts the HTTP host, connects a client, and verifies server->client notifications.

### Notes
- Ensure MCP SDK binaries are Unity-compatible for your target version.
- Prompts are currently stubbed; discovery returns an empty set until implementation is added.
- HTTP stateless mode disables unsolicited notifications; keep `Stateless = false` if you need server broadcasts/notifications.
