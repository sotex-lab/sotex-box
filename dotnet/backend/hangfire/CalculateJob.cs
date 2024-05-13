using System.Text;
using Amazon.S3.Model;
using AutoMapper;
using backend.Services.Aws;
using model.Contracts;
using model.Core;
using Newtonsoft.Json;
using persistence.Repository;
using SseHandler;
using SseHandler.Commands;

namespace backend.Hangfire;

public class CalculateJob
{
    private readonly IAdRepository _adRepository;
    private readonly IGetObjectService _getObjectService;
    private readonly IPutObjectService _putObjectService;
    private IMapper _mapper;
    private readonly ILogger<CalculateJob> _logger;
    private readonly IPreSignObjectService _presigner;
    private readonly IGetOrCreateBucketService _bucketService;
    private readonly IEventCoordinator _eventCoordinator;
    private uint adPageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("CALCULATE_AD_THRESHOLD")!);
    private TimeSpan expiration =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("CALCULATE_URL_EXPIRE")!);

    public CalculateJob(
        IAdRepository adRepository,
        IGetObjectService getObjectService,
        IPutObjectService putObjectService,
        IMapper mapper,
        ILogger<CalculateJob> logger,
        IPreSignObjectService presigner,
        IGetOrCreateBucketService bucketService,
        IEventCoordinator eventCoordinator
    )
    {
        _adRepository = adRepository;
        _getObjectService = getObjectService;
        _putObjectService = putObjectService;
        _mapper = mapper;
        _logger = logger;
        _presigner = presigner;
        _bucketService = bucketService;
        _eventCoordinator = eventCoordinator;
    }

    public async Task Calculate(Guid device)
    {
        var maybeProcessedBucket = await _bucketService.GetProcessed();
        if (!maybeProcessedBucket.IsSuccessful)
        {
            _logger.LogError(
                "Failed to get bucket info for processed: {0}",
                maybeProcessedBucket.Error.ToString()
            );
            return;
        }

        var maybeScheduleBucket = await _bucketService.GetSchedule();
        if (!maybeScheduleBucket.IsSuccessful)
        {
            _logger.LogError(
                "Failed to get bucket info for schedules: {0}",
                maybeScheduleBucket.Error.ToString()
            );
            return;
        }
        var bucket = maybeScheduleBucket.Value;
        ScheduleContract oldSchedule = new ScheduleContract();
        var maybeOldSchedule = await _getObjectService.GetObjectByKey(bucket, device.ToString());
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
            bucket,
            device.ToString(),
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new ScheduleContract
                        {
                            CreatedAt = DateTime.Now,
                            DeviceId = device,
                            Schedule = newBatchOfAds
                                .Select(async x => new ScheduleItemContract
                                {
                                    Ad = _mapper.Map<AdContract>(x),
                                    DownloadLink = await GetLink(x.Id, maybeProcessedBucket.Value)
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
            return;
        }

        await _eventCoordinator.SendMessage(device, Command.CallForSchedule);
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
