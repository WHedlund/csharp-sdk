using System;
using UnityEngine;

/// <summary>
/// Simple MonoBehaviour to start the HttpStreamableListenerServer from a scene.
/// Configure bindings in the inspector, press Play, and connect a streamable HTTP client
/// to http://host:port/{serverId}/mcp.
/// </summary>
public sealed class UnityHttpMcpHost : MonoBehaviour
{
    [Tooltip("Prefix the HttpListener will bind to. Include trailing slash.")]
    public string Prefix = "http://localhost:5005/";

    [Tooltip("Server bindings: server id + provider supplying tools/resources/prompts.")]
    public McpServerBinding[] Servers;

    [Tooltip("Idle timeout in seconds before sessions are pruned. Set 0 to disable.")]
    public float IdleTimeoutSeconds = 600f;

    private HttpStreamableListenerServer _host;

    private void Start()
    {
        if (Servers == null || Servers.Length == 0)
        {
            Debug.LogError("Configure at least one McpServerBinding on UnityHttpMcpHost");
            return;
        }

        var timeout = IdleTimeoutSeconds <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(IdleTimeoutSeconds);

        try
        {
            _host = new HttpStreamableListenerServer(Prefix, Servers, timeout);
            _host.Start();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start HTTP MCP host: {ex}");
            _host = null;
        }
    }

    private async void OnDestroy()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
            _host = null;
        }
    }
}
