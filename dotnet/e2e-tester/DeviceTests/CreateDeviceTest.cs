using System.Text;
using Newtonsoft.Json;
using Shouldly;

namespace e2e_tester.DeviceTests;

public class CreateDeviceTest : E2ETest
{
    public CreateDeviceTest(E2ECtx c)
        : base(c) { }

    protected override string Description() =>
        "Test tries to create a device via an API and connects as that device and connects as that device immediately";

    protected override string Name() => "Create device and connect";

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

        var customToken = new CancellationTokenSource(DefaultJobInterval() * 2);
        token.Register(customToken.Cancel);

        try
        {
            response = await client.GetAsync($"/event/connect?id={contract.Id}", customToken.Token);
        }
        catch (OperationCanceledException) { }

        response.IsSuccessStatusCode.ShouldBeTrue();
        var body = await response.Content.ReadAsStringAsync();
        foreach (var line in body.Split("\n\n"))
        {
            line.ShouldBe("data: \"noop\"");
        }
    }
}
