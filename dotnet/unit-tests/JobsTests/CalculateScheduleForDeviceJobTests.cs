using Amazon.Runtime.Internal.Util;
using AutoMapper;
using backend.Hangfire;
using backend.Services.Aws;
using backend.Services.Files;
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

namespace unit_tests;

[TestClass]
public class CalculateScheduleForDeviceJobTests
{
    private CalculateScheduleForDeviceJob job;
    private Mock<IAdRepository> adRepoMock = new Mock<IAdRepository>();
    private Mock<IDeviceRepository> deviceRepoMock = new Mock<IDeviceRepository>();
    private IMapper mapper = new MapperConfiguration(cfg =>
        cfg.AddMaps(typeof(CoreMapper))
    ).CreateMapper();
    private Mock<IPreSignObjectService> preSignMock = new Mock<IPreSignObjectService>();
    private Mock<IGetOrCreateBucketService> getOrCreateMock = new Mock<IGetOrCreateBucketService>();
    private Mock<IFileUtil> fileUtilMock = new Mock<IFileUtil>();
    private List<Guid> devices = new List<Guid>
    {
        Guid.Parse("A6A94D36-DDEE-452D-BAEF-3CEF9F573D82"),
    };

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

    public CalculateScheduleForDeviceJobTests()
    {
        var factory = new ServiceCollection()
            .AddLogging(opts => opts.AddConsole())
            .BuildServiceProvider()
            .GetService<ILoggerFactory>()!;
        job = new CalculateScheduleForDeviceJob(
            factory.CreateLogger<CalculateScheduleForDeviceJob>(),
            adRepoMock.Object,
            deviceRepoMock.Object,
            mapper,
            preSignMock.Object,
            getOrCreateMock.Object,
            fileUtilMock.Object
        );
    }

    private void Setup(uint pageSize, string localStoragePath, ScheduleContract? contract = null)
    {
        Environment.SetEnvironmentVariable(
            "CALCULATESCHEDULEFOR_DEVICE_THRESHOLD",
            pageSize.ToString()
        );
        Environment.SetEnvironmentVariable(
            "CALCULATESCHEDULEFOR_DEVICE_LOCALPATH",
            localStoragePath
        );
        Environment.SetEnvironmentVariable(
            "CALCULATESCHEDULEFOR_URL_EXPIRE",
            TimeSpan.FromMinutes(30).ToString()
        );

        deviceRepoMock
            .Setup(x => x.GetSingle(It.IsAny<Guid>(), default))
            .Returns<Guid, CancellationToken>(
                (id, token) =>
                {
                    if (devices.Contains(id))
                    {
                        return Task.FromResult(
                            new Result<Device, persistence.Repository.Base.RepositoryError>(
                                new Device { Id = id }
                            )
                        );
                    }
                    return Task.FromResult(
                        new Result<Device, persistence.Repository.Base.RepositoryError>(
                            persistence.Repository.Base.RepositoryError.NotFound
                        )
                    );
                }
            );

        fileUtilMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        fileUtilMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        fileUtilMock
            .Setup(x => x.ReadAllTextAsync(It.IsAny<string>()))
            .ReturnsAsync(JsonConvert.SerializeObject(contract ?? new ScheduleContract()));
        fileUtilMock
            .Setup(x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string>(
                (path, text) =>
                {
                    writtenString = text;
                }
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
            .Setup(x =>
                x.Get(
                    It.IsAny<Amazon.S3.Model.S3Bucket>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>()
                )
            )
            .ReturnsAsync(new Result<string, PreSignErr>("download-link"));
    }

    private void GetOrCreateReturns(string bucket) =>
        getOrCreateMock
            .Setup(x => x.GetProcessed())
            .ReturnsAsync(
                new Result<Amazon.S3.Model.S3Bucket, GetOrCreateBucketError>(
                    new Amazon.S3.Model.S3Bucket
                    {
                        BucketName = bucket,
                        CreationDate = DateTime.Now
                    }
                )
            );

    private void GetOrCreateReturns(GetOrCreateBucketError error) =>
        getOrCreateMock
            .Setup(x => x.GetProcessed())
            .ReturnsAsync(new Result<Amazon.S3.Model.S3Bucket, GetOrCreateBucketError>(error));

    [TestMethod]
    public async Task Should_Not_ProceedInvalidDeviceId()
    {
        await job.Calculate("random");

        getOrCreateMock.Verify(x => x.GetProcessed(), Times.Never());
    }

    [TestMethod]
    public async Task Should_Not_ProceedFailedToGetBucket()
    {
        GetOrCreateReturns(GetOrCreateBucketError.FailedToCreateBucket);

        await job.Calculate(Guid.NewGuid().ToString());

        getOrCreateMock.Verify(x => x.GetProcessed(), Times.Once());
        deviceRepoMock.Verify(x => x.GetSingle(It.IsAny<Guid>(), default), Times.Never());
    }

    [TestMethod]
    public async Task Should_Not_ProceedDeviceNotFound()
    {
        GetOrCreateReturns("test");
        Setup(10, "/");

        await job.Calculate(Guid.NewGuid().ToString());

        getOrCreateMock.Verify(x => x.GetProcessed(), Times.Once());
        deviceRepoMock.Verify(x => x.GetSingle(It.IsAny<Guid>(), default), Times.Once());
        fileUtilMock.Verify(x => x.DirectoryExists(It.IsAny<string>()), Times.Never());
    }

    private ScheduleContract Deserialize()
    {
        writtenString.ShouldNotBeNullOrEmpty();

        return JsonConvert.DeserializeObject<ScheduleContract>(writtenString)!;
    }

    [TestMethod]
    public async Task Should_CreateDirectoryAndWriteBatch()
    {
        GetOrCreateReturns("test");
        Setup(10, "/");

        await job.Calculate(devices[0].ToString());

        fileUtilMock.Verify(x => x.ReadAllTextAsync(It.IsAny<string>()), Times.Once());
        adRepoMock.Verify(x => x.TakeFrom(It.IsAny<Guid>(), It.IsAny<uint>()), Times.Once());

        var deseriliazed = Deserialize();
        deseriliazed.DeviceId.ShouldBe(devices[0]);
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
        Setup(pageSize, "/", schedule);

        await job.Calculate(devices[0].ToString());

        fileUtilMock.Verify(
            x => x.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once()
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
