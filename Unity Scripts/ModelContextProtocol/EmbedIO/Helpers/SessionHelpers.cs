using System.Security.Claims;
using System;

public static class SessionHelpers
{
    public static string GenerateSessionId() => Guid.NewGuid().ToString("N");

    public static bool TryValidateUser(IHttpMcpSession session, ClaimsPrincipal user, out string error)
    {
        if (!session.HasSameUserId(user))
        {
            error = "Forbidden: session user mismatch.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
