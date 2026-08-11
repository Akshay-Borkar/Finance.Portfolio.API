namespace Finance.IdentityService.Infrastructure.Constants;

public static class IdentityConstants
{
    public const string ServiceName = "identity";

    public static class ExternalAuth
    {
        public const string Provider = "EntraExternalId";

        /// <summary>Role assigned to a JIT-provisioned shadow user — matches the Angular
        /// registration form's default (<c>role: ['Employee', ...]</c>), so an Entra sign-in
        /// starts with the same permissions a fresh password sign-up would get.</summary>
        public const string DefaultRole = "Employee";
    }
}
