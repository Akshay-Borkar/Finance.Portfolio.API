namespace Finance.SharedKernel.Auth;

public static class AuthConstants
{
    public static class Claims
    {
        public const string UserId = "uid";
        public const string Email = "email";
    }

    public static class Headers
    {
        public const string UserId = "X-User-Id";
        public const string UserEmail = "X-User-Email";
    }

    public static class Cors
    {
        public const string PolicyName = "CorsPolicy";

        public static readonly string[] AllowedOrigins =
        [
            "http://localhost:4200",
            "http://localhost:3000",
            "https://stfinanceportfolio.z19.web.core.windows.net",
        ];
    }

    public static class SignalR
    {
        public const string HubPathPrefix = "/hubs";
        public const string AccessTokenQueryParam = "access_token";
    }

    public static class Config
    {
        public const string JwtKey = "JwtSettings:Key";
        public const string JwtIssuer = "JwtSettings:Issuer";
        public const string JwtAudience = "JwtSettings:Audience";
        public const string AppInsightsConnectionString = "ApplicationInsights:ConnectionString";
    }

    public static class Database
    {
        public const int RetryMaxCount = 5;
        public const int RetryDelaySeconds = 10;
    }

    public static class Pagination
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
    }
}
