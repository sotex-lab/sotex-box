using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Polly;

public abstract class E2ETest
{
    private readonly E2ECtx ctx;
    private TestSummary summary;

    public E2ETest(E2ECtx c)
    {
        ctx = c;
        summary = new TestSummary
        {
            Description = Description(),
            Name = Name(),
            AllowFail = AllowFail(),
            Retries = -1
        };
    }

    public async Task<TestSummary> Test()
    {
        try
        {
            await ctx.Pipeline.ExecuteAsync(
                static async (test, token) =>
                {
                    var start = DateTime.Now;
                    test.Info("Test started");
                    test.summary.Retries++;

                    await test.Run(token);

                    test.summary.Elapsed = DateTime.Now.Subtract(start);
                    test.summary.Outcome = true;
                    test.Info("Test finished in {0}s", test.summary.Elapsed.TotalSeconds);
                },
                this,
                ctx.Token
            );
        }
        catch (Exception e)
        {
            var message = Regex.Replace(string.Join(' ', e.Message.Split()), @"\s+", " ");
            Error(message);
            summary.ErrorMessage = message;
        }

        return summary;
    }

    protected abstract string Name();
    protected abstract string Description();

    protected virtual bool AllowFail() => false;

    protected abstract Task Run(CancellationToken token);

    protected void Info(string message, params object[] args) =>
        ctx.Logger.LogInformation("Test {0}: {1}", Name(), string.Format(message, args));

    protected void Warn(string message, params object[] args) =>
        ctx.Logger.LogWarning("Test {0}: {1}", Name(), string.Format(message, args));

    protected void Error(string message, params object[] args) =>
        ctx.Logger.LogError("Test {0}: {1}", Name(), string.Format(message, args));

    protected HttpClient GetClient()
    {
        return new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{ctx.BackendPort}"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }
}

public class E2ECtx
{
    public ILogger<E2ETest> Logger { get; }
    public CancellationToken Token { get; }
    public ResiliencePipeline Pipeline { get; }
    public int BackendPort { get; }

    public E2ECtx(
        ILogger<E2ETest> logger,
        ResiliencePipeline pipeline,
        int backendPort,
        CancellationToken token = default
    )
    {
        Logger = logger;
        Pipeline = pipeline;
        Token = token;
        BackendPort = backendPort;
    }
}

public class TestSummary
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool Outcome { get; set; }
    public int Retries { get; set; }
    public TimeSpan Elapsed { get; set; }
    public string? ErrorMessage { get; set; }
    public bool AllowFail { get; set; }
}
