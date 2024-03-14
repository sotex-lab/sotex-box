using System.Net;
using Shouldly;

namespace IntegrationTests.ControllerTests;

[Collection(ConfigurableBackendFactory.IntegrationCollection)]
public class MetricsTests
{
    private readonly ConfigurableBackendFactory _factory;

    public MetricsTests(ConfigurableBackendFactory factory)
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
