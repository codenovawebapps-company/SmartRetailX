using Amazon;
using Amazon.SQS;
using InventoryService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Register the in-memory InventoryService as a Singleton
builder.Services.AddSingleton<InventoryService.Services.InventoryService>();

// Register AWS SQS Client (SDK automatically uses ECS Task Role credentials without hardcoded keys)
builder.Services.AddSingleton<IAmazonSQS>(sp => new AmazonSQSClient(RegionEndpoint.APSouth1));

// Register SQS Background Consumer
builder.Services.AddHostedService<SqsInventoryConsumer>();

var app = builder.Build();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseAuthorization();

// ECS / ALB Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "inventory-service",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();

app.Run();
