using Shouldly;

namespace e2e_tester.PingTests;

public class PingPrometheusTest : E2ETest
{
    public PingPrometheusTest(E2ECtx c)
        : base(c) { }

    protected override string Description() =>
        "When the stack is up, Prometheus should be pingable";

    protected override string Name() => "Ping Prometheus";

    protected override async Task Run(CancellationToken token)
    {
        var client = GetClient();

        var response = await client.GetAsync("/prometheus");

        response.IsSuccessStatusCode.ShouldBe(true);
    }
}
