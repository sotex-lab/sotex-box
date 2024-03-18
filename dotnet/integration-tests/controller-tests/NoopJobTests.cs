using System.Text;
using DotNext.Collections.Generic;
using Shouldly;
using SseHandler;

namespace IntegrationTests.ControllerTests;

[Collection(ConfigurableBackendFactory.IntegrationCollection)]
public class NoopJobTests
{
    private readonly ConfigurableBackendFactory _factory;

    public NoopJobTests(ConfigurableBackendFactory factory)
    {
        _factory = factory;
        _factory.Connections.Clear();
    }

    [Fact]
    public async Task Should_SendNoop()
    {
        var stream = new MemoryStream();
        var connection = new Connection(1.ToString(), stream);
        _factory.Connections[connection.Id] = connection;

        await Task.Delay(
                TimeSpan.FromSeconds(ConfigurableBackendFactory.IntegrationCronIntervalSeconds + 1)
            )
            .ConfigureAwait(false);

        while (true)
        {
            try
            {
                _factory.Connections.Remove(connection.Id);
                break;
            }
            catch (Exception) { }
        }

        var contents = Encoding.UTF8.GetString(stream.ToArray());
        contents.ShouldContain("data: \"noop\"\n\n");
    }
}
