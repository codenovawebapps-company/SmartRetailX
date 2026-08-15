using Amazon.SQS;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// AWS SQS registration
builder.Services.AddSingleton<IAmazonSQS, AmazonSQSClient>();

// Custom Services
builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddSingleton<MockNotificationSender>();
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddHostedService<SqsEventConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root → Swagger UI so http://localhost:5004/ works
app.MapGet("/", () => Results.Redirect("/swagger"));

// UseHttpsRedirection disabled for local/Docker dev & ECS ALB environments (HTTP only)

app.UseAuthorization();

// Health check endpoint for ALB — no auth required
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "notification-service",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();

app.Run();
