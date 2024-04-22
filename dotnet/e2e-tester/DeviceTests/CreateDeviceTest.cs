using System.Text;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using Shouldly;

namespace e2e_tester.DeviceTests;

public class CreateDeviceTest : E2ETest
{
    public CreateDeviceTest(E2ECtx c)
        : base(c) { }

    protected override string Description() =>
        "Test tries to create a device via an API and connects as that device and connects as that device immediately";

    public override string Name() => "Create device and connect";

    protected override async Task Run(CancellationToken token)
    {
        var client = GetClient(TimeSpan.FromSeconds(60));
        var contract = new DeviceContract { UtilityName = "e2e-test" };
        var content = new StringContent(
            JsonConvert.SerializeObject(contract),
            Encoding.UTF8,
            "application/json"
        );
        var response = await client.PostAsync("/devices", content, token);

        response.IsSuccessStatusCode.ShouldBeTrue();
        contract = JsonConvert.DeserializeObject<DeviceContract>(
            await response.Content.ReadAsStringAsync(token)
        );

        contract.ShouldNotBeNull();
        contract.Id.ShouldNotBe(Guid.Empty);

        var deviceRepo = GetRepository<Device, Guid>();
        var maybeDevice = await deviceRepo.GetSingle(contract.Id, token);

        maybeDevice.IsSuccessful.ShouldBeTrue();
        var device = maybeDevice.Value;
        device.UtilityName.ShouldBe("e2e-test");

        _ = Task.Run(
            async () =>
            {
                Info("Setting callback to disconnect the device {0}", contract.Id);
                await Task.Delay(DefaultJobInterval() * 2);
                var disconnectResponse = await GetClient()
                    .DeleteAsync($"/event/forcedisconnect?id={contract.Id}");
                disconnectResponse.IsSuccessStatusCode.ShouldBeTrue();
                Info("Disconnected device {0}", contract.Id);
            },
            token
        );

        var eventConnection = await client.GetAsync($"/event/connect?id={contract.Id}", token);
        var data = await eventConnection.Content.ReadAsStringAsync();

        eventConnection.IsSuccessStatusCode.ShouldBeTrue();
        foreach (var line in data.Split("\n\n"))
        {
            if (string.IsNullOrEmpty(line))
                continue;
            line.ShouldContain("data");
        }
    }
}
