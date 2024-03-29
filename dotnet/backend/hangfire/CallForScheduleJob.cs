using persistence.Repository;
using SseHandler;

namespace backend.Hangfire;

public class CallForScheduleJob : GenericCronJob<CallForScheduleJob>, IGenericCronJob
{
    private readonly IEventCoordinator _eventCoordinator;
    private readonly IConfigurationRepository _configRepository;

    public CallForScheduleJob(
        ILogger<GenericCronJob<CallForScheduleJob>> logger,
        IEventCoordinator eventCoordinator,
        IConfigurationRepository configRepo
    )
        : base(logger)
    {
        _eventCoordinator = eventCoordinator;
        _configRepository = configRepo;
    }

    public static string EnvironmentVariableName => "CALLFORSCHEDULE_CRON";

    public override async Task Run()
    {
        var maybeCurrentKey = await _configRepository.GetSingle("CALL_FOR_SCHEDULE_NEXT_KEY");
        var key = maybeCurrentKey.IsSuccessful ? maybeCurrentKey.Value.Value![0] : 'A';
    }
}
