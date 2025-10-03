using Serilog;
using SendEmailApiThroughSmtp.Configuration;
using SendEmailApiThroughSmtp.Models;
using SendEmailApiThroughSmtp.Services;
using SendEmailApiThroughSmtp.Middleware;
using Microsoft.OpenApi.Models;

namespace SendEmailApiThroughSmtp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build())
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithEnvironmentName()
                .CreateLogger();

            try
            {
                Log.Information("Starting Email API application");

                var builder = WebApplication.CreateBuilder(args);

                // Force known URLs for local development so Swagger is reachable
                // This makes the app listen on http://localhost:5000
                builder.WebHost.UseUrls("http://localhost:5000");

                // Add Serilog
                builder.Host.UseSerilog();

                // Add services to the container
                builder.Services.AddAuthorization();
                builder.Services.AddEndpointsApiExplorer();
                
                // Add Swagger
                builder.Services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Email API",
                        Version = "v1",
                        Description = "API for sending emails through SMTP with queue processing and LiteDB storage",
                        Contact = new OpenApiContact
                        {
                            Name = "Email API Support"
                        }
                    });

                    // Add API Key authentication to Swagger
                    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                    {
                        Description = "API Key authentication. Enter your API key in the text input below.",
                        Name = "X-API-Key",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "ApiKeyScheme"
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                // Configure SMTP settings
                builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

                // Register services
                builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
                builder.Services.AddSingleton<IEmailRepository, EmailRepository>();
                builder.Services.AddScoped<IEmailService, EmailService>();
                builder.Services.AddHostedService<EmailProcessorService>();

                var app = builder.Build();

                // Add API key authentication middleware FIRST
                app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

                app.UseAuthorization();

                // Configure Swagger UI (always available for easy testing)
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Email API v1");
                    options.RoutePrefix = "swagger";
                    options.DocumentTitle = "Email API Documentation";
                    options.DisplayRequestDuration();
                });

                // API Endpoints
                app.MapPost("/api/email/send", async (EmailRequest request, IEmailRepository repository, IEmailQueue queue, ILogger<Program> logger) =>
                {
                    try
                    {
                        logger.LogInformation("Received email request to: {To}, Subject: {Subject}", request.To, request.Subject);

                        // Validate request
                        if (string.IsNullOrWhiteSpace(request.To))
                        {
                            logger.LogWarning("Email request validation failed: To address is required");
                            return Results.BadRequest(new { error = "To address is required" });
                        }

                        if (string.IsNullOrWhiteSpace(request.Subject))
                        {
                            logger.LogWarning("Email request validation failed: Subject is required");
                            return Results.BadRequest(new { error = "Subject is required" });
                        }

                        if (string.IsNullOrWhiteSpace(request.Body))
                        {
                            logger.LogWarning("Email request validation failed: Body is required");
                            return Results.BadRequest(new { error = "Body is required" });
                        }

                        // Create email message
                        var email = new EmailMessage
                        {
                            To = request.To,
                            Subject = request.Subject,
                            Body = request.Body,
                            IsHtml = request.IsHtml,
                            Cc = request.Cc,
                            Bcc = request.Bcc,
                            Status = EmailStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Save to database
                        var emailId = repository.Add(email);
                        email.Id = emailId;

                        // Add to queue
                        await queue.EnqueueAsync(emailId);

                        logger.LogInformation("Email {EmailId} queued successfully", emailId);

                        return Results.Ok(new
                        {
                            id = emailId,
                            message = "Email queued successfully",
                            status = email.Status.ToString()
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error queuing email: {ErrorMessage}", ex.Message);
                        return Results.Problem("An error occurred while queuing the email");
                    }
                })
                .WithName("SendEmail")
                .WithDescription("Queue an email to be sent via SMTP")
                .WithTags("Email")
                .WithOpenApi();

                app.MapGet("/api/email/{id}", (int id, IEmailRepository repository, ILogger<Program> logger) =>
                {
                    try
                    {
                        logger.LogInformation("Fetching email with ID: {EmailId}", id);
                        var email = repository.GetById(id);

                        if (email == null)
                        {
                            logger.LogWarning("Email {EmailId} not found", id);
                            return Results.NotFound(new { error = "Email not found" });
                        }

                        return Results.Ok(email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching email {EmailId}: {ErrorMessage}", id, ex.Message);
                        return Results.Problem("An error occurred while fetching the email");
                    }
                })
                .WithName("GetEmail")
                .WithDescription("Get email status by ID")
                .WithTags("Email")
                .WithOpenApi();

                app.MapGet("/api/email", (IEmailRepository repository, ILogger<Program> logger) =>
                {
                    try
                    {
                        logger.LogInformation("Fetching all emails");
                        var emails = repository.GetAll();
                        return Results.Ok(emails);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching emails: {ErrorMessage}", ex.Message);
                        return Results.Problem("An error occurred while fetching emails");
                    }
                })
                .WithName("GetAllEmails")
                .WithDescription("Get all emails")
                .WithTags("Email")
                .WithOpenApi();

                app.MapGet("/api/email/pending", (IEmailRepository repository, ILogger<Program> logger) =>
                {
                    try
                    {
                        logger.LogInformation("Fetching pending emails");
                        var emails = repository.GetPendingEmails();
                        return Results.Ok(emails);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error fetching pending emails: {ErrorMessage}", ex.Message);
                        return Results.Problem("An error occurred while fetching pending emails");
                    }
                })
                .WithName("GetPendingEmails")
                .WithDescription("Get all pending emails")
                .WithTags("Email")
                .WithOpenApi();

                Log.Information("Email API started. Access Swagger UI at: http://localhost:5000/swagger");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
