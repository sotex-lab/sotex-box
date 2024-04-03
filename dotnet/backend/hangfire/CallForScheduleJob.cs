using DotNext.Collections.Generic;
using Hangfire;
using model.Core;
using persistence.Repository;
using persistence.Repository.Base;
using SseHandler;
using SseHandler.Commands;

namespace backend.Hangfire;

public class CallForScheduleJob
{
    private readonly ILogger<CallForScheduleJob> _logger;
    private readonly IEventCoordinator _eventCoordinator;
    private readonly IConfigurationRepository _configRepository;
    private readonly IBackgroundJobClientV2 _backgroundJobClient;
    private readonly IDeviceRepository _deviceRepository;
    private uint pageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("CALLFORSCHEDULE_DEVICE_THRESHOLD")!);

    private static readonly string pageKey = "CALL_FOR_SCHEDULE_PAGE";

    public CallForScheduleJob(
        ILogger<CallForScheduleJob> logger,
        IEventCoordinator eventCoordinator,
        IConfigurationRepository configRepo,
        IBackgroundJobClientV2 backgroundJobClient,
        IDeviceRepository deviceRepository
    )
    {
        _logger = logger;
        _eventCoordinator = eventCoordinator;
        _configRepository = configRepo;
        _backgroundJobClient = backgroundJobClient;
        _deviceRepository = deviceRepository;
    }

    public static string EnvironmentVariableName => "CALLFORSCHEDULE_MAX_DELAY";

    public async Task Run()
    {
        var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariableName)!;
        if (!TimeSpan.TryParse(fromEnv, out var maxSpan))
        {
            _logger.LogError(
                "Couldn't parse '{0}' with value: {1}",
                EnvironmentVariableName,
                fromEnv
            );
            return;
        }

        var maybeCurrentPage = await _configRepository.GetSingle(pageKey);
        var (currentPage, shouldAdd) = maybeCurrentPage.IsSuccessful
            ? (maybeCurrentPage.Value, false)
            : (new Configuration { Id = pageKey, Value = 0.ToString() }, true);
        var pageNumber = uint.TryParse(currentPage.Value, out var parsed) ? parsed : 0;

        _logger.LogInformation(
            "Calling for schedule page {0} and page size {1}",
            pageNumber,
            pageSize
        );

        var total = await _deviceRepository.Count();
        if (total == 0)
        {
            _logger.LogInformation("No devices. Skipping...");
            _backgroundJobClient.Schedule<CallForScheduleJob>(job => job.Run(), maxSpan);
            return;
        }
        var devices = GetBatch(ref pageNumber, pageSize);

        _logger.LogInformation("Calling {0} devices for schedule", devices.Count);

        await Task.WhenAll(
            devices.Select(x => _eventCoordinator.SendMessage(x.Id, Command.CallForSchedule))
        );

        currentPage.Value = pageNumber.ToString();
        var response = shouldAdd switch
        {
            true => await _configRepository.Add(currentPage),
            false => await _configRepository.Update(currentPage)
        };
        if (!response.IsSuccessful)
        {
            _logger.LogError("Couldn't save config: {0}", response.Error.Stringify());
        }

        var batches = (int)Math.Ceiling((double)total / pageSize);

        var next = maxSpan.Divide(batches);

        _logger.LogInformation("Scheduling next execution in {0}", next);
        _backgroundJobClient.Schedule<CallForScheduleJob>(job => job.Run(), next);

        _logger.LogInformation("Scheduling calulation of schedule for next batch");
        devices = GetBatch(ref pageNumber, pageSize);

        devices.ForEach(x =>
            _backgroundJobClient.Enqueue<CalculateScheduleForDeviceJob>(job =>
                job.Calculate(x.Id.ToString())
            )
        );
    }

    private List<Device> GetBatch(ref uint pageNumber, uint pageSize)
    {
        while (true)
        {
            var devices = _deviceRepository.GetPage(pageNumber, pageSize).ToList();

            if (devices.Count != 0)
            {
                pageNumber += 1;
                return devices;
            }

            pageNumber = 0;
        }
    }
}
