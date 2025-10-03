# Quick Start Guide

## ?? Getting Started in 3 Steps

### Step 1: Configure SMTP Settings

Edit `appsettings.json` and update the SMTP settings:

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",          // Your SMTP server
  "Port": 587,                        // SMTP port
  "EnableSsl": true,                  // Enable SSL/TLS
  "Username": "your-email@gmail.com", // SMTP username
  "Password": "your-app-password",    // SMTP password (use App Password for Gmail)
  "FromEmail": "your-email@gmail.com",// From email address
  "FromName": "Email Service"         // From name
}
```

**For Gmail:**
1. Go to: https://myaccount.google.com/apppasswords
2. Generate an App Password
3. Use that password in the configuration

### Step 2: Run the Application

```bash
cd SendEmailApiThroughSmtp
dotnet run
```

You should see output like:
```
[2025-01-15 10:00:00.000 +00:00] [INF] Starting Email API application
[2025-01-15 10:00:01.000 +00:00] [INF] Email Processor Service is starting
[2025-01-15 10:00:01.000 +00:00] [INF] Email API started. Access Swagger UI at: http://localhost:5000/swagger
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 3: Test the API

Open your browser and go to:
**http://localhost:5000/swagger**

You'll see the Swagger UI with interactive API documentation!

**Features:**
- Try out all API endpoints directly from the browser
- Click "Authorize" button to enter your API key once
- View request/response schemas
- See all available endpoints and their documentation

## ?? Using API Key in Swagger

1. Open Swagger UI: http://localhost:5000/swagger
2. Click the **"Authorize"** button (top right, with a lock icon)
3. Enter your API key: `dev-api-key-12345-change-in-production`
4. Click **"Authorize"** then **"Close"**
5. Now you can test all endpoints!

## ?? Testing with cURL

### Send an Email
```bash
curl -X POST "http://localhost:5000/api/email/send" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-api-key-12345-change-in-production" \
  -d '{
    "to": "recipient@example.com",
    "subject": "Test Email",
    "body": "<h1>Hello!</h1><p>This is a test.</p>",
    "isHtml": true
  }'
```

### Check Email Status
```bash
curl -X GET "http://localhost:5000/api/email/1" \
  -H "X-API-Key: dev-api-key-12345-change-in-production"
```

### List All Emails
```bash
curl -X GET "http://localhost:5000/api/email" \
  -H "X-API-Key: dev-api-key-12345-change-in-production"
```

## ?? API Key

The development API key is: `dev-api-key-12345-change-in-production`

?? **Important**: Change this in production! Update in `appsettings.json`:
```json
"ApiKey": "your-secure-random-api-key-here"
```

## ?? API Documentation

Once running, access the interactive documentation at:
- **Swagger UI**: http://localhost:5000/swagger
- **Swagger JSON**: http://localhost:5000/swagger/v1/swagger.json

**Note**: The Swagger endpoints do NOT require API key authentication - they're publicly accessible for easy testing.

## ?? Checking Logs

Logs are written to:
- **Console**: Real-time logs in your terminal
- **Files**: `logs/log-YYYYMMDD.txt` (rolling daily)

## ?? Database

Email records are stored in `emails.db` (LiteDB) in the application directory.
You can view this database using LiteDB Studio: https://github.com/mbdavid/LiteDB.Studio

## ?? Test Requests

Use the included `test-requests.http` file with:
- Visual Studio Code with REST Client extension
- JetBrains Rider
- Visual Studio 2022

## Common Issues

### Issue: "Can't access Swagger UI"
**Solution**: 
- Make sure the app is running
- Try: http://localhost:5000/swagger
- The Swagger endpoint does NOT require an API key

### Issue: "Failed to send email"
**Solution**: Check your SMTP settings, especially:
- Correct host and port
- Valid username/password
- EnableSsl is set correctly
- For Gmail, using App Password (not regular password)

### Issue: "401 Unauthorized" in Swagger
**Solution**: 
1. Click the **"Authorize"** button in Swagger UI
2. Enter your API key: `dev-api-key-12345-change-in-production`
3. Click "Authorize" then "Close"
4. Try the request again

### Issue: Email stuck in "Pending" status
**Solution**: Check the logs in the console or `logs/` folder for error details.

## Next Steps

1. ? Configure your SMTP settings
2. ? Run the application
3. ? Test with Swagger UI at http://localhost:5000/swagger
4. ? Authorize with your API key in Swagger
5. ? Send a real email
6. ?? Change the API key for production
7. ?? Deploy to your server/cloud

## Support

For more detailed information, see `README.md`
