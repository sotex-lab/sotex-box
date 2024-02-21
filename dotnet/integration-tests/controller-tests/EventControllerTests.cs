using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using SseHandler;

namespace IntegrationTests.ControllerTests;

public class EventControllerTests : IClassFixture<ConfigurableBackendFactory<Program>>
{
    private readonly ConfigurableBackendFactory<Program> _factory;

    public EventControllerTests(ConfigurableBackendFactory<Program> factory)
    {
        _factory = factory;
        _factory.Connections.Clear();
    }

    [Fact]
    public async Task Should_Connect()
    {
        var client = _factory.CreateClient();
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var response = await client.GetAsync(
            "/event/connect?id=1",
            HttpCompletionOption.ResponseHeadersRead,
            tokenSource.Token
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contentType = "Content-Type";
        response.Content.Headers.Contains(contentType).ShouldBeTrue();

        var values = response.Content.Headers.GetValues(contentType);
        values.Count().ShouldBeGreaterThanOrEqualTo(1);
        values.ShouldContain("text/event-stream");
    }

    [Fact]
    public async Task Should_NotConnect()
    {
        var connection = new Connection(1.ToString(), new MemoryStream());
        _factory.Connections[connection.Id] = connection;

        var client = _factory.CreateClient();
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var response = await client.GetAsync(
            $"/event/connect?id={connection.Id}",
            tokenSource.Token
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_Remove()
    {
        var id = 1.ToString();

        var client = _factory.CreateClient();
        var task = Task.Run(async () => await client.GetAsync($"/event/connect?id={id}"));
        var removeResponse = await client.DeleteAsync($"/event/ForceDisconnect?id={id}");

        removeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        task.IsCompleted.ShouldBeTrue();

        var response = await task;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_WriteData()
    {
        var stream = new MemoryStream();
        var connection = new Connection(1.ToString(), stream);
        var testMessage = "test";
        _factory.Connections[connection.Id] = connection;

        var client = _factory.CreateClient();
        var sendResponse = await client.GetAsync(
            $"/event/writedata?id={connection.Id}&message={testMessage}"
        );

        sendResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var content = Encoding.UTF8.GetString(stream.ToArray());
        content.ShouldContain("data:");
        content.ShouldContain(testMessage);
        content.ShouldEndWith("\n\n");
    }
}
