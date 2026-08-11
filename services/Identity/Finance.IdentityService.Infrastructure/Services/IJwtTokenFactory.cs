using Finance.IdentityService.Domain;

namespace Finance.IdentityService.Infrastructure.Services;

/// <summary>
/// Mints the app's own local JWT for an <see cref="ApplicationUser"/> — the single place both
/// password login and Entra External ID sign-in (after JIT provisioning) produce a token, so
/// every downstream service sees byte-identical claim shapes regardless of how the user signed in.
/// </summary>
public interface IJwtTokenFactory
{
    Task<string> CreateTokenAsync(ApplicationUser user);
}
