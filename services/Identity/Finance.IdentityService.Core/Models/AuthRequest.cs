namespace Finance.IdentityService.Core.Models;

public class AuthRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
