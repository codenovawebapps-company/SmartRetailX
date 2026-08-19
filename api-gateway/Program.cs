var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/", () => Results.Ok(new
{
    service = "SmartRetailX API Gateway",
    status = "running",
    version = "v1",
    timestamp = DateTime.UtcNow,
    routes = new[]
    {
        "/api/v1/auth/*",
        "/api/v1/users/*",
        "/api/v1/products/*",
        "/api/v1/orders/*",
        "/api/v1/inventory/*",
        "/api/v1/payments/*",
        "/api/v1/notifications/*"
    }
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    gateway = "api-gateway",
    timestamp = DateTime.UtcNow
}));

app.MapReverseProxy();

app.Run();
