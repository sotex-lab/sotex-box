using System.Data.Common;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using persistence;
using Polly;
using Polly.Retry;
using Respawn;

public class TestExecutor
{
    private static int BACKEND_PORT = 8000;
    private static int DATABASE_PORT = 5432;
    private static int MINIO_PORT = 9000;
    private readonly IContainer testEnvironment;
    private readonly ILogger<TestExecutor> logger;
    private readonly ILoggerFactory loggerFactory;
    private readonly string name;
    private readonly CancellationToken token;
    private readonly ResiliencePipeline pipeline;
    private HttpClient? client;
    private RespawnerOptions respawnerOptions;
    private Respawner? respawner;
    private DbConnection? dbConnection;
    private ApplicationDbContext? applicationDbContext;
    private string absolutePath;

    public TestExecutor(
        ILoggerFactory logFactory,
        string path,
        string friendlyName = "",
        CancellationToken cancelToken = default
    )
    {
        absolutePath = path;
        loggerFactory = logFactory;
        logger = loggerFactory.CreateLogger<TestExecutor>();
        name = friendlyName;
        token = cancelToken;
        respawnerOptions = new RespawnerOptions
        {
            SchemasToInclude = ["public"],
            DbAdapter = DbAdapter.Postgres
        };
        Info("Creating test environment");

        testEnvironment = new ContainerBuilder()
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .WithName($"e2e-tester-{name}")
            .WithImage("e2e")
            .WithPrivileged(true)
            .WithBindMount("/var/lib/docker/overlay2", "/var/lib/docker/overlay2")
            .WithBindMount("/var/lib/docker/image", "/var/lib/docker/image")
            .WithBindMount($"{absolutePath}/.git", "/sotex/.git")
            .WithBindMount($"{absolutePath}/distribution", "/sotex/distribution")
            .WithBindMount($"{absolutePath}/dotnet/backend", "/sotex/dotnet/backend")
            .WithBindMount($"{absolutePath}/dotnet/model", "/sotex/dotnet/model")
            .WithBindMount($"{absolutePath}/dotnet/persistence", "/sotex/dotnet/persistence")
            .WithBindMount($"{absolutePath}/dotnet/sse-handler", "/sotex/dotnet/sse-handler")
            .WithBindMount($"{absolutePath}/infra/config", "/sotex/infra/config")
            .WithBindMount($"{absolutePath}/python", "/sotex/python")
            .WithBindMount($"{absolutePath}/Makefile", "/sotex/Makefile")
            .WithBindMount($"{absolutePath}/poetry.lock", "/sotex/poetry.lock")
            .WithBindMount($"{absolutePath}/pyproject.toml", "/sotex/pyproject.toml")
            .WithBindMount($"{absolutePath}/requirements.txt", "/sotex/requirements.txt")
            .WithBindMount($"{absolutePath}/docker-compose.yaml", "/sotex/docker-compose.yaml")
            .WithPortBinding(BACKEND_PORT, true)
            .WithPortBinding(DATABASE_PORT, true)
            .WithPortBinding(MINIO_PORT, true)
            .Build();

        Info("Created test environment");

        var options = new RetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            MaxRetryAttempts = 5,
            MaxDelay = TimeSpan.FromSeconds(10)
        };

