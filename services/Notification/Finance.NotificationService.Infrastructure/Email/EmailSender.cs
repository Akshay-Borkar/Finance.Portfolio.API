using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Finance.NotificationService.Infrastructure.Email;

public class EmailSender
{
    private readonly EmailSettings _settings;

    public EmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<bool> SendAsync(string to, string subject, string htmlContent)
    {
        var client = new SendGridClient(_settings.ApiKey);
        var from = new EmailAddress(_settings.FromAddress, _settings.FromName);
        var msg = MailHelper.CreateSingleEmail(
            from,
            new EmailAddress(to),
            subject,
            htmlContent,
            htmlContent);

        var response = await client.SendEmailAsync(msg);
        return response.IsSuccessStatusCode;
    }
}
