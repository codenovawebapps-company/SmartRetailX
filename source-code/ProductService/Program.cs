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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
    c.RoutePrefix = "swagger";
});

// Redirect root → Swagger UI so http://localhost:5002/ works
app.MapGet("/", () => Results.Redirect("/swagger"));

// UseHttpsRedirection disabled for local/Docker dev (HTTP only)

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
