using backend.Services.Batching;
using persistence.Repository;
using SseHandler;
using SseHandler.Commands;

namespace backend.Hangfire;

public class CallForScheduleJob : GenericCronJob<CallForScheduleJob>, IGenericCronJob
{
    private readonly IEventCoordinator _eventCoordinator;
    private readonly IConfigurationRepository _configRepository;
    private readonly IDeviceBatcher<Guid> _deviceBatcher;
    private uint threashold =>
        uint.Parse(Environment.GetEnvironmentVariable("CALLFORSCHEDULE_DEVICE_THRESHOLD")!);

    public CallForScheduleJob(
        ILogger<GenericCronJob<CallForScheduleJob>> logger,
        IEventCoordinator eventCoordinator,
        IConfigurationRepository configRepo,
        IDeviceBatcher<Guid> deviceBatcher
    )
        : base(logger)
    {
        _eventCoordinator = eventCoordinator;
        _configRepository = configRepo;
        _deviceBatcher = deviceBatcher;
    }

    public static string EnvironmentVariableName => "CALLFORSCHEDULE_CRON";

    public override async Task Run()
    {
        var maybeCurrentKey = await _configRepository.GetSingle("CALL_FOR_SCHEDULE_NEXT_KEY");
        var key = maybeCurrentKey.IsSuccessful ? maybeCurrentKey.Value.Value![0] : 'A';
        var maybeCurrentMaxChars = await _configRepository.GetSingle("CALL_FOR_SCHEDULE_MAX_CHARS");
        var maxChars = maybeCurrentMaxChars.IsSuccessful
            ? uint.TryParse(maybeCurrentMaxChars.Value.Value, out uint chars)
                ? chars
                : 36
            : 36;

        IEnumerable<Guid> batch;

        while (true)
        {
            _logger.LogInformation(
                "Running calculating of batch to call starting from {0} and taking {1} chars",
                key,
                maxChars
            );

            var result = _deviceBatcher.NextBatch(
                _eventCoordinator.GetConnectionIds(),
                key,
                maxChars
            );

            if (!result.IsSuccessful)
            {
                var formatted = string.Format("Unexpected result: {0}", result.Error);
                _logger.LogError(formatted);
                throw new Exception(formatted);
            }

            batch = result.Value;
            if (batch.Count() <= threashold)
                break;

            _logger.LogInformation(
                "Device count '{0}' is greater than threshold '{1}', reducing...",
                batch.Count(),
                threashold
            );
            maxChars -= 1;
        }

        var jobs = batch
            .Select(x => _eventCoordinator.SendMessage(x, Command.CallForSchedule))
            .ToList();
        await Task.WhenAll(jobs);

        maybeCurrentKey.Value.Value = _deviceBatcher.NextKey(key, maxChars).ToString();
        maybeCurrentMaxChars.Value.Value = maxChars.ToString();

        var tasks = new[]
        {
            _configRepository.Update(maybeCurrentKey.Value),
            _configRepository.Update(maybeCurrentMaxChars.Value)
        };
        await Task.WhenAll(tasks);

        _logger.LogInformation("Finished calling devices for schedule");
    }
}
