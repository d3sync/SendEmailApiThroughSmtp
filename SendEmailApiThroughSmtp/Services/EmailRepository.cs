using LiteDB;
using SendEmailApiThroughSmtp.Models;

namespace SendEmailApiThroughSmtp.Services
{
    public interface IEmailRepository
    {
        int Add(EmailMessage email);
        EmailMessage? GetById(int id);
        IEnumerable<EmailMessage> GetPendingEmails();
        void Update(EmailMessage email);
        IEnumerable<EmailMessage> GetAll();
    }

    public class EmailRepository : IEmailRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<EmailRepository> _logger;

        public EmailRepository(IConfiguration configuration, ILogger<EmailRepository> logger)
        {
            var dbPath = configuration["DatabasePath"] ?? "emails.db";
            _connectionString = $"Filename={dbPath};Connection=shared";
            _logger = logger;
            
            // Initialize the database
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            col.EnsureIndex(x => x.Status);
            col.EnsureIndex(x => x.CreatedAt);
        }

        public int Add(EmailMessage email)
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            var id = col.Insert(email);
            _logger.LogInformation("Email added to database with ID: {EmailId}", id);
            return id.AsInt32;
        }

        public EmailMessage? GetById(int id)
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            return col.FindById(id);
        }

        public IEnumerable<EmailMessage> GetPendingEmails()
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            return col.Find(x => x.Status == EmailStatus.Pending).ToList();
        }

        public void Update(EmailMessage email)
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            col.Update(email);
            _logger.LogInformation("Email updated in database: {EmailId}, Status: {Status}", email.Id, email.Status);
        }

        public IEnumerable<EmailMessage> GetAll()
        {
            using var db = new LiteDatabase(_connectionString);
            var col = db.GetCollection<EmailMessage>("emails");
            return col.FindAll().ToList();
        }
    }
}
