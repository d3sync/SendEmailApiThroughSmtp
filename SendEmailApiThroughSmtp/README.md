# Email API Through SMTP

A .NET 9 Web API for sending emails through SMTP servers with queue processing, LiteDB storage, and structured logging using Serilog.

## Features

- **Email Queue Processing**: Uses a background service with channel-based queue for reliable email delivery
- **LiteDB Storage**: Persists all email records with status tracking
- **SMTP with SSL**: Supports secure email sending through SMTP servers
- **Structured Logging**: Comprehensive logging using Serilog with enrichers (Machine Name, Thread ID, Environment)
- **Retry Logic**: Automatic retry mechanism for failed emails
- **RESTful API**: Simple HTTP endpoints for email operations
- **API Key Authentication**: Secure API endpoints with API key-based authentication
- **Swagger Documentation**: Interactive API documentation with Swagger/Swashbuckle

## Configuration

Update `appsettings.json` with your SMTP settings and API key:

```json
{
  "ApiKey": "YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION",
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

### Important Security Notes
?? **ALWAYS change the default API key in production!**
- Generate a strong, random API key (at least 32 characters)
- Store the API key securely
- Never commit the actual API key to source control
- Consider using environment variables or Azure Key Vault for production

### Gmail Configuration
If using Gmail, you need to:
1. Enable 2-factor authentication
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use the App Password in the `Password` field

## API Documentation

Once the application is running, you can access the interactive API documentation at:

**Swagger UI**: `http://localhost:5000/swagger`

The Swagger interface provides:
- Interactive API explorer with "Try it out" functionality
- Request/response examples and schemas
- Built-in API key authorization
- Easy testing of all endpoints

### Using Swagger UI:
1. Open http://localhost:5000/swagger in your browser
2. Click the **"Authorize"** button (lock icon at the top)
3. Enter your API key: `dev-api-key-12345-change-in-production`
4. Click **"Authorize"** and then **"Close"**
5. Now you can test any endpoint by clicking "Try it out"

## Authentication

All API endpoints require an API key to be included in the request headers:

```
X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION
```

Example using curl:
```bash
curl -X GET "http://localhost:5000/api/email" \
  -H "X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION"
```

**Note**: The `/swagger` endpoints do not require authentication.

## API Endpoints

### Send Email
**POST** `/api/email/send`

Headers:
```
Content-Type: application/json
X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION
```

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

Headers:
```
X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION
```

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

Headers:
```
X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION
```

Returns an array of all email records.

### Get Pending Emails
**GET** `/api/email/pending`

Headers:
```
X-API-Key: YOUR-SECRET-API-KEY-HERE-CHANGE-THIS-IN-PRODUCTION
```

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

Access the API documentation at: `http://localhost:5000/swagger`

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
- **Swashbuckle.AspNetCore**: Swagger/OpenAPI documentation
- System.Net.Mail: SMTP client for sending emails

## Architecture

1. **API Layer**: Minimal API endpoints for email operations
2. **Authentication Layer**: API key-based authentication middleware
3. **Queue Layer**: Channel-based queue for asynchronous processing
4. **Background Service**: Processes emails from the queue
5. **Repository Layer**: LiteDB data access
6. **Email Service**: SMTP email sending logic

## Error Handling

- Failed emails are automatically retried up to `MaxRetries` times
- After maximum retries, emails are marked as "Failed"
- All errors are logged with full exception details
- HTTP errors return appropriate status codes and messages
- Missing or invalid API key returns 401 Unauthorized

## Security Best Practices

1. **Change the default API key** immediately
2. Use environment variables for sensitive configuration in production
3. Enable HTTPS in production
4. Regularly rotate API keys
5. Monitor logs for unauthorized access attempts
6. Consider rate limiting for production use
7. Use strong SMTP passwords (App Passwords for Gmail)
