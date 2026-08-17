using Amazon.SQS;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Register Notification logic service
builder.Services.AddSingleton<NotificationService.Services.NotificationService>();

// AWS SQS & Supporting Services
builder.Services.AddSingleton<IAmazonSQS, AmazonSQSClient>();
builder.Services.AddSingleton<NotificationStore>();
builder.Services.AddSingleton<MockNotificationSender>();
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddHostedService<SqsEventConsumer>();

var app = builder.Build();

// Configure Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notification Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseAuthorization();

// ECS / ALB Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "notification-service",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();

app.Run();
