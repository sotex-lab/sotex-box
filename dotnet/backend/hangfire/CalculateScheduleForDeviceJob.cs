using Amazon.S3;
using AutoMapper;
using backend.Services.Aws;
using backend.Services.Files;
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
    private string localStoragePath =>
        Environment.GetEnvironmentVariable("CALCULATESCHEDULEFOR_DEVICE_LOCALPATH")!;
    private TimeSpan expiration =>
        TimeSpan.Parse(Environment.GetEnvironmentVariable("CALCULATESCHEDULEFOR_URL_EXPIRE")!);
    private readonly IAdRepository _adRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IMapper _mapper;
    private readonly IPreSignObjectService _presigner;
    private readonly IGetOrCreateBucketService _bucketService;
    private readonly IFileUtil _fileUtil;

    public CalculateScheduleForDeviceJob(
        ILogger<CalculateScheduleForDeviceJob> logger,
        IAdRepository adRepository,
        IDeviceRepository deviceRepository,
        IMapper mapper,
        IPreSignObjectService preSign,
        IGetOrCreateBucketService getOrCreate,
        IFileUtil fileUtil
    )
    {
        _adRepository = adRepository;
        _logger = logger;
        _deviceRepository = deviceRepository;
        _mapper = mapper;
        _presigner = preSign;
        _bucketService = getOrCreate;
        _fileUtil = fileUtil;
    }

    public async Task Calculate(Guid deviceId)
    {
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

        if (!_fileUtil.DirectoryExists(localStoragePath))
        {
            _logger.LogInformation("Directory {0} doesn't exist, creating...", localStoragePath);
            _fileUtil.CreateDirectory(localStoragePath);
        }

        ScheduleContract oldSchedule = new ScheduleContract();
        var filePath = Path.Combine(localStoragePath, string.Format("{0}.json", deviceId));
        if (_fileUtil.FileExists(filePath))
        {
            _logger.LogInformation("Found previous schedule for device {0}", deviceId);
            oldSchedule = JsonConvert.DeserializeObject<ScheduleContract>(
                await File.ReadAllTextAsync(filePath)
            );
        }

        var maybeLastAd = oldSchedule.Schedule.LastOrDefault();
        var newBatchOfAds = await _adRepository.TakeFrom(
            maybeLastAd == null ? Guid.Empty : maybeLastAd.Ad!.Id,
            pageSize
        );

        await _fileUtil.WriteAllTextAsync(
            filePath,
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
        );
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
