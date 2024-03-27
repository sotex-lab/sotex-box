using System.Data.Common;
using Amazon.S3;
using Amazon.SQS;
using backend.Aws;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using persistence;
using Respawn;
using SseHandler;
using Testcontainers.PostgreSql;

public class ConfigurableBackendFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    public const string IntegrationCollection = "integration collection";
    public const int IntegrationCronIntervalSeconds = 15;
    public Dictionary<Guid, Connection> Connections { get; set; } =
        new Dictionary<Guid, Connection>();

    private Respawner? respawner;
    private DbConnection? connection;
    private IContainer? minioContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var topLevel = Environment.GetEnvironmentVariable("TOP_LEVEL")!;

        var environmentVars = new Dictionary<string, string>
        {
            ["MINIO_ROOT_USER"] = "admin",
            ["MINIO_ROOT_PASSWORD"] = "admin123",
        };

        var postgresContainer = new PostgreSqlBuilder()
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .WithName("postgres")
            .Build();

        minioContainer = new ContainerBuilder()
            .WithImage("quay.io/minio/minio:RELEASE.2024-03-07T00-43-48Z")
            .WithPortBinding(9000)
            .WithPortBinding(9001)
            .WithEnvironment(environmentVars)
            .WithCleanUp(true)
            .WithAutoRemove(true)
            .WithCommand(["server", "/data", "--console-address", ":9001"])
            .WithName("minio")
            .Build();

        var sqsContainer = new ContainerBuilder()
            .WithName("sqs")
            .WithImage("softwaremill/elasticmq-native")
            .WithPortBinding(9324, 9324)
            .WithPortBinding(9325, 9325)
            .WithBindMount($"{topLevel}/infra/config/sqs/elasticmq.conf", "/opt/elasticmq.conf")
            .Build();

        Task.WhenAll(
                [
                    postgresContainer.StartAsync(),
                    minioContainer.StartAsync(),
                    sqsContainer.StartAsync()
                ]
            )
            .Wait();

        var backendEnvVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "test",
            ["CONNECTION_STRING"] = postgresContainer.GetConnectionString(),
            ["AWS_S3_URL"] = "http://localhost:9000",
            ["AWS_REGION"] = "localhost",
            ["AWS_S3_ACCESS_KEY"] = environmentVars["MINIO_ROOT_USER"],
            ["AWS_S3_SECRET_KEY"] = environmentVars["MINIO_ROOT_PASSWORD"],
            ["AWS_PROTOCOL"] = "http",
            ["AWS_PROXY_HOST"] = "localhost",
            ["AWS_PROXY_PORT"] = "9000",
            ["AWS_SQS_URL"] = "http://localhost:9324",
            ["AWS_SQS_ACCESS_KEY"] = "x",
            ["AWS_SQS_SECRET_KEY"] = "x",
            ["AWS_SQS_NONPROCESSED_QUEUE_URL"] = "/000000000000/nonprocessed",
            ["REQUIRE_KNOWN_DEVICES"] = "false",
        };

        foreach (var kvp in backendEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }

        var respawnerOptions = new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        };

        builder.ConfigureServices(services =>
        {
            var eventCoordinatorDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Singleton
                && x.ServiceType == typeof(IEventCoordinator)
            );
            services.Remove(eventCoordinatorDescriptor);

            services.AddEventCoordinator(Connections);

            var databaseDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Scoped
                && x.ServiceType == typeof(ApplicationDbContext)
            );
            services.Remove(databaseDescriptor);

            var context = new ApplicationDbContextFactory().CreateDbContext(
                postgresContainer.GetConnectionString()
            );

            context.Database.Migrate();

            connection = context.Database.GetDbConnection();
            connection.Open();

            respawner = Respawner
                .CreateAsync(connection, respawnerOptions)
                .GetAwaiter()
                .GetResult();

            services.AddSotexBoxDatabase(postgresContainer.GetConnectionString());

            var s3Descriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Singleton && x.ServiceType == typeof(IAmazonS3)
            );
            services.Remove(s3Descriptor);

            var sqsDescriptor = services.Single(x =>
                x.Lifetime == ServiceLifetime.Singleton && x.ServiceType == typeof(IAmazonSQS)
            );
            services.Remove(sqsDescriptor);

            services.ConfigureAwsClients();
        });
    }

    public void ResetStorages()
    {
        respawner!.ResetAsync(connection!).Wait();

        var buckets = new string[] { "non-processed" };

        foreach (var bucket in buckets)
        {
            minioContainer!
                .ExecAsync(["mc", "rm", "--recursive", "--force", $"/data/{bucket}"])
                .Wait();
        }
    }
}

[CollectionDefinition(ConfigurableBackendFactory.IntegrationCollection)]
public class BackendCollection : ICollectionFixture<ConfigurableBackendFactory> { }
