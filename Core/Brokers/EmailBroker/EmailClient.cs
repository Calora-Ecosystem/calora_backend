using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Core.Brokers.EmailBroker;

public class EmailClient(
    IOptions<EmailConfig> options)
{
    private readonly EmailConfig _config = options.Value;

    public async Task SendMailAsync(
        string email, string body, string subject = "", bool isHtml = false)
    {
        var transaction = SentrySdk.GetTransaction();
        var span = transaction?.StartChild("email.send", "Sending Email");

        try
        {
            var from = new MailAddress(_config.Username, _config.From);
            var to = new MailAddress(email);
            var mail = new MailMessage(from, to)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            using var smtpClient = new SmtpClient(_config.Host, _config.Port);
            smtpClient.Credentials = new NetworkCredential(_config.Username, _config.Password);
            smtpClient.EnableSsl = true;

            await smtpClient.SendMailAsync(mail);
            span?.Finish(SpanStatus.Ok);
        }
        catch (Exception e)
        {
            span?.Finish(e);
            throw;
        }
    }
}