using backend.Aws;
using backend.Hangfire;
using Hangfire;
using Hangfire.Dashboard;
using model.Mappers;
using OpenTelemetry.Metrics;
using persistence;
using SseHandler;
using SseHandler.Metrics;

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
            .AddPrometheusExporter()
            .AddMeter(IDeviceMetrics.MeterName);
    });

builder.Services.AddHangfire(config =>
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage()
);
builder.Services.AddHangfireServer();

builder.Services.AddSotexBoxDatabase();
builder.Services.AddAutoMapper(typeof(CoreMapper).Assembly);

builder.Services.ConfigureAwsClient();

var app = builder.Build();

var result = app.Migrate();
if (!result.IsSuccessful && !app.Environment.IsEnvironment("test"))
{
    Environment.Exit(1);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("test"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

var host = app.Services.GetRequiredService<IHostApplicationLifetime>();
var connections = app.Services.GetRequiredService<IEventCoordinator>();
host.ApplicationStopping.Register(connections.RemoveAll);

app.EnqueueJobs();

// TODO: Setup custom authorization once we have roles
app.UseHangfireDashboard(
    "/hangfire",
    new DashboardOptions { IsReadOnlyFunc = (DashboardContext ctx) => true }
);

app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
