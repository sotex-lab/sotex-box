using System.Text;
using AutoMapper;
using backend.Services.Aws;
using model.Contracts;
using Newtonsoft.Json;
using persistence.Repository;
using persistence.Repository.Base;

namespace backend.Hangfire;

public class CalculateScheduleForDeviceJob
{
    private ILogger<CalculateScheduleForDeviceJob> _logger;
    private uint pageSize =>
        uint.Parse(Environment.GetEnvironmentVariable("CALCULATESCHEDULEFOR_DEVICE_THRESHOLD")!);
    private TimeSpan expiration =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("CALCULATESCHEDULEFOR_URL_EXPIRE")!);
    private readonly IAdRepository _adRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMapper _mapper;
    private readonly IPreSignObjectService _presigner;
    private readonly IGetOrCreateBucketService _bucketService;
    private readonly IPutObjectService _putObjectService;
    private readonly IGetObjectService _getObjectService;

    public CalculateScheduleForDeviceJob(
        ILogger<CalculateScheduleForDeviceJob> logger,
        IAdRepository adRepository,
        IDeviceRepository deviceRepository,
        IMapper mapper,
        IPreSignObjectService preSign,
        IGetOrCreateBucketService getOrCreate,
        IPutObjectService putObjectService,
        IGetObjectService getObjectService
    )
    {
        _adRepository = adRepository;
        _logger = logger;
        _deviceRepository = deviceRepository;
        _mapper = mapper;
        _presigner = preSign;
        _bucketService = getOrCreate;
        _putObjectService = putObjectService;
        _getObjectService = getObjectService;
    }

    public async Task Calculate(string deviceIdString)
    {
        if (!Guid.TryParse(deviceIdString, out var deviceId))
        {
            _logger.LogError("Couldn't parse string {0} as a valid Guid", deviceIdString);
            return;
        }
        var maybeBucket = await _bucketService.GetProcessed();
        if (!maybeBucket.IsSuccessful)
        {
            _logger.LogError(
                "Couldn't retrieve processed bucket with error: {0}",
                maybeBucket.Error.ToString()
            );
            return;
        }

        var maybeDevice = await _deviceRepository.GetSingle(deviceId);
        if (!maybeDevice.IsSuccessful)
        {
            _logger.LogError(
                "Calculation of schedule was called for device {0} which couldn't be retrieved from storage due to: {1}",
                deviceId,
                maybeDevice.Error.Stringify()
            );
            return;
        }

        var maybeScheduleBucket = await _bucketService.GetSchedule();
        if (!maybeScheduleBucket.IsSuccessful)
        {
            _logger.LogInformation(
                "Failed to get bucket info for schedules: {0}",
                maybeScheduleBucket.Error.ToString()
            );
            return;
        }

        ScheduleContract oldSchedule = new ScheduleContract();
        var maybeOldSchedule = await _getObjectService.GetObjectByKey(
            maybeScheduleBucket.Value,
            deviceIdString
        );
        if (maybeOldSchedule.IsSuccessful)
        {
            oldSchedule = maybeOldSchedule.Value;
        }

        var maybeLastAd = oldSchedule.Schedule.LastOrDefault();
        var newBatchOfAds = await _adRepository.TakeFrom(
            maybeLastAd == null ? Guid.Empty : maybeLastAd.Ad!.Id,
            pageSize
        );

        var response = await _putObjectService.Put(
            maybeScheduleBucket.Value,
            deviceIdString,
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(
                        new ScheduleContract
                        {
                            CreatedAt = DateTime.Now,
                            DeviceId = maybeDevice.Value.Id,
                            Schedule = newBatchOfAds
                                .Select(async x => new ScheduleItemContract
                                {
                                    Ad = _mapper.Map<AdContract>(x),
                                    DownloadLink = await GetLink(x.Id, maybeBucket.Value)
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
