using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using model.Core;
using persistence;
using persistence.Repository.Base;
using Polly;
using Shouldly;

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
            var lenght = message.Length;
            var cap = 150;
            message = message.Substring(0, Math.Min(cap, lenght));
            if (lenght > cap)
            {
                message = string.Format("{0}...", message);
            }
            Error(e.Message);
            summary.ErrorMessage = message;
        }

        return summary;
    }

    public abstract string Name();
    protected abstract string Description();

    protected virtual bool AllowFail() => false;

    protected abstract Task Run(CancellationToken token);

    protected void Info(string message, params object[] args) =>
        ctx.Info("Test {0}: {1}", [Name(), string.Format(message, args)]);

    protected void Warn(string message, params object[] args) =>
        ctx.Warn("Test {0}: {1}", [Name(), string.Format(message, args)]);

    protected void Error(string message, params object[] args) =>
        ctx.Error("Test {0}: {1}", [Name(), string.Format(message, args)]);

    protected HttpClient GetClient(TimeSpan timeout = default)
    {
        return new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{ctx.BackendPort}"),
            Timeout = timeout == default ? TimeSpan.FromSeconds(15) : timeout
        };
    }

    protected string ResourcesDir() => ctx.ResourcesDir;

    protected TimeSpan DefaultJobInterval() => TimeSpan.FromSeconds(15);

    protected IRepository<TEntity, T> GetRepository<TEntity, T>()
        where TEntity : Entity<T>, new()
        where T : IComparable, IEquatable<T>
    {
        var repoType = typeof(ApplicationDbContext)
            .Assembly.GetTypes()
            .FirstOrDefault(x =>
                x.IsSubclassOf(typeof(Repository<TEntity, T, ApplicationDbContext>))
            );
        Info(
            "Requesting repo that has TEntity '{0}' and T '{1}'",
            typeof(TEntity).Name,
            typeof(T).Name
        );
        repoType.ShouldNotBeNull(
            string.Format(
                "Repo type not found with TEntity '{0}' and T '{1}'",
                typeof(TEntity).Name,
                typeof(T).Name
            )
        );

        return (IRepository<TEntity, T>)
            Activator.CreateInstance(repoType, ctx.ApplicationDbContext)!;
    }
}

public class E2ECtx
{
    public CancellationToken Token { get; }
    public ResiliencePipeline Pipeline { get; }
    public int BackendPort { get; }
    public string ResourcesDir { get; }
    public ApplicationDbContext ApplicationDbContext { get; }
    public Action<string, object[]> Info { get; }
    public Action<string, object[]> Warn { get; }
    public Action<string, object[]> Error { get; }

    public E2ECtx(
        ResiliencePipeline pipeline,
        int backendPort,
        string resourcesDir,
        ApplicationDbContext applicationDbContext,
        Action<string, object[]> info,
        Action<string, object[]> warn,
        Action<string, object[]> error,
        CancellationToken token = default
    )
    {
        Info = info;
        Warn = warn;
        Error = error;
        ResourcesDir = resourcesDir;
        Pipeline = pipeline;
        Token = token;
        BackendPort = backendPort;
        ApplicationDbContext = applicationDbContext;
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
