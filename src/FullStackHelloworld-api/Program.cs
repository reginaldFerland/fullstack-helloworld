using FullStackHelloworld_api.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<ExtendedHealthChecks>("LiveCheck", HealthStatus.Degraded, new[] {"ready"});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configuring a ready vs live check as based on https://stackoverflow.com/a/60509645
var healthReadyUrl = Environment.GetEnvironmentVariable("healthReadyUrl") ?? "/health/ready";
app.MapHealthChecks(healthReadyUrl, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    ResultStatusCodes = new Dictionary<HealthStatus, int>
    {
        { HealthStatus.Healthy, 200 },
        { HealthStatus.Degraded, 207 },
        { HealthStatus.Unhealthy, 503 },
    }
});

var healthLiveUrl = Environment.GetEnvironmentVariable("healthLiveUrl") ?? "/health/live";
app.MapHealthChecks(healthLiveUrl, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = (_) => false,
    ResultStatusCodes = new Dictionary<HealthStatus, int>
    {
        { HealthStatus.Healthy, 200 },
        { HealthStatus.Degraded, 207 },
        { HealthStatus.Unhealthy, 503 },
    }
});


app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
