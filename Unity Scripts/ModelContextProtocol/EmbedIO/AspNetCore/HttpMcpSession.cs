using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

public sealed class HttpMcpSession<TTransport> : IAsyncDisposable, IHttpMcpSession
    where TTransport : ITransport
{
    private int _referenceCount;
    private int _getRequestStarted;
    private readonly CancellationTokenSource _disposeCts = new();

    public string Id { get; }
    public TTransport Transport { get; }
    public (string Type, string Value, string Issuer)? UserIdClaim { get; }

    public IMcpServer? Server { get; set; }
    public Task? ServerRunTask { get; set; }
    public CancellationTokenSource? PushLoopTokenSource { get; private set; }
    public Task? PushLoopTask { get; private set; }

    public ConcurrentDictionary<string, bool> Subscriptions { get; } = new();

    public CancellationToken SessionClosed => _disposeCts.Token;
    public bool IsActive => !SessionClosed.IsCancellationRequested && _referenceCount > 0;
    public long LastActivityTicks { get; private set; }

    public HttpMcpSession(string sessionId, TTransport transport, ClaimsPrincipal user)
    {
        Id = sessionId;
        Transport = transport;
        UserIdClaim = GetUserIdClaim(user);
        LastActivityTicks = DateTimeOffset.UtcNow.Ticks;
    }

    public IDisposable AcquireReference()
    {
        Interlocked.Increment(ref _referenceCount);
        return new UnreferenceDisposable(this);
    }

    public bool TryStartGetRequest() => Interlocked.Exchange(ref _getRequestStarted, 1) == 0;

    public void StartPushLoop(CancellationToken token)
    {
        PushLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        PushLoopTask = MCPCapabilityCreator.StartResourcePushLoop(Server, Subscriptions, PushLoopTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _disposeCts.Cancel();

            if (PushLoopTokenSource is not null)
                PushLoopTokenSource.Cancel();

            if (PushLoopTask is not null)
                await PushLoopTask;

            if (ServerRunTask is not null)
                await ServerRunTask;
        }
        catch (OperationCanceledException)
        {
            // Swallow expected cancellation
        }
        catch (Exception)
        {
            // Optionally swallow or rethrow unexpected exceptions
        }
        finally
        {
            try
            {
                if (Server is not null)
                    await Server.DisposeAsync();
            }
            catch (Exception)
            {
                // Swallow or log if needed
            }

            try
            {
                await Transport.DisposeAsync();
            }
            catch (Exception)
            {
                // Swallow or log if needed
            }

            _disposeCts.Dispose();
        }
    }


    public bool HasSameUserId(ClaimsPrincipal user)
        => UserIdClaim == GetUserIdClaim(user);

    private static (string Type, string Value, string Issuer)? GetUserIdClaim(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true) return null;

        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                 ?? user.FindFirst("sub")
                 ?? user.FindFirst(ClaimTypes.Upn);

        return claim is { } idClaim
            ? (idClaim.Type, idClaim.Value, idClaim.Issuer)
            : null;
    }

    private sealed class UnreferenceDisposable : IDisposable
    {
        private readonly HttpMcpSession<TTransport> _session;

        public UnreferenceDisposable(HttpMcpSession<TTransport> session)
        {
            _session = session;
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _session._referenceCount) == 0)
                _session.LastActivityTicks = DateTimeOffset.UtcNow.Ticks;
        }
    }
}
