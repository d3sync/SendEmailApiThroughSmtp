namespace SendEmailApiThroughSmtp.Models
{
    public class EmailMessage
    {
        public int Id { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public EmailStatus Status { get; set; } = EmailStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
    }

    public enum EmailStatus
    {
        Pending,
        Sending,
        Sent,
        Failed
    }
}
