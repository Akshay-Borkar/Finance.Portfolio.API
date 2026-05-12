using Finance.Contracts.Events;
using Finance.NotificationService.Infrastructure.Email;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Finance.NotificationService.Infrastructure.Consumers;

public class AlertTriggeredConsumer : IConsumer<AlertTriggered>
{
    private readonly EmailSender _emailSender;
    private readonly ILogger<AlertTriggeredConsumer> _logger;

    public AlertTriggeredConsumer(EmailSender emailSender, ILogger<AlertTriggeredConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AlertTriggered> context)
    {
        var msg = context.Message;

        var subject = $"Price Alert Triggered: {msg.Ticker}";
        var html = $"""
            <h2>Price Alert Triggered</h2>
            <p>Your alert for <strong>{msg.Ticker}</strong> has been triggered.</p>
            <p>The price is now <strong>{msg.CurrentPrice:F2}</strong>,
               which is {msg.Direction} your target of <strong>{msg.TargetPrice:F2}</strong>.</p>
            <p>Log in to your dashboard to review your portfolio.</p>
            """;

        var sent = await _emailSender.SendAsync(msg.UserEmail, subject, html);

        _logger.LogInformation(
            "Alert email {Status} to {Email} for {Ticker} (alert {AlertId})",
            sent ? "sent" : "failed", msg.UserEmail, msg.Ticker, msg.AlertId);
    }
}