        pipeline = new ResiliencePipelineBuilder().AddRetry(options).Build();
    }

    public async Task<bool> Start()
    {
        if (!await StartEnvironment())
            return false;

        if (!await OverrideEnvVariables())
            return false;

        if (!await StartStack())
            return false;

        client = new HttpClient()
        {
            BaseAddress = new Uri(
                string.Format(
                    "http://localhost:{0}",
                    testEnvironment.GetMappedPublicPort(BACKEND_PORT)
                )
            ),
            Timeout = TimeSpan.FromSeconds(10)
        };

        if (!await EnsureBackendStarted())
            return false;

        if (!await ConfigureDatabaseConnection())
            return false;

        Info("Stack started successfully");
        return true;
    }

    public async Task Stop()
    {
        Info("Stopping stack");
        await testEnvironment.StopAsync();
    }

    private async Task ResetStorages()
    {
        await respawner!.ResetAsync(dbConnection!);

        var buckets = new string[] { "non-processed" };

        foreach (var bucket in buckets)
        {
            var output = await testEnvironment.ExecAsync(
                ["docker", "exec", "minio", "mc", "rm", "--recursive", "--force", $"/data/{bucket}"]
            );

            if (output.ExitCode != 0)
            {
                Warn("Couldn't clean minio bucket {0}, Stderr: \n{1}", bucket, output.Stderr);
            }
        }
    }

    public async Task<IEnumerable<TestSummary>> TestBatch(Type[] tests)
    {
        var summaries = new List<TestSummary>();

        var options = new RetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 3,
            MaxDelay = TimeSpan.FromSeconds(10),
            OnRetry = async (_) =>
            {
                await ResetStorages();
            }
        };
        var pipeline = new ResiliencePipelineBuilder().AddRetry(options).Build();
        var logger = loggerFactory.CreateLogger<E2ETest>();
        var testContext = new E2ECtx(
            logger,
            pipeline,
            testEnvironment.GetMappedPublicPort(BACKEND_PORT),
            $"{absolutePath}/dotnet/e2e-tester/Resources",
            applicationDbContext!,
            token
        );

        Info("Batch contains {0} tests. Running...", tests.Length);

        foreach (var test in tests)
        {
            E2ETest instance;
            try
            {
                instance = (E2ETest)Activator.CreateInstance(test, testContext)!;
            }
            catch (Exception e)
            {
                Error("Error during test initialization: {0}", e.Message);
                var badSummary = new TestSummary
                {
                    Description = "unknown",
                    Name = test.Name,
                    ErrorMessage = string.Format("Error during test initialization: {0}", e.Message)
                };
                summaries.Add(badSummary);
                continue;
            }

            summaries.Add(await instance.Test());
        }

        Info("Batch finished");

        return summaries;
    }

    private async Task<bool> ConfigureDatabaseConnection()
    {
        try
        {
            applicationDbContext = new ApplicationDbContextFactory().CreateDbContext(
                $"Host=localhost;Port={testEnvironment.GetMappedPublicPort(DATABASE_PORT)};Username=postgres;Password=postgres;Database=postgres"
            );

            dbConnection = applicationDbContext.Database.GetDbConnection();
            await dbConnection.OpenAsync(token);

            respawner = await Respawner.CreateAsync(dbConnection, respawnerOptions);

            Info("Database communication established");
        }
        catch (Exception e)
        {
            Error("Error while creating database connection: {0}", e.Message);
            return false;
        }
        return true;
    }

    private async Task<bool> EnsureBackendStarted()
    {
        try
        {
            await pipeline.ExecuteAsync(
                static async (executor, token) =>
                {
                    executor.Info("Pinging backend to see if its up");

                    var response = await executor.client!.GetAsync("/swagger/index.html");

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    executor.Warn("Ping failed with status code: {0}", response.StatusCode);
                    throw new KeyNotFoundException("backend");
                },
                this,
                token
            );
        }
        catch (Exception e)
        {
            Error(e.Message);
            return false;
        }
        return true;
    }

    private async Task<bool> StartStack()
    {
        Info("Starting our stack");

        var composeResult = await testEnvironment.ExecAsync(["make", "compose-up-d"], token);

        if (composeResult.ExitCode != 0)
        {
            Error(
                "Received exit status code {0}: \n{1}",
                composeResult.ExitCode,
                composeResult.Stderr
            );
            return false;
        }

        return true;
    }

    private async Task<bool> OverrideEnvVariables()
    {
        var commmands = new[]
        {
            new[]
            {
                "sed",
                "-i",
                $"s/:9000/:{testEnvironment.GetMappedPublicPort(MINIO_PORT)}/g",
                ".env"
            },
            ["sed", "-i", $"s/REQUIRE_KNOWN_DEVICES=false/REQUIRE_KNOWN_DEVICES=true/g", ".env"]
        };

        Info("Overriding environment variables");

        foreach (var command in commmands)
        {
            var writeResult = await testEnvironment.ExecAsync(command);
            if (writeResult.ExitCode != 0)
            {
                Error(
                    "Received exit status code {0}: \n{1}",
                    writeResult.ExitCode,
                    writeResult.Stderr
                );
                return false;
            }
        }

        return true;
    }

    private async Task<bool> StartEnvironment()
    {
        Info("Starting test environment");

        await testEnvironment.StartAsync(token);

        if (!await EnvironmentStarted())
            return false;

        Info("Started test environment");

        return true;
    }

    private async Task<bool> EnvironmentStarted()
    {
        try
        {
            await pipeline.ExecuteAsync(
                static async (executor, token) =>
                {
                    executor.Info("Pinging test environment");
                    var result = await executor.testEnvironment.ExecAsync(
                        ["docker", "info"],
                        token
                    );

                    if (!result.Stdout.EndsWith("Server:\n"))
                    {
                        return;
                    }

                    executor.Warn("Test environment still not started...");
                    throw new KeyNotFoundException("docker");
                },
                this,
                token
            );
        }
        catch (Exception e)
        {
            Error(e.Message);
            return false;
        }
        return true;
    }

    private void Info(string message, params object[] args) =>
        logger.LogInformation("Executor {0}: {1}", name, string.Format(message, args));

    private void Warn(string message, params object[] args) =>
        logger.LogWarning("Executor {0}: {1}", name, string.Format(message, args));

    private void Error(string message, params object[] args) =>
        logger.LogError("Executor {0}: {1}", name, string.Format(message, args));
}
