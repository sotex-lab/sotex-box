using System.Data;
using System.Reflection;
using Cocona;
using ConsoleTables;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (int parallelism, string absolutePath, string? log, CoconaAppContext ctx) =>
    {
        var testSummaries = new List<TestSummary>();
        var loggerFactory = LoggerFactory.Create(options =>
        {
            options.AddSimpleConsole(opts => opts.TimestampFormat = "O");
            options.SetMinimumLevel(
                log switch
                {
                    "warn" => LogLevel.Warning,
                    "error" => LogLevel.Error,
                    "trace" => LogLevel.Trace,
                    "debug" => LogLevel.Debug,
                    _ => LogLevel.Information,
                }
            );
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
            foreach (var task in testTasks)
            {
                testSummaries.AddRange(await task);
            }

            globalLogger.LogInformation("Received {0} tests", testSummaries.Count());

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
            if (!testSummaries.Any())
                Environment.Exit(0);

            ConsoleTable
                .From(
                    testSummaries.Select(x => new
                    {
                        x.Name,
                        Result = x.Outcome ? "✅" : "❌",
                        Duration = string.Format("{0}s", x.Elapsed),
                        x.Retries,
                        Error = string.IsNullOrEmpty(x.ErrorMessage) ? "/" : x.ErrorMessage,
                        x.Description
                    })
                )
                .Write(Format.Alternative);

            var succeeded = testSummaries.Count(x => x.Outcome);
            var succeededProcentage = Math.Round(100 * (double)succeeded / testSummaries.Count);

            ConsoleTable
                .From(
                    new[]
                    {
                        new
                        {
                            Result = "Succeeded",
                            Count = succeeded,
                            Procentage = string.Format("{0}%", succeededProcentage)
                        },
                        new
                        {
                            Result = "Failed",
                            Count = testSummaries.Count - succeeded,
                            Procentage = string.Format("{0}%", 100 - succeededProcentage)
                        },
                    }
                )
                .Write(Format.Alternative);

            Environment.Exit(succeeded == testSummaries.Count ? 0 : 1);
        }
    }
);
