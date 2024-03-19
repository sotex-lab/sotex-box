using Cocona;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (string parallelism, string absolutePath, CoconaAppContext ctx) =>
    {
        var logger = LoggerFactory
            .Create(options =>
            {
                options.AddSimpleConsole(opts => opts.TimestampFormat = "O");
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
            .WithPortBinding(8000)
            .WithPortBinding(5050)
            .WithBindMount("/var/lib/docker/image", "/var/lib/docker/image")
            .WithBindMount("/var/lib/docker/overlay2", "/var/lib/docker/overlay2")
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

        logger.LogInformation("Starting our services");

        var composeResult = await stack.ExecAsync(["make", "compose-up-d"], ctx.CancellationToken);

        if (composeResult.ExitCode == 0)
        {
            logger.LogInformation("Our services started");
        }
        else
        {
            logger.LogWarning("Couldn't start our services due to:\n{0}", composeResult.Stderr);
            return;
        }

        var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

        attempt = 0;
        while (true)
        {
            if (ctx.CancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Shutdown requested");
                return;
            }
            logger.LogInformation("Attempt {0}: pinging backend", attempt++);
            try
            {
                var response = await client.GetAsync("http://localhost:8000/swagger/index.html");

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Backend started");
                    break;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadGateway)
                {
                    logger.LogInformation("Status code was bad gateway.");
                    await Task.Delay(1000);
                    continue;
                }

                logger.LogWarning("Backend responded with status code: {0}", response.StatusCode);

                if (attempt >= 5)
                {
                    logger.LogWarning("Our backend didn't start");
                    await Task.Delay(100000, ctx.CancellationToken);
                    return;
                }
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Received timeout, retrying");
            }
        }

        await stack.StopAsync();

        logger.LogInformation("Stack stopped");
    }
);
