using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

public class TestExecutor
{
    private static int BACKEND_PORT = 8000;
    private static int DATABASE_PORT = 5050;
    private static int MINIO_PORT = 9000;
    private readonly IContainer testEnvironment;
    private readonly ILogger<TestExecutor> logger;
    private readonly string name;
    private readonly CancellationToken token;
    private readonly ResiliencePipeline pipeline;
    private HttpClient? client;

    public TestExecutor(
        ILoggerFactory loggerFactory,
        string friendlyName = "",
        CancellationToken cancelToken = default
    )
    {
        logger = loggerFactory.CreateLogger<TestExecutor>();
        name = friendlyName;
        token = cancelToken;
        Info("Creating test environment");

        testEnvironment = new ContainerBuilder()
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .WithName($"e2e-tester-{name}")
            .WithImage("e2e")
            .WithPrivileged(true)
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
        Info("Starting test environment");

        await testEnvironment.StartAsync(token);
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

        Info("Started test environment");

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

        Info("Stack started successfully");
        return true;
    }

    public async Task Stop()
    {
        Info("Stopping stack");
        await testEnvironment.StopAsync();
    }

    private void Info(string message, params object[] args) =>
        logger.LogInformation("Executor {0}: {1}", name, string.Format(message, args));

    private void Warn(string message, params object[] args) =>
        logger.LogWarning("Executor {0}: {1}", name, string.Format(message, args));

    private void Error(string message, params object[] args) =>
        logger.LogError("Executor {0}: {1}", name, string.Format(message, args));
}
