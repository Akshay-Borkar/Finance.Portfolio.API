using Finance.IdentityService.Application.Contracts;
using Finance.SharedKernel.Auth.Exceptions;
using Finance.IdentityService.Application.Models;
using Finance.IdentityService.Domain;
using Microsoft.AspNetCore.Identity;

namespace Finance.IdentityService.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenFactory _tokenFactory;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenFactory tokenFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenFactory = tokenFactory;
    }

    public async Task<AuthResponse> Login(AuthRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName)
            ?? throw new NotFoundException(nameof(ApplicationUser), request.UserName);

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
            throw new BadRequestException($"Credentials for '{request.UserName}' aren't valid.");

        return new AuthResponse
        {
            Id = user.Id,
            Token = await _tokenFactory.CreateTokenAsync(user),
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            DisplayName = user.GetDisplayName()
        };
    }

    public async Task<RegistrationResponse> Register(RegistrationRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("\n", result.Errors.Select(e => $"• {e.Description}"));
            throw new BadRequestException(errors);
        }

        await _userManager.AddToRoleAsync(user, request.Role);
        return new RegistrationResponse { UserId = user.Id };
    }
}
