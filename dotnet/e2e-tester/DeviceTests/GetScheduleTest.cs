using System.Text.Json.Nodes;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using Shouldly;

namespace e2e_tester.DeviceTests;

public class GetScheduleTest : E2ETest
{
    public GetScheduleTest(E2ECtx c)
        : base(c) { }

    public override string Name() => "Get schedule test";

    protected override string Description() =>
        "If a device exists after a certain period it should be called for schedule and should have a schedule";

    protected override async Task Run(CancellationToken token)
    {
        var deviceRepo = GetRepository<Device, Guid>();
        var device = new Device { UtilityName = "test" };
        var maybeSavedDevice = await deviceRepo.Add(device, token);
        maybeSavedDevice.IsSuccessful.ShouldBeTrue();
        device = maybeSavedDevice.Value;

        var adRepo = GetRepository<Ad, Guid>();
        var ad = new Ad();
        var maybeSavedAd = await adRepo.Add(ad, token);
        maybeSavedAd.IsSuccessful.ShouldBeTrue();
        ad = maybeSavedAd.Value;

        var maxDelay = TimeSpan
            .Parse(GetEnvironmentVariable("CALLFORSCHEDULE_MAX_DELAY"))
            .Multiply(2);
        Info("Sleeping for {0} to wait for schedule for device {1}", maxDelay, device.Id);
        await Task.Delay(maxDelay, token);

        var client = GetClient();
        var scheduleResponse = await client.GetAsync($"/schedule/{device.Id}", token);
        scheduleResponse.IsSuccessStatusCode.ShouldBeTrue();

        var url = await scheduleResponse.Content.ReadAsStringAsync();
        url.ShouldNotBeNullOrEmpty();
        var scheduleInBucketResponse = await client.GetAsync(url, token);
        scheduleInBucketResponse.IsSuccessStatusCode.ShouldBeTrue();

        var schedule = JsonConvert.DeserializeObject<ScheduleContract>(
            await scheduleInBucketResponse.Content.ReadAsStringAsync(token)
        );
        schedule.ShouldNotBeNull();
        schedule.Schedule.ShouldNotBeEmpty();
        var onlyItem = schedule.Schedule.Single();
        onlyItem.Ad.ShouldNotBeNull();
        onlyItem.Ad.Id.ShouldBe(ad.Id);
        onlyItem.DownloadLink.ShouldNotBeNullOrEmpty();
    }
}
