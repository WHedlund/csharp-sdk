using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using UnityEngine;

/// <summary>
/// Starts the streamable HTTP server, connects a client, and verifies a server-sent notification is received.
/// </summary>
public class TestHttpNotifications : MonoBehaviour
{
    public McpObjectDefinitionProvider provider;
    public string prefix = "http://localhost:5005/";
    public string serverId = "default";

    private HttpStreamableListenerServer server;
    private McpClient client;
    private CancellationTokenSource cts = new();

    private async void Start()
    {
        if (provider == null)
        {
            Debug.LogError("Assign a McpObjectDefinitionProvider");
            return;
        }

        // Start HTTP MCP server (stateless must be false to allow notifications)
        server = new HttpStreamableListenerServer(
            prefix,
            new[] { new McpServerBinding { ServerId = serverId, Provider = provider, Stateless = false } });
        server.Start();

        // Connect client
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{prefix.TrimEnd('/')}/{serverId}/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "http-test-client"
        });
        client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);

        // Listen for custom notification
        var received = new TaskCompletionSource<JsonRpcNotification>();
        // Fire-and-forget registration; skip await-using to stay compatible with older Unity runtimes.
        client.RegisterNotificationHandler("custom/ping", (n, _) =>
        {
            received.TrySetResult(n);
            return new ValueTask();
        });

        // Send periodic notifications from server side
        _ = Task.Run(async () =>
        {
            var i = 0;
            while (!cts.IsCancellationRequested)
            {
                await server.BroadcastNotificationAsync("custom/ping", new { message = $"tick {i++}" }, cts.Token);
                await Task.Delay(500, cts.Token);
            }
        }, cts.Token);

        // Assert first notification arrives
        try
        {
            var timeout = Task.Delay(TimeSpan.FromSeconds(5));
            var completed = await Task.WhenAny(received.Task, timeout);
            if (completed == timeout)
            {
                Debug.LogError("No notification received within timeout");
            }
            else
            {
                var first = await received.Task;
                Debug.Log($"Notification received: {first.Params}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Notification test error: " + ex);
        }
    }

    private async void OnDestroy()
    {
        cts.Cancel();

        if (client != null)
        {
            await client.DisposeAsync();
            client = null;
        }

        if (server != null)
        {
            await server.DisposeAsync();
            server = null;
        }

        cts.Dispose();
    }
}
