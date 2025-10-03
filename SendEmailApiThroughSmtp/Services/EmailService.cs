using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SendEmailApiThroughSmtp.Configuration;
using SendEmailApiThroughSmtp.Models;

namespace SendEmailApiThroughSmtp.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailMessage email);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailMessage email)
        {
            try
            {
                _logger.LogInformation("Attempting to send email {EmailId} to {To}", email.Id, email.To);

                using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    EnableSsl = _smtpSettings.EnableSsl,
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    Timeout = _smtpSettings.TimeoutSeconds * 1000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                    Subject = email.Subject,
                    Body = email.Body,
                    IsBodyHtml = email.IsHtml
                };

                // Add recipients
                foreach (var recipient in email.To.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    mailMessage.To.Add(recipient.Trim());
                }

                // Add CC recipients
                if (!string.IsNullOrWhiteSpace(email.Cc))
                {
                    foreach (var cc in email.Cc.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        mailMessage.CC.Add(cc.Trim());
                    }
                }

                // Add BCC recipients
                if (!string.IsNullOrWhiteSpace(email.Bcc))
                {
                    foreach (var bcc in email.Bcc.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        mailMessage.Bcc.Add(bcc.Trim());
                    }
                }

                await smtpClient.SendMailAsync(mailMessage);
                
                _logger.LogInformation("Email {EmailId} sent successfully to {To}", email.Id, email.To);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email {EmailId} to {To}. Error: {ErrorMessage}", 
                    email.Id, email.To, ex.Message);
                return false;
            }
        }
    }
}
