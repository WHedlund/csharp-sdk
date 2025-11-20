using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using UnityEngine;
using System.Linq;
using Microsoft.Extensions.AI;

/// <summary>
/// In-memory transport test that exercises discovery, tool calls, and resource reads against a provider.
/// </summary>
public class TestMcpInvocation : MonoBehaviour
{
    public McpObjectDefinitionProvider provider;

    async void Start()
    {
        if (provider == null)
        {
            Debug.LogError("Assign provider in inspector");
            return;
        }

        Debug.Log("===== MCP STREAM TEST START =====");

        await RunInMemoryServerTest();

        Debug.Log("===== MCP STREAM TEST END =====");
    }

    async Task RunInMemoryServerTest()
    {
        var tools = provider.GetTools();
        var resources = provider.GetResources();

        //Debug.Log("Discovered tools: " + tools.Count);
        //Debug.Log("Discovered resources: " + resources.Count);

        var clientToServerPipe = new Pipe();
        var serverToClientPipe = new Pipe();

        var serverTransport = new StreamServerTransport(
            clientToServerPipe.Reader.AsStream(),
            serverToClientPipe.Writer.AsStream()
        );

        // Your SDK requires primitive collections
        var options = new McpServerOptions()
        {
            ToolCollection = new McpServerPrimitiveCollection<McpServerTool>(),
            ResourceCollection = new McpServerResourceCollection()
        };
        foreach (var t in tools)
            options.ToolCollection.Add(t);

        foreach (var r in resources)
            options.ResourceCollection.Add(r);

        // TOOLS PASS Through dispatcher
        options.Filters.CallToolFilters.Add(next =>
            async (context, cancellationToken) =>
            {
                CallToolResult result = null;
                await UnityMcpDispatcher.Run(async () =>
                {
                    result = await next(context, cancellationToken);
                });
                return result;
            });

        McpServer server = null;
        McpClient client = null;

        try
        {
            // was: await using McpServer server = McpServer.Create(serverTransport, options);
            server = McpServer.Create(serverTransport, options);

            _ = server.RunAsync();

            var clientTransport = new StreamClientTransport(
                clientToServerPipe.Writer.AsStream(),
                serverToClientPipe.Reader.AsStream()
            );

            // was: await using McpClient client = await McpClient.CreateAsync(clientTransport);
            client = await McpClient.CreateAsync(clientTransport);

            await TestListTools(client);
            await TestCallToolsExplicit(client);
            await TestResourcesExplicit(client);
        }
        finally
        {
            // Dispose client if it supports IDisposable
            if (client is IDisposable clientDisposable)
            {
                clientDisposable.Dispose();
            }

            // Dispose server if it supports IDisposable
            if (server is IDisposable serverDisposable)
            {
                serverDisposable.Dispose();
            }
        }
    }


    async Task TestListTools(McpClient client)
    {
        Debug.Log("Listing tools:");
        var listed = await client.ListToolsAsync();

        foreach (var t in listed)
            Debug.Log("Tool: " + t.Name);
    }

    //
    // CALL YOUR SPECIFIC TOOLS WITH EXPLICIT ARGUMENTS
    //
    async Task TestCallToolsExplicit(McpClient client)
    {
        Debug.Log("Calling tools explicitly:");

        // ---- 1. CaptureFrame(format) ----
        try
        {
            var args = new Dictionary<string, object>()
            {
                //["format"] = JsonDocument.Parse("\"png\"").RootElement.Clone()
            };

            var tools = await client.ListToolsAsync();

            var result = await client.CallToolAsync("gameobject.CaptureFrame", args);
            if (result.IsError == true)
            {
                // Tool execution failed - get error message from content  
                var errorMessage = (result.Content[0] as TextContentBlock)?.Text;
                Debug.LogError("Tool failed with error: " + errorMessage);
            }
            else
            {
                // Tool succeeded  
                Debug.Log("CaptureFrame result: " + JsonSerializer.Serialize(result));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("CaptureFrame error: " + ex);
        }

        // ---- 2. MoveCamera(x, y, z) ----
        try
        {
            var args = new Dictionary<string, object>()
            {
                ["x"] = JsonDocument.Parse("1.0").RootElement.Clone(),
                ["y"] = JsonDocument.Parse("2.0").RootElement.Clone(),
                ["z"] = JsonDocument.Parse("3.0").RootElement.Clone()
            };

            var result = await client.CallToolAsync("gameobject.MoveCamera", args);
            if (result.IsError == true)
            {
                // Tool execution failed - get error message from content  
                var errorMessage = (result.Content[0] as TextContentBlock)?.Text;
                Debug.LogError("Tool failed with error: " + errorMessage);
            }
            else
            {
                Debug.Log("MoveCamera result: " + JsonSerializer.Serialize(result));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("MoveCamera error: " + ex);
        }
    }

    //
    // CALL YOUR SPECIFIC RESOURCE
    //
    async Task TestResourcesExplicit(McpClient client)
    {
        Debug.Log("Reading resources explicitly:");

        try
        {
            // This matches your attribute:
            // UriTemplate = "unity://camera/{id}/guidelines"
            // For testing, we pass literal URI (no path params resolved)
            var result = await client.ReadResourceAsync("unity://camera/{id}/guidelines");

            foreach (var content in result.Contents)
            {
                if (content is TextResourceContents text)
                {
                    Debug.Log("Resource returned: " + text.Text);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Resource read error: " + ex);
        }
    }
}
