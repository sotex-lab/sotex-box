using Amazon.Runtime.SharedInterfaces;
using backend.Services.Aws;
using DotNext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using persistence.Repository;

namespace backend.Controllers;

enum ScheduleError
{
    General = 1,
    Bucket,
    PreSign,
    UnknownId
}

[ApiController]
[Route("/[controller]")]
public class ScheduleController(
    IPreSignObjectService preSignObjectService,
    IDeviceRepository deviceRepository,
    IGetOrCreateBucketService getOrCreateBucketService,
    IMemoryCache cache,
    ILogger<ScheduleController> logger
) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var key = string.Format("{0}-schedule-url", id);
        var entry = await cache.GetOrCreateAsync(
            key,
            async (cacheEntry) =>
            {
                var maximumExpiration = TimeSpan.Parse(
                    Environment.GetEnvironmentVariable("CALLFORSCHEDULE_MAX_DELAY")!
                );

                var maybeDevice = await deviceRepository.GetSingle(id);
                if (!maybeDevice.IsSuccessful)
                {
                    logger.LogWarning("Requested schedule for unknown device: {0}", id);
                    return new Result<string, ScheduleError>(ScheduleError.UnknownId);
                }

                var maybeScheduleBucket = await getOrCreateBucketService.GetSchedule();
                if (!maybeScheduleBucket.IsSuccessful)
                {
                    logger.LogError(
                        "Couldn't get schedule bucket: {0}",
                        maybeScheduleBucket.Error.ToString()
                    );
                    return new Result<string, ScheduleError>(ScheduleError.Bucket);
                }

                var maybeUrl = await preSignObjectService.Get(
                    maybeScheduleBucket.Value,
                    id.ToString()
                );
                if (!maybeUrl.IsSuccessful)
                {
                    logger.LogError("Couldn't presign url: {0}", maybeUrl.Error.ToString());
                    return new Result<string, ScheduleError>(ScheduleError.PreSign);
                }

                cacheEntry.SetValue(maybeUrl.Value);
                cacheEntry.SetSlidingExpiration(maximumExpiration);

                return new Result<string, ScheduleError>(maybeUrl.Value);
            }
        );

        return entry.IsSuccessful ? Ok(entry.Value) : BadRequest(entry.Error.ToString());
    }
}
