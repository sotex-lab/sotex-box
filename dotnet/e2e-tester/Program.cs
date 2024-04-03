using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Alba.CsConsoleFormat;
using Alba.CsConsoleFormat.Fluent;
using Cocona;
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
            var tasks = executors.Select((x, i) => (x.Start(), i)).ToList();
            await Task.WhenAll(tasks.Select(x => x.Item1));
            foreach (var task in tasks)
            {
                var outcome = task.Item1.Result;

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
        catch (Exception e)
        {
            globalLogger.LogError("Received error: {0}", e);
        }
        finally
        {
            var tasks = executors.Select(x => x.Stop());
            await Task.WhenAll(tasks);
            if (!testSummaries.Any())
                Environment.Exit(0);

            var legend = new Document()
            {
                Children =
                {
                    new Grid
                    {
                        Columns = { GridLength.Auto, GridLength.Auto },
                        Children =
                        {
                            new Cell("Symbol"),
                            new Cell("Meaning"),
                            new[]
                            {
                                new { Legend = "✓".Green(), Meaning = "successful" },
                                new { Legend = "X".Red(), Meaning = "failed" },
                                new
                                {
                                    Legend = "!".Yellow(),
                                    Meaning = "failed but allowed to fail"
                                }
                            }.Select(x =>
                                new[]
                                {
                                    new Cell(x.Legend) { Align = Align.Center },
                                    new Cell(x.Meaning)
                                }
                            )
                        },
                        AutoPosition = true
                    }
                }
            };
            ConsoleRenderer.RenderDocument(legend);

            var summaries = new Document(
                new Grid
                {
                    Columns =
                    {
                        GridLength.Auto,
                        GridLength.Auto,
                        GridLength.Auto,
                        GridLength.Auto,
                        GridLength.Auto,
                        GridLength.Auto
                    },
                    Children =
                    {
                        new Cell("Name") { Align = Align.Center },
                        new Cell("Result") { Align = Align.Center },
                        new Cell("Duration") { Align = Align.Center },
                        new Cell("Retries") { Align = Align.Center },
                        new Cell("Error") { Align = Align.Center },
                        new Cell("Description") { Align = Align.Center },
                        testSummaries
                            .Select(x => new
                            {
                                x.Name,
                                Result = x.Outcome
                                    ? "✓".Green()
                                    : x.AllowFail
                                        ? "!".Yellow()
                                        : "X".Red(),
                                Duration = string.Format("{0}s", x.Elapsed),
                                x.Retries,
                                Error = string.IsNullOrEmpty(x.ErrorMessage) ? "/" : x.ErrorMessage,
                                x.Description,
                            })
                            .Select(x =>
                                new[]
                                {
                                    new Cell(x.Name),
                                    new Cell(x.Result) { Align = Align.Center },
                                    new Cell(x.Duration),
                                    new Cell(x.Retries) { Align = Align.Center },
                                    new Cell(x.Error) { TextWrap = TextWrap.WordWrap },
                                    new Cell(x.Description) { TextWrap = TextWrap.WordWrap },
                                }
                            )
                    }
                }
            );
            ConsoleRenderer.RenderDocument(summaries);

            var succeeded = testSummaries.Count(x => x.Outcome);
            var succeededProcentage = Math.Round(100 * (double)succeeded / testSummaries.Count, 2);

            var overview = new Document(
                new Grid
                {
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto },
                    Children =
                    {
                        new Cell("Result"),
                        new Cell("Count"),
                        new Cell("Percentage"),
                        new[]
                        {
                            new
                            {
                                Result = "Succeeded".Green(),
                                Count = succeeded.ToString().Green(),
                                Percentage = string.Format("{0}%", succeededProcentage).Green()
                            },
                            new
                            {
                                Result = "Failed".Red(),
                                Count = (testSummaries.Count - succeeded).ToString().Red(),
                                Percentage = string.Format("{0}%", 100 - succeededProcentage).Red()
                            },
                        }.Select(x =>
                            new[]
                            {
                                new Cell(x.Result),
                                new Cell(x.Count) { Align = Align.Center },
                                new Cell(x.Percentage) { Align = Align.Center },
                            }
                        )
                    }
                }
            );
            ConsoleRenderer.RenderDocument(overview);

            Environment.Exit(
                succeeded >= testSummaries.Count(x => x.Outcome && !x.AllowFail) ? 0 : 1
            );
        }
    }
);
