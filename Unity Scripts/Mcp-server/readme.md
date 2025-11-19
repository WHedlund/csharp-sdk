Adapter for official mcp csharp sdk into unity.

We assume the mcp library is compiled compatible with unity version at hand. See other readme [].

Samples/ provide scripts defining tools, resources, etc. These are used by Tests
Tests/ varifies basic functionallity like tool discovery and invocation. In memory server call should reflect real use cases.
UnityMcpDiscovery script iterattes trhough a gameobject hierarki and finds all McpTools, McpResrouces, etc.
McpObjectDefinitionProvider script is the (unity) monobehvoiur for finding McpResrouces etc. This is use by serers to find the definitions of a server.
Background features dispatchers for main thread

Tools discovery in hierarky implemented.
Resources discovery in hierarky implemented.
TODO: prompts not tested or discoverable

TODO: notification to client from server