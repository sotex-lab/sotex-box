using System.Text;
using Amazon.S3.Model;
using AutoMapper;
using backend.Services.Aws;
using Hangfire;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using persistence.Repository;
using persistence.Repository.Base;
using SseHandler;
using SseHandler.Commands;

namespace backend.Hangfire;

public class ScheduleJob
{
    private readonly ILogger<ScheduleJob> _logger;
    private readonly IConfigurationRepository _configRepository;
    private readonly IBackgroundJobClientV2 _backgroundJobClient;
    private readonly IDeviceRepository _deviceRepository;
    private uint pageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("SCHEDULE_DEVICE_THRESHOLD")!);
    public TimeSpan maxSpan =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("SCHEDULE_MAX_DELAY")!);

    private static readonly string pageKey = "CALL_FOR_SCHEDULE_PAGE";

    public ScheduleJob(
        ILogger<ScheduleJob> logger,
        IConfigurationRepository configRepo,
        IBackgroundJobClientV2 backgroundJobClient,
        IDeviceRepository deviceRepository
    )
    {
        _logger = logger;
        _configRepository = configRepo;
        _backgroundJobClient = backgroundJobClient;
        _deviceRepository = deviceRepository;
    }

    public async Task Run()
    {
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
            _backgroundJobClient.Schedule<ScheduleJob>(job => job.Run(), maxSpan);
            return;
        }
        var devices = GetBatch(ref pageNumber, pageSize);

        _logger.LogInformation("Calculating schedules for {0} devices", devices.Count);

        foreach (var device in devices)
        {
            _backgroundJobClient.Enqueue<CalculateJob>(j => j.Calculate(device.Id));
        }

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
        _backgroundJobClient.Schedule<ScheduleJob>(job => job.Run(), next);
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
