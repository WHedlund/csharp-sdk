using EmbedIO;
using EmbedIO.WebApi;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System;
using ModelContextProtocol.Server;

public class EmbedIOServerHost : MonoBehaviour
{
    public string url = "http://127.0.0.1:8888/";
    private WebServer _server;
    private CancellationTokenSource _cts;

    // List to store services to be registered
    [SerializeField]
    private List<ServiceRegistration> _serviceRegistrations = new List<ServiceRegistration>();

    void Start() 
    {
        InitializeServer();
    }

    // Method to register services before starting the server
    public void RegisterService(string routePrefix, Dictionary<object, List<MethodInfo>> serviceDict, Action<string> logFunction = null)
    {
        var registration = new ServiceRegistration(routePrefix, serviceDict);
        if (logFunction != null ) registration.SessionState.logFunction = logFunction;

        foreach (var service in serviceDict)
        {
            foreach (var method in service.Value)
            {
                if (method.GetCustomAttribute<McpServerToolAttribute>() is McpServerToolAttribute toolAttr)
                {
                    registration.SessionState.Tools.Add(McpServerTool.Create(method, service.Key));
                }

                if (method.GetCustomAttribute<McpServerResourceAttribute>() is McpServerResourceAttribute resourceAttr)
                {
                    registration.SessionState.Resources.Add(Tuple.Create(service.Key, method, resourceAttr.ToResource()));
                }
            }
        }

        _serviceRegistrations.Add(registration);
        Debug.Log($"[MCP] Service registered for route: {routePrefix}");
    }


    // Initialize and start the server with all registered services
    private void InitializeServer()
    {
        _cts = new CancellationTokenSource();
        Debug.Log("[MCP] Initializing EmbedIO server...");

        // Create the server with basic configuration
        _server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
            .WithLocalSessionManager();

        // Register all services that were added to the list
        foreach (var serviceRegistration in _serviceRegistrations)
        {
            // Register Streamable HTTP at /<routePrefix>
            // Register SSE at /<routePrefix>/sse
            _server = _server.WithWebApi(serviceRegistration.routePrefix, m =>
            {
                m.WithController(() => new StreamableHttpApiController(serviceRegistration.routePrefix, serviceRegistration.SessionState));
                m.WithController(() => new SseApiController(serviceRegistration.routePrefix, serviceRegistration.SessionState));
            });


            Debug.Log($"[MCP] Registered endpoints at {serviceRegistration.routePrefix} and {serviceRegistration.routePrefix}/sse");
        }


        // Start the server
        _server.RunAsync(_cts.Token);
        Debug.Log("[MCP] EmbedIO Server running at " + url);
    }

    // Method to restart the server with the current registrations
    public void RestartServer()
    {
        // Dispose of the current server if it exists
        if (_server != null)
        {
            _cts.Cancel();
            _server.Dispose();
            Debug.Log("[MCP] Server stopped for restart.");
        }

        // Reinitialize with current registrations
        InitializeServer();
    }

    void OnApplicationQuit()
    {
        Debug.Log("[MCP] Application quitting. Stopping server...");
        _cts?.Cancel();
        _server?.Dispose();
        Debug.Log("[MCP] Server stopped.");
    }
}
