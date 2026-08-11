using Microsoft.AspNetCore.Identity;

namespace Finance.IdentityService.Domain;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>The "oid" claim of an external identity provider token (e.g. Entra External ID) — null for password-only accounts.</summary>
    public string? ExternalObjectId { get; set; }

    /// <summary>Which external provider ExternalObjectId came from, e.g. "EntraExternalId" — null for password-only accounts.</summary>
    public string? ExternalIdentityProvider { get; set; }

    /// <summary>Human-friendly name for greeting/display: FirstName+LastName, else Email, else
    /// UserName as a last resort. The FirstName/LastName fallback matters because a JIT-
    /// provisioned external account's UserName is a synthetic "aad_{oid}" string, and its
    /// given_name/family_name claims aren't guaranteed to be present in every token (depends on
    /// what the tenant's user flow actually collects and returns) — Email is nearly always
    /// populated and is far more presentable than the raw synthetic username either way.</summary>
    public string GetDisplayName()
    {
        var fullName = $"{FirstName} {LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return !string.IsNullOrWhiteSpace(Email) ? Email : UserName ?? string.Empty;
    }
}
