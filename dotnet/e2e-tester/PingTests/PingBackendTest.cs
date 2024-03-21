using Shouldly;

namespace e2e_tester.PingTests;

public class PingBackendTest : E2ETest
{
    public PingBackendTest(E2ECtx c)
        : base(c) { }

    protected override string Description() => "When the stack is up, backend should be available";

    protected override string Name() => "Ping backend";

    protected override async Task Run(CancellationToken token)
    {
        var client = GetClient();

        var response = await client.GetAsync("/swagger/index.html");

        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
