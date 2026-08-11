using Finance.IdentityService.Application.Contracts;
using Finance.IdentityService.Application.Models;
using Finance.SharedKernel.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.IdentityService.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IExternalAuthService _externalAuthService;

    public AuthController(IAuthService authService, IExternalAuthService externalAuthService)
    {
        _authService = authService;
        _externalAuthService = externalAuthService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(AuthRequest request)
        => Ok(await _authService.Login(request));

    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest request)
        => Ok(await _authService.Register(request));

    /// <summary>
    /// Exchanges a validated Microsoft Entra External ID access token (sent as the raw Bearer
    /// token on this one call) for this app's own local JWT — the SPA uses that local token for
    /// every subsequent request, exactly like a password login. See the EntraExternalId scheme
    /// in Finance.SharedKernel.Auth.JwtAuthenticationExtensions.
    /// </summary>
    [HttpPost("external/login")]
    [Authorize(AuthenticationSchemes = AuthConstants.Schemes.EntraExternalId)]
    public async Task<ActionResult<AuthResponse>> ExternalLogin()
        => Ok(await _externalAuthService.LoginWithExternalIdentityAsync(User));
}
