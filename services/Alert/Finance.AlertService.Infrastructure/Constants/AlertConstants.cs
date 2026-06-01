namespace Finance.AlertService.Infrastructure.Constants;

public static class AlertConstants
{
    public static class Direction
    {
        public const string Above = "above";
        public const string Below = "below";
    }

    public static class Config
    {
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
    }
}
