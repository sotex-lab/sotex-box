using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Cocona;
using ConsoleTables;
using DotNet.Testcontainers.Configurations;
using DotNext.Collections.Generic;
using Microsoft.Extensions.Logging;

CoconaApp.Run(
    async (int parallelism, string absolutePath, string? log, CoconaAppContext ctx) =>
    {
        TestcontainersSettings.Logger = LoggerFactory
            .Create(options =>
            {
                options.SetMinimumLevel(LogLevel.Critical);
            })
            .CreateLogger("testcontainers");

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
        var servers = new List<TcpListener>();

        for (int i = 0; i < parallelism; i++)
        {
            var server = new TcpListener(IPAddress.Loopback, 0);
            server.Start();
            servers.Add(server);
            var port = ((IPEndPoint)server.LocalEndpoint).Port;

            executors.Add(
                new TestExecutor(
                    loggerFactory,
                    absolutePath,
                    port,
                    i.ToString(),
                    ctx.CancellationToken
                )
            );
        }

        servers.ForEach(x => x.Stop());

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

            var concurrentStack = new ConcurrentStack<Type>();
            concurrentStack.PushRange(types.ToArray());

            var testTasks = executors.Select(x =>
                Task.Run(async () => await x.TestBatch(concurrentStack), ctx.CancellationToken)
            );

            foreach (var summaries in await Task.WhenAll(testTasks))
            {
                testSummaries.AddRange(summaries);
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
                    new[]
                    {
                        new { Legend = "✅", Meaning = "successful" },
                        new { Legend = "❌", Meaning = "failed" },
                        new { Legend = "❕", Meaning = "failed but allowed to fail" }
                    }
                )
                .Write(Format.Alternative);

            ConsoleTable
                .From(
                    testSummaries.Select(x => new
                    {
                        x.Name,
                        Result = x.Outcome
                            ? "✅"
                            : x.AllowFail
                                ? "❕"
                                : "❌",
                        Duration = string.Format("{0}s", x.Elapsed),
                        x.Retries,
                        Error = string.IsNullOrEmpty(x.ErrorMessage) ? "/" : x.ErrorMessage,
                    })
                )
                .Write(Format.Alternative);

            var succeeded = testSummaries.Count(x => x.Outcome);
            var succeededProcentage = Math.Round(100 * (double)succeeded / testSummaries.Count, 2);

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

            Environment.Exit(
                succeeded >= testSummaries.Count(x => x.Outcome && !x.AllowFail) ? 0 : 1
            );
        }
    }
);
