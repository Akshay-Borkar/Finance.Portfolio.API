using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Finance.NotificationService.Infrastructure.Email;

public class EmailSender
{
    private readonly SendGridClient _client;
    private readonly EmailAddress _from;

    public EmailSender(IOptions<EmailSettings> settings)
    {
        var s = settings.Value;
        _client = new SendGridClient(s.ApiKey);
        _from = new EmailAddress(s.FromAddress, s.FromName);
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlContent, CancellationToken cancellationToken = default)
    {
        var from = _from;
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(to),
            subject,
            htmlContent,
            htmlContent);

        var response = await _client.SendEmailAsync(msg, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
