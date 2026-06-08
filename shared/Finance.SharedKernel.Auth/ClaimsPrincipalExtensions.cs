using System.Security.Claims;

namespace Finance.SharedKernel.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(AuthConstants.Claims.UserId)?.Value;
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }
}
