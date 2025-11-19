using System.Security.Claims;

public interface IHttpMcpSession
{
    string Id { get; }
    bool HasSameUserId(ClaimsPrincipal user);
}
