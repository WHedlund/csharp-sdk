using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Simple notification gateway that hides transport/session details from gameplay code.
/// </summary>
public interface IMcpNotificationService
{
    /// <summary>
    /// Sends a resource-updated notification to subscribers of the given URI on the configured server.
    /// </summary>
    Task NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default);
}

/// <summary>
/// Server-scoped implementation that delegates to the HTTP streamable host.
/// </summary>
public sealed class McpNotificationService : IMcpNotificationService
{
    private readonly HttpStreamableListenerServer _host;
    private readonly string _serverId;

    public McpNotificationService(HttpStreamableListenerServer host, string serverId)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _serverId = string.IsNullOrWhiteSpace(serverId)
            ? throw new ArgumentNullException(nameof(serverId))
            : serverId;
    }

    public Task NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default)
    {
        return _host.NotifyResourceUpdatedAsync(uri, _serverId, cancellationToken);
    }
}
