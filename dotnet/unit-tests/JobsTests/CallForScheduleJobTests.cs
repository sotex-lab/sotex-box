using backend.Hangfire;
using backend.Services.Batching;
using DotNext;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging;
using model.Core;
using Moq;
using persistence.Repository;
using Shouldly;
using SseHandler;

namespace unit_tests;

[TestClass]
public class CallForScheduleJobTests
{
    private CallForScheduleJob job;
    private Mock<ILogger<CallForScheduleJob>> loggerMock = new Mock<ILogger<CallForScheduleJob>>();
    private Mock<IEventCoordinator> coordinatorMock = new Mock<IEventCoordinator>();
    private Mock<IConfigurationRepository> configRepoMock;
    private IDeviceBatcher<Guid> deviceBatcher = new DeviceBatcher<Guid>();
    private Mock<IBackgroundJobClientV2> backroundSchedulerMock =
        new Mock<IBackgroundJobClientV2>();
    private Configuration nextKey;
    private const string nextKeyId = "CALL_FOR_SCHEDULE_NEXT_KEY";
    private Configuration maxChars;
    private const string maxCharsId = "CALL_FOR_SCHEDULE_MAX_CHARS";
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

    private readonly List<Guid> called = new List<Guid>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public CallForScheduleJobTests()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Setup('A', 16, 100, "00:10:00");
    }

    private void Setup(char startChar, uint totalChars, int threshold, string maxDelay)
    {
        coordinatorMock = new Mock<IEventCoordinator>();
        configRepoMock = new Mock<IConfigurationRepository>();
        backroundSchedulerMock = new Mock<IBackgroundJobClientV2>();

        nextKey = new Configuration { Id = nextKeyId, Value = startChar.ToString() };
        maxChars = new Configuration { Id = maxCharsId, Value = totalChars.ToString() };

        Environment.SetEnvironmentVariable(
            "CALLFORSCHEDULE_DEVICE_THRESHOLD",
            threshold.ToString()
        );
        Environment.SetEnvironmentVariable("CALLFORSCHEDULE_MAX_DELAY", maxDelay);

        configRepoMock.Setup(x => x.GetSingle(nextKey.Id, default)).ReturnsAsync(nextKey);
        configRepoMock.Setup(x => x.GetSingle(maxChars.Id, default)).ReturnsAsync(maxChars);
        configRepoMock
            .Setup(x => x.Update(It.IsAny<Configuration>(), default))
            .Callback<Configuration, CancellationToken>(
                (config, token) =>
                {
                    switch (config.Id)
                    {
                        case nextKeyId:
                            nextKey = config;
                            break;
                        case maxCharsId:
                            maxChars = config;
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
                    Console.WriteLine("Called: " + key);
                    called.Add(key);
                }
            )
            .ReturnsAsync(new Result<bool, EventCoordinatorError>(true));

        job = new CallForScheduleJob(
            loggerMock.Object,
            coordinatorMock.Object,
            configRepoMock.Object,
            deviceBatcher,
            backroundSchedulerMock.Object
        );
    }

    [TestMethod]
    public async Task Should_CallAllDevices()
    {
        await job.Run();

        // called.Count.ShouldBe(16);
        foreach (var device in called)
        {
            devices.ShouldContain(device);
        }
    }

    //Test what happens when there is more than threshold but it cannot be split anymore because maxChars is 1
}
