using System.Threading.Channels;
using SendEmailApiThroughSmtp.Models;

namespace SendEmailApiThroughSmtp.Services
{
    public interface IEmailQueue
    {
        ValueTask EnqueueAsync(int emailId);
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
    }

    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<int> _channel;
        private readonly ILogger<EmailQueue> _logger;

        public EmailQueue(ILogger<EmailQueue> logger)
        {
            _logger = logger;
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<int>(options);
        }

        public async ValueTask EnqueueAsync(int emailId)
        {
            await _channel.Writer.WriteAsync(emailId);
            _logger.LogInformation("Email {EmailId} enqueued for processing", emailId);
        }

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            var emailId = await _channel.Reader.ReadAsync(cancellationToken);
            _logger.LogDebug("Email {EmailId} dequeued for processing", emailId);
            return emailId;
        }
    }
}
