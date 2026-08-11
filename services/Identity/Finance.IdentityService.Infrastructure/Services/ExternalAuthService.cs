using System.Security.Claims;
using Finance.IdentityService.Application.Contracts;
using Finance.IdentityService.Application.Models;
using Finance.IdentityService.Domain;
using Finance.SharedKernel.Auth.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IdentityConstants = Finance.IdentityService.Infrastructure.Constants.IdentityConstants;

namespace Finance.IdentityService.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IJwtTokenFactory _tokenFactory;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IJwtTokenFactory tokenFactory,
        ILogger<ExternalAuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenFactory = tokenFactory;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginWithExternalIdentityAsync(ClaimsPrincipal externalPrincipal)
    {
        var objectId = GetObjectId(externalPrincipal)
            ?? throw new BadRequestException("The external token has no object id (oid/sub) claim.");

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ExternalObjectId == objectId);

        if (user is null)
        {
            user = await ProvisionShadowUserAsync(objectId, externalPrincipal);
        }

        return new AuthResponse
        {
            Id = user.Id,
            Token = await _tokenFactory.CreateTokenAsync(user),
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.GetDisplayName()
        };
    }

    private async Task<ApplicationUser> ProvisionShadowUserAsync(string objectId, ClaimsPrincipal externalPrincipal)
    {
        // Deliberately not linked to any existing password account by email match — an
        // unverified email claim from the token isn't proof of ownership of a local account,
        // so silently merging identities here would be an account-takeover vector. Every new
        // external identity gets its own shadow user instead.
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ExternalObjectId = objectId,
            ExternalIdentityProvider = IdentityConstants.ExternalAuth.Provider,
            Email = GetEmail(externalPrincipal),
            UserName = $"aad_{objectId}",
            FirstName = externalPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? externalPrincipal.FindFirstValue("given_name") ?? string.Empty,
            LastName = externalPrincipal.FindFirstValue(ClaimTypes.Surname) ?? externalPrincipal.FindFirstValue("family_name") ?? string.Empty,
            EmailConfirmed = true
        };

        // No password set — CreateAsync without a password leaves PasswordHash null, so
        // CheckPasswordSignInAsync (the /api/auth/login path) can never authenticate this
        // shadow account. It only ever gets a token via this external-exchange path.
        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("\n", result.Errors.Select(e => $"• {e.Description}"));
            throw new BadRequestException(errors);
        }

        if (await _roleManager.RoleExistsAsync(IdentityConstants.ExternalAuth.DefaultRole))
        {
            await _userManager.AddToRoleAsync(user, IdentityConstants.ExternalAuth.DefaultRole);
        }
        else
        {
            // Pre-existing gap in this app: no roles are seeded anywhere, so a fresh environment
            // has none yet. Don't fail the whole sign-in over it — log and continue with no role,
            // same net effect password Register() would hit today if it didn't throw on this.
            _logger.LogWarning(
                "Role '{Role}' does not exist — provisioned {UserId} without a role assignment.",
                IdentityConstants.ExternalAuth.DefaultRole, user.Id);
        }

        return user;
    }

    private static string? GetObjectId(ClaimsPrincipal principal) =>
        principal.FindFirstValue("oid")
        ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? principal.FindFirstValue("sub");

    private static string GetEmail(ClaimsPrincipal principal) =>
        principal.FindFirstValue("emails")
        ?? principal.FindFirstValue(ClaimTypes.Email)
        ?? principal.FindFirstValue("email")
        ?? principal.FindFirstValue("preferred_username")
        ?? string.Empty;
}
