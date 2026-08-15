var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root → Swagger UI so http://localhost:5003/ works
app.MapGet("/", () => Results.Redirect("/swagger"));

// UseHttpsRedirection disabled for local/Docker dev (HTTP only)

app.UseAuthorization();

// Health check endpoint for ALB — no auth required
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "order-service",
    timestamp = DateTime.UtcNow
})).AllowAnonymous();

app.MapControllers();

app.Run();
