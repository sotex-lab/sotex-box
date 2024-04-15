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

        var deviceInBatchThreshold = uint.Parse(
            GetEnvironmentVariable("SCHEDULE_DEVICE_THRESHOLD")
        );

        var devices = Enumerable
            .Range(0, (int)deviceInBatchThreshold + 10)
            .Select(i => new Device { UtilityName = string.Format("test-device-{0}", i) })
            .ToList();
        foreach (var device in devices)
        {
            var maybeSavedDevice = await deviceRepo.Add(device, token);
            maybeSavedDevice.IsSuccessful.ShouldBeTrue();
        }

        var adRepo = GetRepository<Ad, Guid>();
        var ad = new Ad();
        var maybeSavedAd = await adRepo.Add(ad, token);
        maybeSavedAd.IsSuccessful.ShouldBeTrue();
        ad = maybeSavedAd.Value;

        var maxDelay = TimeSpan.Parse(GetEnvironmentVariable("SCHEDULE_MAX_DELAY")).Multiply(1.2);
        Info("Sleeping for {0} to wait for schedule", maxDelay);
        await Task.Delay(maxDelay, token);

        var client = GetClient();
        var shouldBeCorrectError = 0;
        var shouldNotBeCorrectError = 0;
        foreach (var device in devices.Take((int)deviceInBatchThreshold))
        {
            try
            {
                await ShouldHaveCorrectAds(device, client, token, ad);
            }
            catch (Exception)
            {
                shouldBeCorrectError++;
            }
        }

        foreach (
            var device in devices
                .Skip((int)deviceInBatchThreshold)
                .Take(devices.Count - (int)deviceInBatchThreshold)
        )
        {
            try
            {
                await ShouldNotHaveCorrectAds(device, client, token);
            }
            catch (Exception)
            {
                shouldNotBeCorrectError++;
            }
        }

        (shouldBeCorrectError, shouldNotBeCorrectError).ShouldBe((0, 0));
    }

    private async Task ShouldHaveCorrectAds(
        Device device,
        HttpClient client,
        CancellationToken token,
        Ad ad
    )
    {
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

    private async Task ShouldNotHaveCorrectAds(
        Device device,
        HttpClient client,
        CancellationToken token
    )
    {
        var scheduleResponse = await client.GetAsync($"/schedule/{device.Id}", token);
        scheduleResponse.IsSuccessStatusCode.ShouldBeTrue();

        var url = await scheduleResponse.Content.ReadAsStringAsync();
        var minioResponse = await client.GetAsync(url, token);
        minioResponse.IsSuccessStatusCode.ShouldBeFalse();
    }
}
