using Amazon.S3.Model;
using AutoMapper;
using backend.Hangfire;
using backend.Services.Aws;
using DotNext;
using DotNext.Collections.Generic;
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
public class CalculateJobTests
{
    private IMapper mapper = new MapperConfiguration(cfg =>
        cfg.AddMaps(typeof(CoreMapper))
    ).CreateMapper();
    private Mock<IAdRepository> adRepoMock = new Mock<IAdRepository>();
    private Mock<IPreSignObjectService> preSignMock = new Mock<IPreSignObjectService>();
    private Mock<IGetOrCreateBucketService> getOrCreateMock = new Mock<IGetOrCreateBucketService>();
    private Mock<IPutObjectService> putObjectMock = new Mock<IPutObjectService>();
    private Mock<IGetObjectService> getObjectMock = new Mock<IGetObjectService>();
    private Mock<IEventCoordinator> coordinatorMock = new Mock<IEventCoordinator>();
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
    private string? writtenString;
    private List<Guid> called = new List<Guid>();

    private void Setup(string maxDelay, uint adPageSize, ScheduleContract? contract = null)
    {
        Environment.SetEnvironmentVariable("CALCULATE_AD_THRESHOLD", adPageSize.ToString());
        Environment.SetEnvironmentVariable(
            "CALCULATE_URL_EXPIRE",
            TimeSpan.FromMinutes(30).ToString()
        );

        coordinatorMock = new Mock<IEventCoordinator>();
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

        var factory = new ServiceCollection()
            .AddLogging(opts => opts.AddConsole())
            .BuildServiceProvider()
            .GetService<ILoggerFactory>()!;

        preSignMock
            .Setup(x => x.Get(It.IsAny<S3Bucket>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new Result<string, PreSignErr>("download-link"));

        getOrCreateMock
            .Setup(x => x.GetSchedule())
            .ReturnsAsync(new Result<S3Bucket, GetOrCreateBucketError>(new S3Bucket()));

        job = new CalculateJob(
            adRepoMock.Object,
            getObjectMock.Object,
            putObjectMock.Object,
            mapper,
            factory.CreateLogger<CalculateJob>(),
            preSignMock.Object,
            getOrCreateMock.Object,
            coordinatorMock.Object
        );
    }

    private CalculateJob? job;

    private ScheduleContract Deserialize()
    {
        writtenString.ShouldNotBeNullOrEmpty();

        return JsonConvert.DeserializeObject<ScheduleContract>(writtenString)!;
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

        uint pageSize = 10;
        Setup("04:00:00", pageSize, schedule);

        await job!.Calculate(devices[0]);

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
