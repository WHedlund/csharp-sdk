using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System;

public static class McpServerFactoryHelper
{
    private static Implementation CreateServerInfo(string name = "MyServer", string version = "1.0.0")
    {
        return new Implementation
        {
            Name = name,
            Version = version
        };
    }

    public static McpServerOptions CreateServerOptions(
        List<McpServerTool> tools,
        ConcurrentDictionary<string, bool> subscriptions,
        List<Tuple<object, MethodInfo, Resource>> resources)
    {
        return new McpServerOptions
        {
            ServerInfo = CreateServerInfo(),
            Capabilities = new ServerCapabilities
            {
                Tools = MCPCapabilityCreator.CreateToolsCapability(tools),
                Resources = MCPCapabilityCreator.CreateResourceCapability(subscriptions, resources),
                Logging = MCPCapabilityCreator.CreateLoggingCapability()
            }
        };
    }

    public static McpServerOptions CreateServerOptions(
        List<McpServerTool> tools,
        ConcurrentDictionary<string, bool> subscriptions,
        List<Tuple<object, MethodInfo, Resource>> resources,
        Action<string> logFunction = null)
    {
        return new McpServerOptions
        {
            ServerInfo = CreateServerInfo(),
            Capabilities = new ServerCapabilities
            {
                Tools = MCPCapabilityCreator.CreateToolsCapability(tools, logFunction),
                Resources = MCPCapabilityCreator.CreateResourceCapability(subscriptions, resources),
                Logging = MCPCapabilityCreator.CreateLoggingCapability()
            }
        };
    }
}
