using System.Security.Claims;
using Finance.IdentityService.Application.Models;

namespace Finance.IdentityService.Application.Contracts;

/// <summary>
/// Exchanges a validated external identity (e.g. a Microsoft Entra External ID token, already
/// authenticated by the EntraExternalId scheme) for this app's own local JWT — looking up or
/// JIT-provisioning a local <c>ApplicationUser</c> keyed by the external "oid" claim so every
/// downstream service treats an Azure AD sign-in identically to a password login.
/// </summary>
public interface IExternalAuthService
{
    Task<AuthResponse> LoginWithExternalIdentityAsync(ClaimsPrincipal externalPrincipal);
}
