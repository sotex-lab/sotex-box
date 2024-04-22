using model.Core;
using Shouldly;
using SseHandler.Commands;

namespace e2e_tester.DeviceTests;

public class ConnectAndListeTest : E2ETest
{
    public ConnectAndListeTest(E2ECtx c)
        : base(c) { }

    public override string Name() => "Connect and listen";

    protected override string Description() =>
        "A device, when connected, should be able to receive correct messages";

    protected override async Task Run(CancellationToken token)
    {
        var device = new Device { UtilityName = "test" };
        var deviceRepo = GetRepository<Device, Guid>();
        var addResult = await deviceRepo.Add(device, token);
        addResult.IsSuccessful.ShouldBeTrue();
        device = addResult.Value;
        var maxDely = GetEnvironmentVariable("SCHEDULE_MAX_DELAY");
        var parsed = TimeSpan.Parse(maxDely);
        var delay = parsed.Multiply(2);

        _ = Task.Run(async () =>
        {
            Info("Setting callback to disconnect in {0} for device id {1}", delay, device.Id);
            await Task.Delay(delay);

            var diconnectResponse = await GetClient()
                .DeleteAsync($"/event/forcedisconnect?id={device.Id}");
            diconnectResponse.IsSuccessStatusCode.ShouldBeTrue();
            Info("Disconnected device {0}", device.Id);
        });

        var client = GetClient(delay.Add(TimeSpan.FromSeconds(15)));
        var eventConnection = await client.GetAsync($"/event/connect?id={device.Id}", token);
        var singleLine = await eventConnection.Content.ReadAsStringAsync();
        var data = singleLine.Split("\n\n");

        eventConnection.IsSuccessStatusCode.ShouldBeTrue();
        var wantMap = new Dictionary<Command, int>
        {
            [Command.Noop] = (int)(delay.TotalSeconds / DefaultJobInterval().TotalSeconds),
            [Command.CallForSchedule] = (int)(delay.TotalSeconds / parsed.TotalSeconds)
        };

        foreach (var kvp in wantMap)
        {
            var count = data.Count(x => x.Contains(kvp.Key.AsString()));
            count.ShouldBeGreaterThanOrEqualTo(kvp.Value - 1);
            count.ShouldBeLessThanOrEqualTo(kvp.Value + 1);
        }
    }
}
