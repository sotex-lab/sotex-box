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
    private readonly IEventCoordinator _eventCoordinator;
    private readonly IConfigurationRepository _configRepository;
    private readonly IBackgroundJobClientV2 _backgroundJobClient;
    private readonly IDeviceRepository _deviceRepository;
    private uint pageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("SCHEDULE_DEVICE_THRESHOLD")!);
    private uint adPageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("SCHEDULE_AD_THRESHOLD")!);
    private TimeSpan expiration =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("SCHEDULE_URL_EXPIRE")!);
    public TimeSpan maxSpan =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("SCHEDULE_MAX_DELAY")!);

    private static readonly string pageKey = "CALL_FOR_SCHEDULE_PAGE";
    private readonly IMapper _mapper;
    private readonly IPreSignObjectService _presigner;
    private readonly IGetOrCreateBucketService _bucketService;
    private readonly IPutObjectService _putObjectService;
    private readonly IGetObjectService _getObjectService;
    private readonly IAdRepository _adRepository;

    public ScheduleJob(
        ILogger<ScheduleJob> logger,
        IEventCoordinator eventCoordinator,
        IConfigurationRepository configRepo,
        IBackgroundJobClientV2 backgroundJobClient,
        IDeviceRepository deviceRepository,
        IMapper mapper,
        IPreSignObjectService preSignObjectService,
        IGetOrCreateBucketService getOrCreateBucketService,
        IPutObjectService putObjectService,
        IGetObjectService getObjectService,
        IAdRepository adRepository
    )
    {
        _logger = logger;
        _eventCoordinator = eventCoordinator;
        _configRepository = configRepo;
        _backgroundJobClient = backgroundJobClient;
        _deviceRepository = deviceRepository;
        _mapper = mapper;
        _presigner = preSignObjectService;
        _bucketService = getOrCreateBucketService;
        _putObjectService = putObjectService;
        _getObjectService = getObjectService;
        _adRepository = adRepository;
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

        var maybeScheduleBucket = await _bucketService.GetSchedule();
        if (!maybeScheduleBucket.IsSuccessful)
        {
            _logger.LogError(
                "Failed to get bucket info for schedules: {0}",
                maybeScheduleBucket.Error.ToString()
            );
            return;
        }

        var maybeNonProcessedBucket = await _bucketService.GetNonProcessed();
        if (!maybeNonProcessedBucket.IsSuccessful)
        {
            _logger.LogError(
                "Failed to get bucket info for non processed: {0}",
                maybeNonProcessedBucket.Error.ToString()
            );
            return;
        }

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
            await Calculate(device, maybeScheduleBucket.Value, maybeNonProcessedBucket.Value);
            await _eventCoordinator.SendMessage(device.Id, Command.CallForSchedule);
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

    private async Task Calculate(
        Device device,
        S3Bucket scheduleBucket,
        S3Bucket nonProcessedBucket
    )
    {
        ScheduleContract oldSchedule = new ScheduleContract();
        var maybeOldSchedule = await _getObjectService.GetObjectByKey(
            scheduleBucket,
            device.Id.ToString()
        );
        if (maybeOldSchedule.IsSuccessful)
        {
            oldSchedule = maybeOldSchedule.Value;
        }

        var maybeLastAd = oldSchedule.Schedule.LastOrDefault();
        var newBatchOfAds = await _adRepository.TakeFrom(
            maybeLastAd == null ? Guid.Empty : maybeLastAd.Ad!.Id,
            adPageSize
        );

        var response = await _putObjectService.Put(
            scheduleBucket,
            device.Id.ToString(),
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new ScheduleContract
                        {
                            CreatedAt = DateTime.Now,
                            DeviceId = device.Id,
                            Schedule = newBatchOfAds
                                .Select(async x => new ScheduleItemContract
                                {
                                    Ad = _mapper.Map<AdContract>(x),
                                    DownloadLink = await GetLink(x.Id, nonProcessedBucket)
                                })
                                .Select(x => x.Result)
                        }
                    )
                )
            )
        );
        if (!response.IsSuccessful)
        {
            _logger.LogError(
                "Couldn't persist schedule due to error: {0}",
                response.Error.ToString()
            );
        }
    }

    private async Task<string> GetLink(Guid id, Amazon.S3.Model.S3Bucket bucket)
    {
        var maybePresigned = await _presigner.Get(
            bucket,
            id.ToString(),
            DateTime.Now.Add(expiration)
        );
        if (!maybePresigned.IsSuccessful)
        {
            throw new Exception(
                string.Format("Received error: {0}", maybePresigned.Error.ToString())
            );
        }

        return maybePresigned.Value;
    }
}
