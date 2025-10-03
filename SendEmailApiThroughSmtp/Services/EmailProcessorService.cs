using Microsoft.Extensions.Options;
using SendEmailApiThroughSmtp.Configuration;
using SendEmailApiThroughSmtp.Models;

namespace SendEmailApiThroughSmtp.Services
{
    public class EmailProcessorService : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailProcessorService> _logger;
        private readonly SmtpSettings _smtpSettings;

        public EmailProcessorService(
            IEmailQueue emailQueue,
            IServiceProvider serviceProvider,
            ILogger<EmailProcessorService> logger,
            IOptions<SmtpSettings> smtpSettings)
        {
            _emailQueue = emailQueue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _smtpSettings = smtpSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Processor Service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var emailId = await _emailQueue.DequeueAsync(stoppingToken);

                    // Create a new scope for each email processing
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IEmailRepository>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var email = repository.GetById(emailId);
                    if (email == null)
                    {
                        _logger.LogWarning("Email {EmailId} not found in database", emailId);
                        continue;
                    }

                    // Update status to Sending
                    email.Status = EmailStatus.Sending;
                    repository.Update(email);

                    // Send the email
                    var success = await emailService.SendEmailAsync(email);

                    if (success)
                    {
                        email.Status = EmailStatus.Sent;
                        email.SentAt = DateTime.UtcNow;
                        email.ErrorMessage = null;
                        _logger.LogInformation("Email {EmailId} sent successfully", emailId);
                    }
                    else
                    {
                        email.RetryCount++;
                        if (email.RetryCount >= _smtpSettings.MaxRetries)
                        {
                            email.Status = EmailStatus.Failed;
                            email.ErrorMessage = $"Failed after {email.RetryCount} attempts";
                            _logger.LogError("Email {EmailId} failed after {RetryCount} attempts", emailId, email.RetryCount);
                        }
                        else
                        {
                            email.Status = EmailStatus.Pending;
                            _logger.LogWarning("Email {EmailId} failed, will retry. Attempt {RetryCount}/{MaxRetries}", 
                                emailId, email.RetryCount, _smtpSettings.MaxRetries);
                            
                            // Re-queue for retry
                            await _emailQueue.EnqueueAsync(emailId);
                        }
                    }

                    repository.Update(email);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Email Processor Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email queue: {ErrorMessage}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Email Processor Service has stopped");
        }
    }
}
