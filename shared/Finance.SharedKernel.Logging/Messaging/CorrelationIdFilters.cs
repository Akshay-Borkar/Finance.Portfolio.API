using System.Diagnostics;
using MassTransit;
using LogContext = Serilog.Context.LogContext;

namespace Finance.SharedKernel.Logging.Messaging;

/// <summary>
/// Stamps the ambient correlation id (set by CorrelationIdMiddleware for HTTP-triggered work,
/// or freshly generated for background jobs) onto every outgoing Send/Publish message header.
/// </summary>
public class CorrelationIdSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    public const string HeaderName = "X-Correlation-Id";

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdSend");

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var correlationId = CorrelationContext.CorrelationId ?? Guid.NewGuid().ToString("n");
        context.Headers.Set(HeaderName, correlationId);
        Activity.Current?.SetTag("correlation.id", correlationId);
        return next.Send(context);
    }
}

/// <summary>
/// Restores the correlation id from an inbound message header into the ambient context and the
/// Serilog LogContext, so consumer logs — and anything it publishes in turn — stay in the same trail.
/// </summary>
public class CorrelationIdConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdConsume");

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationId = context.Headers.Get<string>(CorrelationIdSendFilter<T>.HeaderName) ?? Guid.NewGuid().ToString("n");
        CorrelationContext.CorrelationId = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next.Send(context);
        }
    }
}

public static class MassTransitCorrelationExtensions
{
    /// <summary>
    /// Wires the correlation id send/publish/consume filters onto a bus. Call once inside
    /// UsingRabbitMq/UsingAzureServiceBus, before ConfigureEndpoints.
    /// </summary>
    public static void UseCorrelationLogging(this IBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        cfg.UseSendFilter(typeof(CorrelationIdSendFilter<>), context);
        cfg.UsePublishFilter(typeof(CorrelationIdSendFilter<>), context);
        cfg.UseConsumeFilter(typeof(CorrelationIdConsumeFilter<>), context);
    }
}
