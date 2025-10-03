# Email API Through SMTP

A .NET 9 Web API for sending emails through SMTP servers with queue processing, LiteDB storage, and structured logging using Serilog.

## Features

- **Email Queue Processing**: Uses a background service with channel-based queue for reliable email delivery
- **LiteDB Storage**: Persists all email records with status tracking
- **SMTP with SSL**: Supports secure email sending through SMTP servers
- **Structured Logging**: Comprehensive logging using Serilog with enrichers (Machine Name, Thread ID, Environment)
- **Retry Logic**: Automatic retry mechanism for failed emails
- **RESTful API**: Simple HTTP endpoints for email operations

## Configuration

Update `appsettings.json` with your SMTP settings:

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Email Service",
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  },
  "DatabasePath": "emails.db"
}
```

### Gmail Configuration
If using Gmail, you need to:
1. Enable 2-factor authentication
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use the App Password in the `Password` field

## API Endpoints

### Send Email
**POST** `/api/email/send`

Request body:
```json
{
  "to": "recipient@example.com",
  "subject": "Test Email",
  "body": "<h1>Hello World</h1>",
  "isHtml": true,
  "cc": "cc@example.com",
  "bcc": "bcc@example.com"
}
```

Response:
```json
{
  "id": 1,
  "message": "Email queued successfully",
  "status": "Pending"
}
```

### Get Email by ID
**GET** `/api/email/{id}`

Response:
```json
{
  "id": 1,
  "to": "recipient@example.com",
  "subject": "Test Email",
  "body": "<h1>Hello World</h1>",
  "isHtml": true,
  "cc": null,
  "bcc": null,
  "status": "Sent",
  "createdAt": "2025-01-15T10:00:00Z",
  "sentAt": "2025-01-15T10:00:05Z",
  "errorMessage": null,
  "retryCount": 0
}
```

### Get All Emails
**GET** `/api/email`

Returns an array of all email records.

### Get Pending Emails
**GET** `/api/email/pending`

Returns an array of emails with status "Pending".

## Email Status

- **Pending**: Email is queued and waiting to be sent
- **Sending**: Email is currently being sent
- **Sent**: Email was successfully sent
- **Failed**: Email failed after maximum retry attempts

## Logging

Logs are written to:
- **Console**: Real-time logging with structured format
- **Files**: Daily rolling log files in `logs/` directory (max 10MB per file, 30 days retention)

Log format includes:
- Timestamp
- Log Level
- Source Context
- Machine Name
- Thread ID
- Message
- Exception details (if any)

## Running the Application

```bash
dotnet run
```

The API will be available at `http://localhost:5000` (or the configured port).

## Database

Email records are stored in `emails.db` (LiteDB) in the application directory. The database includes:
- All email details
- Status tracking
- Timestamps (created and sent)
- Error messages
- Retry count

## Dependencies

- **LiteDB**: Embedded NoSQL database
- **Serilog.AspNetCore**: Structured logging framework
- **Serilog.Enrichers.Environment**: Machine name enricher
- **Serilog.Enrichers.Thread**: Thread ID enricher
- System.Net.Mail: SMTP client for sending emails

## Architecture

1. **API Layer**: Minimal API endpoints for email operations
2. **Queue Layer**: Channel-based queue for asynchronous processing
3. **Background Service**: Processes emails from the queue
4. **Repository Layer**: LiteDB data access
5. **Email Service**: SMTP email sending logic

## Error Handling

- Failed emails are automatically retried up to `MaxRetries` times
- After maximum retries, emails are marked as "Failed"
- All errors are logged with full exception details
- HTTP errors return appropriate status codes and messages
