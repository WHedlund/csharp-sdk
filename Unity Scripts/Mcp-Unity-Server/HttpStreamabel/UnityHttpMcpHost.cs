using System;
using System.Collections.Generic;
using System.Linq;
using ModelContextProtocol.Server;
using UnityEngine;

public interface INotificationServiceConsumer
{
    void SetNotificationService(IMcpNotificationService service);
}

public sealed class UnityHttpMcpHost : MonoBehaviour
{
    [Tooltip("Prefix the HttpListener will bind to. Include trailing slash.")]
    public string Prefix = "http://127.0.0.1:8888";

    [Tooltip("Server bindings: server id + provider supplying tools/resources/prompts.")]
    public McpServerBinding[] Servers;

    [Tooltip("Idle timeout in seconds before sessions are pruned. Set 0 to disable.")]
    public float IdleTimeoutSeconds = 600f;

    private HttpStreamableListenerServer _host;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto discover all providers in the scene, regardless of where they live
        var providers = FindObjectsOfType<McpObjectDefinitionProvider>(true);

        var newBindings = new List<McpServerBinding>();

        if (providers != null && providers.Length > 0)
        {
            foreach (var provider in providers)
            {
                if (provider == null)
                    continue;

                // Try to keep any existing binding so we preserve Stateless etc.
                McpServerBinding existing = null;

                if (Servers != null)
                {
                    existing = Servers.FirstOrDefault(
                        b => b != null && b.Provider == provider
                    );
                }

                var binding = existing ?? new McpServerBinding();

                binding.Provider = provider;
                binding.ServerId = provider.name;

                newBindings.Add(binding);
            }
        }

        Servers = newBindings.ToArray();
    }
#endif

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
            WireNotificationServices();
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

    private void WireNotificationServices()
    {
        if (_host == null || Servers == null)
        {
            return;
        }

        foreach (var binding in Servers)
        {
            if (binding == null || binding.Provider == null || string.IsNullOrWhiteSpace(binding.ServerId))
            {
                continue;
            }

            var notifier = new McpNotificationService(_host, binding.ServerId);
            var consumers = binding.Provider
                .GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<INotificationServiceConsumer>();

            foreach (var consumer in consumers)
            {
                consumer.SetNotificationService(notifier);
            }
        }
    }
}
