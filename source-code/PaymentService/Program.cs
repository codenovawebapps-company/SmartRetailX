using Amazon.EventBridge;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// AWS EventBridge configuration
builder.Services.AddSingleton<IAmazonEventBridge, AmazonEventBridgeClient>();
builder.Services.AddSingleton<EventPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root → Swagger UI so http://localhost:5005/ works
app.MapGet("/", () => Results.Redirect("/swagger"));

// UseHttpsRedirection disabled for local/Docker dev & ECS ALB environments (HTTP only)

app.UseAuthorization();

// Health check endpoint for ALB — no auth required
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "payment-service",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();

app.Run();
