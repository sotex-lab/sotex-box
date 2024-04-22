using System.Net;
using System.Text;
using model.Core;
using persistence.Repository;
using Shouldly;
using SseHandler;

namespace IntegrationTests.ControllerTests;

[Collection(ConfigurableBackendFactory.IntegrationCollection)]
public class EventControllerTests
{
    private readonly ConfigurableBackendFactory _factory;
    private readonly IDeviceRepository _deviceRepository;

    public EventControllerTests(ConfigurableBackendFactory factory)
    {
        _factory = factory;
        _factory.Connections.Clear();
        // Needed to reset if in any test it is changed
        Environment.SetEnvironmentVariable("REQUIRE_KNOWN_DEVICES", "false");
        var serviceProvider = _factory.Services.CreateScope().ServiceProvider;
        _deviceRepository = serviceProvider.GetRequiredService<IDeviceRepository>();
    }

    [Fact]
    public async Task Should_Connect()
    {
        var client = _factory.CreateClient();
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var id = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/event/connect?id={id}",
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
        var connection = new Connection(Guid.NewGuid(), new MemoryStream());
        _factory.Connections[connection.Id] = connection;

        var client = _factory.CreateClient();
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var response = await client.GetAsync(
            $"/event/connect?id={connection.Id}",
            tokenSource.Token
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain(EventCoordinatorError.DuplicateKey.Stringify());
    }

    [Fact]
    public async Task Should_Remove()
    {
        var id = Guid.NewGuid();

        var client = _factory.CreateClient();
        var task = Task.Run(async () => await client.GetAsync($"/event/connect?id={id}"));
        await Task.Delay(100);
        var removeResponse = await client.DeleteAsync($"/event/ForceDisconnect?id={id}");

        await Task.Delay(500);

        removeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        task.IsCompleted.ShouldBeTrue();

        var response = await task;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_WriteData()
    {
        var stream = new MemoryStream();
        var connection = new Connection(Guid.NewGuid(), stream);
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

    [Fact]
    public async Task Should_NotAllowToConnect()
    {
        var client = _factory.CreateClient();
        Environment.SetEnvironmentVariable("REQUIRE_KNOWN_DEVICES", "true");

        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var id = Guid.NewGuid();

        var response = await client.GetAsync(
            $"/event/connect?id={id}",
            HttpCompletionOption.ResponseHeadersRead,
            tokenSource.Token
        );

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_AllowToConnect()
    {
        var device = new Device() { UtilityName = "test" };
        var maybeDevice = await _deviceRepository.Add(device);
        maybeDevice.IsSuccessful.ShouldBeTrue();
        var client = _factory.CreateClient();
        Environment.SetEnvironmentVariable("REQUIRE_KNOWN_DEVICES", "true");
        var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        device = maybeDevice.Value;
        var response = await client.GetAsync(
            $"/event/connect?id={device.Id}",
            HttpCompletionOption.ResponseHeadersRead,
            tokenSource.Token
        );

        response.IsSuccessStatusCode.ShouldBeTrue();
        maybeDevice = await _deviceRepository.GetSingle(device.Id);
        maybeDevice.IsSuccessful.ShouldBeTrue();
    }
}
