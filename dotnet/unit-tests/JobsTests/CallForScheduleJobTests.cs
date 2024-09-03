using Amazon.S3.Model;
using AutoMapper;
using backend.Hangfire;
using backend.Services.Aws;
using DotNext;
using DotNext.Collections.Generic;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using model.Contracts;
using model.Core;
using model.Mappers;
using Moq;
using Newtonsoft.Json;
using persistence.Repository;
using Shouldly;
using SseHandler;

namespace unit_tests;

[TestClass]
public class CallForScheduleJobTests
{
    private ScheduleJob job;
    private Mock<IConfigurationRepository> configRepoMock;
    private Mock<IDeviceRepository> deviceRepoMock;
    private Mock<IBackgroundJobClientV2> backroundSchedulerMock =
        new Mock<IBackgroundJobClientV2>();
    private Configuration page;
    private const string pageId = "CALL_FOR_SCHEDULE_PAGE";
    private List<Guid> devices = new List<Guid>
    {
        Guid.Parse("A6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("B6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("C6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("D6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("E6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("F6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("06A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("16A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("26A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("36A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("46A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("56A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("66A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("76A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("86A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
        Guid.Parse("96A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
    };
    private TimeSpan calledTimespan = new TimeSpan();
    private readonly List<Guid> called = new List<Guid>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CallForScheduleJobTests()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    { }

    private void Setup(uint pageNumber, int threshold, string maxDelay)
    {
        configRepoMock = new Mock<IConfigurationRepository>();
        backroundSchedulerMock = new Mock<IBackgroundJobClientV2>();
        deviceRepoMock = new Mock<IDeviceRepository>();

        page = new Configuration { Id = pageId, Value = pageNumber.ToString() };

        Environment.SetEnvironmentVariable("SCHEDULE_DEVICE_THRESHOLD", threshold.ToString());
        Environment.SetEnvironmentVariable("SCHEDULE_MAX_DELAY", maxDelay);

        configRepoMock.Setup(x => x.GetSingle(page.Id, default)).ReturnsAsync(page);
        configRepoMock
            .Setup(x => x.Update(It.IsAny<Configuration>(), default))
            .Callback<Configuration, CancellationToken>(
                (config, token) =>
                {
                    switch (config.Id)
                    {
                        case pageId:
                            page = config;
                            break;
                        default:
                            throw new Exception("Unexpected config key: " + config.Id);
                    }
                }
            );

        deviceRepoMock
            .Setup(x => x.GetPage(It.IsAny<uint>(), It.IsAny<uint>()))
            .Returns<uint, uint>(
                (page, pageSize) =>
                    devices
                        .Skip((int)(page * pageSize))
                        .Take((int)pageSize)
                        .Select(x => new Device { Id = x })
            );
        deviceRepoMock.Setup(x => x.Count()).ReturnsAsync(() => devices.Count);

        var factory = new ServiceCollection()
            .AddLogging(opts => opts.AddConsole())
            .BuildServiceProvider()
            .GetService<ILoggerFactory>()!;

        backroundSchedulerMock
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>(
                (job, state) =>
                {
                    if (state is ScheduledState scheduledState)
                    {
                        calledTimespan = scheduledState.EnqueueAt.Subtract(DateTime.UtcNow);
                        Console.WriteLine("Setting callback: " + calledTimespan);
                    }
                    else if (state is EnqueuedState enqueuedState)
                    {
                        called.Add((Guid)job.Args[0]!);
                    }
                }
            );
        backroundSchedulerMock
            .Setup(x =>
                x.Create(
                    It.IsAny<Job>(),
                    It.IsAny<IState>(),
                    It.IsAny<IDictionary<string, object>>()
                )
            )
            .Callback<Job, IState, IDictionary<string, object>>(
                (job, state, dict) =>
                {
                    var scheduledState = (ScheduledState)state;
                    calledTimespan = scheduledState.EnqueueAt.Subtract(DateTime.UtcNow);
                    Console.WriteLine("Setting callback: " + calledTimespan);
                }
            );

        job = new ScheduleJob(
            factory.CreateLogger<ScheduleJob>(),
            configRepoMock.Object,
            backroundSchedulerMock.Object,
            deviceRepoMock.Object
        );
    }

    [TestMethod]
    public async Task Should_CallAllDevices()
    {
        Setup(0, 100, "00:10:00");
        await job.Run();

        called.Count.ShouldBe(devices.Count);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(10));
        page.Value.ShouldBe("1".ToString());
    }

    [TestMethod]
    public async Task Should_SplitBecauseMoreThanMaxBatch()
    {
        Setup(3, 2, "00:10:00");
        await job.Run();

        called.Count.ShouldBe(2);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(1.25));
        page.Value.ShouldBe("4".ToString());
    }

    [TestMethod]
    public async Task Should_Rollover()
    {
        Setup((uint)(devices.Count / 2 + 1), 2, "00:10:00");
        await job.Run();

        called.Count.ShouldBe(2);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(1.25));
        page.Value.ShouldBe("1".ToString());
    }

    [TestMethod]
    public async Task Should_WorkInProd()
    {
        var prodDevices = new List<Guid>();
        foreach (var device in devices)
        {
            for (int i = 0; i < 1200 / devices.Count; i++)
            {
                prodDevices.Add(device);
            }
        }
        devices = prodDevices;
        Setup(2, 100, "04:00:00");

        await job.Run();

        called.Count.ShouldBe(100);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(20));
        page.Value.ShouldBe("3".ToString());
    }
}

internal static class TimespanShouldly
{
    public static void ShouldBeAround(this TimeSpan timeSpan, TimeSpan other)
    {
        timeSpan.ShouldBeGreaterThanOrEqualTo(other.Subtract(TimeSpan.FromSeconds(2)));
        timeSpan.ShouldBeLessThanOrEqualTo(other.Add(TimeSpan.FromSeconds(2)));
    }
}
