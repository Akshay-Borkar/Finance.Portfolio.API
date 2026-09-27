namespace Finance.SharedKernel.Messaging;

public static class MessagingConstants
{
    public static class Config
    {
        public const string RabbitMqHost = "RabbitMq:Host";
        public const string RabbitMqUsername = "RabbitMq:Username";
        public const string RabbitMqPassword = "RabbitMq:Password";

        /// <summary>Flat key, not nested under a section — matches the Key Vault secret name.</summary>
        public const string ServiceBusConnectionString = "ServiceBusConnectionString";
    }

    /// <summary>RabbitMQ's own default credentials, used when the config supplies none (local dev).</summary>
    public const string DefaultRabbitMqCredential = "guest";
}
