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
    private Mock<IEventCoordinator> coordinatorMock = new Mock<IEventCoordinator>();
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

    private IMapper mapper = new MapperConfiguration(cfg =>
        cfg.AddMaps(typeof(CoreMapper))
    ).CreateMapper();
    private Mock<IAdRepository> adRepoMock = new Mock<IAdRepository>();
    private Mock<IPreSignObjectService> preSignMock = new Mock<IPreSignObjectService>();
    private Mock<IGetOrCreateBucketService> getOrCreateMock = new Mock<IGetOrCreateBucketService>();
    private Mock<IPutObjectService> putObjectMock = new Mock<IPutObjectService>();
    private Mock<IGetObjectService> getObjectMock = new Mock<IGetObjectService>();
    private List<Ad> ads = new List<Ad>
    {
        new Ad { Id = Guid.Parse("B6A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("C6A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("D6A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("E6A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("F6A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("06A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("16A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("26A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("36A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("46A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("56A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("66A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("76A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("86A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
        new Ad { Id = Guid.Parse("96A94D36-DDEE-452D-BAEF-3CEF9F573D82") },
    };
    private string? writtenString;

    private void Setup(
        uint pageNumber,
        int threshold,
        string maxDelay,
        uint adPageSize,
        ScheduleContract? contract = null
    )
    {
        coordinatorMock = new Mock<IEventCoordinator>();
        configRepoMock = new Mock<IConfigurationRepository>();
        backroundSchedulerMock = new Mock<IBackgroundJobClientV2>();
        deviceRepoMock = new Mock<IDeviceRepository>();

        page = new Configuration { Id = pageId, Value = pageNumber.ToString() };

        Environment.SetEnvironmentVariable("SCHEDULE_DEVICE_THRESHOLD", threshold.ToString());
        Environment.SetEnvironmentVariable("SCHEDULE_MAX_DELAY", maxDelay);
        Environment.SetEnvironmentVariable("SCHEDULE_AD_THRESHOLD", adPageSize.ToString());
        Environment.SetEnvironmentVariable(
            "SCHEDULE_URL_EXPIRE",
            TimeSpan.FromMinutes(30).ToString()
        );

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

        coordinatorMock.Setup(x => x.GetConnectionIds()).Returns(devices);
        coordinatorMock
            .Setup(x => x.SendMessage(It.IsAny<Guid>(), It.IsAny<object>()))
            .Callback<Guid, object>(
                (key, _) =>
                {
                    called.Add(key);
                }
            )
            .ReturnsAsync(new Result<bool, EventCoordinatorError>(true));

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

        putObjectMock
            .Setup(x => x.Put(It.IsAny<S3Bucket>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .Callback<S3Bucket, string, Stream>(
                async (bucket, key, stream) =>
                {
                    writtenString = await new StreamReader(stream).ReadToEndAsync();
                }
            )
            .ReturnsAsync(new Result<string, PutObjectError>(string.Empty));

        getObjectMock
            .Setup(x => x.GetObjectByKey(It.IsAny<S3Bucket>(), It.IsAny<string>()))
            .ReturnsAsync(
                new Result<ScheduleContract, GetObjectError>(contract ?? new ScheduleContract())
            );

        adRepoMock
            .Setup(x => x.TakeFrom(It.IsAny<Guid>(), It.IsAny<uint>()))
            .Returns<Guid, uint>(
                (id, take) =>
                {
                    IQueryable<Ad> adsQuery = ads.AsQueryable();
                    if (id != Guid.Empty)
                    {
                        adsQuery = adsQuery.SkipWhile(x => x.Id != id).Skip(1);
                    }
                    return Task.FromResult(adsQuery.Take((int)take).AsEnumerable());
                }
            );

        preSignMock
            .Setup(x => x.Get(It.IsAny<S3Bucket>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Result<string, PreSignErr>("download-link"));

        job = new ScheduleJob(
            factory.CreateLogger<ScheduleJob>(),
            coordinatorMock.Object,
            configRepoMock.Object,
            backroundSchedulerMock.Object,
            deviceRepoMock.Object,
            mapper,
            preSignMock.Object,
            getOrCreateMock.Object,
            putObjectMock.Object,
            getObjectMock.Object,
            adRepoMock.Object
        );
    }

    private void GetOrCreateReturns(string bucket)
    {
        getOrCreateMock
            .Setup(x => x.GetProcessed())
            .ReturnsAsync(
                new Result<S3Bucket, GetOrCreateBucketError>(
                    new S3Bucket { BucketName = bucket, CreationDate = DateTime.Now }
                )
            );

        getOrCreateMock
            .Setup(x => x.GetSchedule())
            .ReturnsAsync(
                new Result<S3Bucket, GetOrCreateBucketError>(
                    new S3Bucket { BucketName = bucket, CreationDate = DateTime.Now }
                )
            );
    }

    private void GetOrCreateReturns(GetOrCreateBucketError error) =>
        getOrCreateMock
            .Setup(x => x.GetSchedule())
            .ReturnsAsync(new Result<S3Bucket, GetOrCreateBucketError>(error));

    [TestMethod]
    public async Task Should_CallAllDevices()
    {
        Setup(0, 100, "00:10:00", 10);
        await job.Run();

        called.Count.ShouldBe(devices.Count);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(10));
        page.Value.ShouldBe("1".ToString());
    }

    [TestMethod]
    public async Task Should_SplitBecauseMoreThanMaxBatch()
    {
        Setup(3, 2, "00:10:00", 10);
        await job.Run();

        called.Count.ShouldBe(2);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(1.25));
        page.Value.ShouldBe("4".ToString());
    }

    [TestMethod]
    public async Task Should_Rollover()
    {
        Setup((uint)(devices.Count / 2 + 1), 2, "00:10:00", 10);
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
        Setup(2, 100, "04:00:00", 10);

        await job.Run();

        called.Count.ShouldBe(100);
        calledTimespan.ShouldBeAround(TimeSpan.FromMinutes(20));
        page.Value.ShouldBe("3".ToString());
    }

    [TestMethod]
    public async Task Should_Not_ProceedFailedToGetBucket()
    {
        Setup(2, 100, "04:00:00", 10);
        GetOrCreateReturns(GetOrCreateBucketError.FailedToCreateBucket);

        await job.Run();

        called.Count.ShouldBe(0);
        getOrCreateMock.Verify(x => x.GetSchedule(), Times.Once());
        deviceRepoMock.Verify(x => x.GetSingle(It.IsAny<Guid>(), default), Times.Never());
    }

    private ScheduleContract Deserialize()
    {
        writtenString.ShouldNotBeNullOrEmpty();

        return JsonConvert.DeserializeObject<ScheduleContract>(writtenString)!;
    }

    [TestMethod]
    public async Task Should_CreateBucketAndWriteBatch()
    {
        GetOrCreateReturns("test");
        Setup(2, 100, "04:00:00", 10);
        await job.Run();

        getOrCreateMock.Verify(x => x.GetSchedule(), Times.Once());
        adRepoMock.Verify(
            x => x.TakeFrom(It.IsAny<Guid>(), It.IsAny<uint>()),
            Times.Exactly(called.Count)
        );

        var deseriliazed = Deserialize();
        deseriliazed.Schedule.ShouldNotBeEmpty();
        deseriliazed.Schedule.Count().ShouldBe(10);
        var adIds = ads.Select(x => x.Id).ToList();
        deseriliazed.Schedule.ForEach(x =>
        {
            x.DownloadLink.ShouldNotBeNullOrEmpty();
            adIds.ShouldContain(x.Ad!.Id);
        });
    }

    [TestMethod]
    public async Task Should_CreateScheduleIfNotStartingFromNothing()
    {
        Enumerable
            .Range(0, 1000)
            .Select(_ => new Ad { AdScope = AdScope.Global, Id = Guid.NewGuid(), })
            .ForEach(ads.Add);
        var randomAd = new Random().Next(0, ads.Count);

        var schedule = new ScheduleContract
        {
            CreatedAt = DateTime.Now,
            DeviceId = devices[0],
            Schedule =
            [
                new ScheduleItemContract
                {
                    Ad = new AdContract { Id = ads[randomAd].Id, Scope = ads[randomAd++].AdScope }
                }
            ]
        };

        GetOrCreateReturns("test");
        uint pageSize = 10;
        Setup(2, 100, "04:00:00", pageSize, schedule);

        await job.Run();

        putObjectMock.Verify(
            x => x.Put(It.IsAny<S3Bucket>(), It.IsAny<string>(), It.IsAny<Stream>()),
            Times.Exactly(called.Count)
        );
        var deserialized = Deserialize();
        deserialized.Schedule.Count().ShouldBe(10);
        var expectedAds = new List<Ad>();
        while (expectedAds.Count != pageSize)
        {
            if (randomAd == ads.Count)
            {
                randomAd = 0;
            }
            expectedAds.Add(ads[randomAd++]);
        }
        var outcome = true;
        for (int i = 0; i < expectedAds.Count; i++)
        {
            var expected = expectedAds[i].Id;
            var got = deserialized.Schedule.ElementAt(i).Ad!.Id;
            Console.WriteLine("Got {0}, Expected {1}", got, expected);
            outcome &= got == expected;
        }
        outcome.ShouldBeTrue();
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
