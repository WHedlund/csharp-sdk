using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

public class McpUniversalBridge : MonoBehaviour
{
    [Header("MCP stdio server")]
    [Tooltip("Command to launch the MCP server (for example `uv`).")]
    public string Command = "uv";

    [Tooltip("Arguments for the MCP server command.")]
    public string[] Arguments = { "tool", "run", "universal-bridge-mcp" };

    private StdioClientTransport clientTransport;
    private McpClient client;
    private bool isConnected = false;

    private async void Start()
    {
        await ConnectAsync();

        if (isConnected)
        {
            await RunTestAsync();
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            Debug.Log($"Starting MCP stdio process: {Command} {string.Join(" ", Arguments)}");

            clientTransport = new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Command = Command,
                    Arguments = Arguments,
                    Name = "Unity MCP stdio client"
                });

            client = await McpClient.CreateAsync(clientTransport);
            isConnected = true;

            Debug.Log("MCP stdio client connected.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start / connect MCP stdio process: {ex.Message}\n{ex.StackTrace}");
            isConnected = false;
        }
    }

    private async Task RunTestAsync()
    {
        if (!isConnected || client == null)
        {
            Debug.LogWarning("MCP client not connected; cannot run test.");
            return;
        }

        try
        {
            // List available tools
            var tools = await client.ListToolsAsync();
            var toolNames = tools.Select(t => t.Name).ToList();
            Debug.Log("Available tools: " + string.Join(", ", toolNames));

            // Call the "add" tool: a = 5, b = 3
            var toolResult = await client.CallToolAsync(
                "add",
                new Dictionary<string, object?>
                {
                    ["a"] = 5,
                    ["b"] = 3
                });

            if (toolResult?.Content == null || toolResult.Content.Count == 0)
            {
                Debug.LogWarning("Tool `add` returned no content.");
                return;
            }

            // Most MCP text tools return one or more text content blocks
            var texts = toolResult.Content
                .OfType<TextContentBlock>()
                .Select(c => c.Text)
                .ToList();

            string combined = texts.Count > 0
                ? string.Join(", ", texts)
                : "[no text blocks in result]";

            Debug.Log("Tool `add` result: " + combined);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while running MCP test: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async void OnDestroy()
    {
        try
        {
            if (client != null)
            {
                Debug.Log("Disposing MCP client...");
                await client.DisposeAsync();
                client = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error while disposing MCP client: {ex.Message}");
        }

        clientTransport = null;
        isConnected = false;
    }
}
