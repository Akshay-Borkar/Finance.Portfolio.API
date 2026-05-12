using Finance.IdentityService.Application.Contracts;
using Finance.IdentityService.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Finance.IdentityService.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(AuthRequest request)
        => Ok(await _authService.Login(request));

    [HttpPost("register")]
    public async Task<ActionResult<RegistrationResponse>> Register(RegistrationRequest request)
        => Ok(await _authService.Register(request));
}
