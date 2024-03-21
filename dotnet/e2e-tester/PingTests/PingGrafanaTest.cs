using Shouldly;

namespace e2e_tester.PingTests;

public class PingGrafanaTest : E2ETest
{
    public PingGrafanaTest(E2ECtx c)
        : base(c) { }

    protected override bool AllowFail() => true;

    protected override string Description() => "When the stack is up, Grafana should be reachable";

    protected override string Name() => "Ping Grafana";

    protected override async Task Run(CancellationToken token)
    {
        var client = GetClient();

        var response = await client.GetAsync("/grafana", token);

        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
