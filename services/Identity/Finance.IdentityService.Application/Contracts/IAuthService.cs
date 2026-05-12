using Finance.IdentityService.Application.Models;

namespace Finance.IdentityService.Application.Contracts;

public interface IAuthService
{
    Task<AuthResponse> Login(AuthRequest request);
    Task<RegistrationResponse> Register(RegistrationRequest request);
}
