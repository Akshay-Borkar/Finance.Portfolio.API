using Finance.IdentityService.Core.Models;

namespace Finance.IdentityService.Core.Services;

public interface IAuthService
{
    Task<AuthResponse> Login(AuthRequest request);
    Task<RegistrationResponse> Register(RegistrationRequest request);
}
