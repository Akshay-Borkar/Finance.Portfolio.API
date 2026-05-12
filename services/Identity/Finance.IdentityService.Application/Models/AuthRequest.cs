namespace Finance.IdentityService.Application.Models;

public class AuthRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
