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

    private IEnumerable<Type> repositories;

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
        repositories = typeof(ApplicationDbContext)
            .Assembly.GetTypes()
            .Where(x => x.GenericTypeArguments.Contains(typeof(ApplicationDbContext)));
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

    protected string ResourcesDir() => ctx.ResourcesDir;

    protected TimeSpan DefaultJobInterval() => TimeSpan.FromSeconds(15);

    protected IRepository<TEntity, T> GetRepository<TEntity, T>()
        where TEntity : Entity<T>, new()
        where T : IComparable, IEquatable<T>
    {
        var repoType = repositories
            .Where(x =>
                x.GenericTypeArguments.Contains(typeof(TEntity))
                && x.GenericTypeArguments.Contains(typeof(T))
            )
            .FirstOrDefault();
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
    public ILogger<E2ETest> Logger { get; }
    public CancellationToken Token { get; }
    public ResiliencePipeline Pipeline { get; }
    public int BackendPort { get; }
    public string ResourcesDir { get; }
    public ApplicationDbContext ApplicationDbContext { get; }

    public E2ECtx(
        ILogger<E2ETest> logger,
        ResiliencePipeline pipeline,
        int backendPort,
        string resourcesDir,
        ApplicationDbContext applicationDbContext,
        CancellationToken token = default
    )
    {
        ResourcesDir = resourcesDir;
        Logger = logger;
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
