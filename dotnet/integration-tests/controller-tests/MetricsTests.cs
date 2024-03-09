using System.Net;
using Shouldly;

[Collection("Integration tests")]
public class MetricsTests : IClassFixture<ConfigurableBackendFactory<Program>>
{
    private readonly ConfigurableBackendFactory<Program> _factory;

    public MetricsTests(ConfigurableBackendFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Should_GetMetrics()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
