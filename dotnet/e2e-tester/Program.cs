using Cocona;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (string parallelism, string absolutePath, CoconaAppContext ctx) =>
    {
        var logger = LoggerFactory
            .Create(options =>
            {
                options.AddConsole();
                options.SetMinimumLevel(LogLevel.Information);
            })
            .CreateLogger("e2e-tests");

        logger.LogInformation("Creating stack");

        var stack = new ContainerBuilder()
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .WithName("e2e-tester")
            .WithImage("e2e")
            .WithPrivileged(true)
            .Build();

        await stack.StartAsync();

        logger.LogInformation("Stack created");
        var attempt = 0;

        while (true)
        {
            var result = await stack.ExecAsync(["docker", "info"]);
            if (!result.Stdout.EndsWith("Server:\n"))
            {
                logger.LogInformation("Stack started");
                break;
            }
            if (attempt >= 5)
            {
                logger.LogWarning("Stack didn't start");
                return;
            }
            if (ctx.CancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Shutdown requested");
                return;
            }

            logger.LogInformation("Waiting for stack to start");
            await Task.Delay(1000);
            attempt++;
        }

        await stack.StopAsync();

        logger.LogInformation("Stack stopped");
    }
);
