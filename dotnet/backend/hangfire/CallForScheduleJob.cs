using backend.Services.Batching;
using Hangfire;
using persistence.Repository;
using SseHandler;
using SseHandler.Commands;

namespace backend.Hangfire;

public class CallForScheduleJob
{
    private readonly ILogger<CallForScheduleJob> _logger;
    private readonly IEventCoordinator _eventCoordinator;
    private readonly IConfigurationRepository _configRepository;
    private readonly IDeviceBatcher<Guid> _deviceBatcher;
    private readonly IBackgroundJobClientV2 _backgroundJobClient;
    private uint threashold =>
        uint.Parse(Environment.GetEnvironmentVariable("CALLFORSCHEDULE_DEVICE_THRESHOLD")!);

    private static readonly string nextKey = "CALL_FOR_SCHEDULE_NEXT_KEY";
    private static readonly string maxChars = "CALL_FOR_SCHEDULE_MAX_CHARS";

    public CallForScheduleJob(
        ILogger<CallForScheduleJob> logger,
        IEventCoordinator eventCoordinator,
        IConfigurationRepository configRepo,
        IDeviceBatcher<Guid> deviceBatcher,
        IBackgroundJobClientV2 backgroundJobClient
    )
    {
        _logger = logger;
        _eventCoordinator = eventCoordinator;
        _configRepository = configRepo;
        _deviceBatcher = deviceBatcher;
        _backgroundJobClient = backgroundJobClient;
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

        var maybeCurrentKey = await _configRepository.GetSingle(nextKey);
        var (currentKey, newKey) = maybeCurrentKey.IsSuccessful
            ? (maybeCurrentKey.Value, false)
            : (new model.Core.Configuration() { Id = nextKey, Value = 'A'.ToString() }, true);
        var key = currentKey.Value![0];

        var maybeCurrentMaxChars = await _configRepository.GetSingle(maxChars);
        var (currentMaxChars, newChars) = maybeCurrentMaxChars.IsSuccessful
            ? (maybeCurrentMaxChars.Value, false)
            : (new model.Core.Configuration() { Id = maxChars, Value = 36.ToString() }, true);
        var chars = uint.TryParse(currentMaxChars.Value, out uint outChars) ? outChars : 36;

        IEnumerable<Guid> batch;

        while (true)
        {
            _logger.LogInformation(
                "Running calculating of batch to call starting from {0} and taking {1} chars",
                key,
                chars
            );

            var result = _deviceBatcher.NextBatch(_eventCoordinator.GetConnectionIds(), key, chars);

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
            chars /= 2;
        }

        var jobs = batch
            .Select(x => _eventCoordinator.SendMessage(x, Command.CallForSchedule))
            .ToList();
        await Task.WhenAll(jobs);

        currentKey.Value = _deviceBatcher.NextKey(key, chars).ToString();
        currentMaxChars.Value = chars.ToString();

        var configs = new[] { (currentKey, newKey), (currentMaxChars, newChars) };
        foreach (var (config, newConfig) in configs)
        {
            var result = newConfig switch
            {
                true => await _configRepository.Add(config),
                false => await _configRepository.Update(config)
            };
            if (result.IsSuccessful)
                continue;

            _logger.LogError("Failed to save config '{0}' due to: {1}", config.Id, result.Error);
        }

        _logger.LogInformation("Finished calling devices for schedule");

        var factor = 36 / chars;
        var actual = maxSpan.Divide(factor);

        _logger.LogInformation("Queueing next execution in {0}", actual);
        _backgroundJobClient.Schedule<CallForScheduleJob>(
            nameof(CallForScheduleJob).ToLowerInvariant(),
            job => job.Run(),
            actual
        );
    }
}
