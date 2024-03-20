using Cocona;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (int parallelism, string absolutePath, CoconaAppContext ctx) =>
    {
        var loggerFactory = LoggerFactory.Create(options =>
        {
            options.AddSimpleConsole(opts => opts.TimestampFormat = "O");
            options.SetMinimumLevel(LogLevel.Information);
        });
        var globalLogger = loggerFactory.CreateLogger("global");

        var executors = new List<TestExecutor>();

        for (int i = 0; i < parallelism; i++)
        {
            executors.Add(
                new TestExecutor(loggerFactory, absolutePath, i.ToString(), ctx.CancellationToken)
            );
        }

        try
        {
            var tasks = executors.Select((x, i) => (x.Start(), i));
            await Task.WhenAll(tasks.Select(x => x.Item1));
            foreach (var task in tasks)
            {
                var outcome = await task.Item1;

                if (!outcome)
                {
                    globalLogger.LogWarning("Executor {0} failed to start", task.i.ToString());
                    throw new OperationCanceledException();
                }
            }

            globalLogger.LogInformation("All executors running");
            await Task.Delay(TimeSpan.FromMinutes(5), ctx.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            globalLogger.LogInformation("Received shutdown");
        }
        finally
        {
            var tasks = executors.Select(x => x.Stop());
            await Task.WhenAll(tasks);
        }
    }
);
