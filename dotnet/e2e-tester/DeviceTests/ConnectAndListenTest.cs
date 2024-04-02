namespace e2e_tester.DeviceTests;

public class ConnectAndListeTest : E2ETest
{
    public ConnectAndListeTest(E2ECtx c)
        : base(c) { }

    public override string Name() => "Connect and listen";

    protected override string Description() =>
        "A device, when connected, should be able to receive correct messages";

    protected override Task Run(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
