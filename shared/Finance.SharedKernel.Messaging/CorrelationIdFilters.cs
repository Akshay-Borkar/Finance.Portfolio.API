using System.Diagnostics;
using Finance.SharedKernel.Logging;
using MassTransit;
using LogContext = Serilog.Context.LogContext;

namespace Finance.SharedKernel.Messaging;

/// <summary>
/// Stamps the ambient correlation id (set by CorrelationIdMiddleware for HTTP-triggered work,
/// or freshly generated for background jobs) onto every outgoing Send message header.
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
/// Publish-pipe twin of <see cref="CorrelationIdSendFilter{T}"/>. MassTransit matches scoped filters
/// on the exact context interface, so a filter registered with UsePublishFilter must implement
/// IFilter&lt;PublishContext&lt;T&gt;&gt; — reusing the SendContext one throws at the first Publish.
/// </summary>
public class CorrelationIdPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdPublish");

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var correlationId = CorrelationContext.CorrelationId ?? Guid.NewGuid().ToString("n");
        context.Headers.Set(CorrelationIdSendFilter<T>.HeaderName, correlationId);
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
        cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), context);
        cfg.UseConsumeFilter(typeof(CorrelationIdConsumeFilter<>), context);
    }
}
