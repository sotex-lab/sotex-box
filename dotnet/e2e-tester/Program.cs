using System.Reflection;
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

            globalLogger.LogInformation("Discoverying tests...");
            var types = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    type.IsClass
                    && !type.IsAbstract
                    && type.IsSubclassOf(typeof(E2ETest))
                    && type.Namespace != null
                    && type.Namespace.StartsWith("e2e_tester")
                );

            var testTasks = types
                .Select((item, index) => new { Item = item, BatchIndex = index / executors.Count })
                .GroupBy(x => x.BatchIndex, x => x.Item)
                .Select(index => executors[index.Key].TestBatch(index.ToArray()));

            await Task.WhenAll(testTasks);

            globalLogger.LogInformation("All tests finished");
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
