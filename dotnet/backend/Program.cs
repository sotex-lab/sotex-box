using OpenTelemetry.Metrics;
using SseHandler;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddEventCoordinator();

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});

builder
    .Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

var host = app.Services.GetRequiredService<IHostApplicationLifetime>();
var connections = app.Services.GetRequiredService<IEventCoordinator>();
host.ApplicationStopping.Register(connections.RemoveAll);

app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
