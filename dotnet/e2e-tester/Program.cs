using Cocona;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (string parallelism, CoconaAppContext ctx) =>
    {
        var loggerFactory = LoggerFactory.Create(options =>
        {
            options.AddSimpleConsole(opts => opts.TimestampFormat = "O");
            options.SetMinimumLevel(LogLevel.Information);
        });
        var globalLogger = loggerFactory.CreateLogger("global");
        var testExecutor = new TestExecutor(loggerFactory, "test-executor", ctx.CancellationToken);
        try
        {
            var outcome = await testExecutor.Start();
            if (outcome)
            {
                await Task.Delay(100_000, ctx.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            globalLogger.LogInformation("Received shutdown");
        }
        finally
        {
            await testExecutor.Stop();
        }
    }
);
