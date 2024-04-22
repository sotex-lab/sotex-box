namespace model.Contracts;

public class ScheduleContract
{
    public DateTime CreatedAt { get; set; }
    public Guid DeviceId { get; set; }
    public IEnumerable<ScheduleItemContract> Schedule { get; set; } =
        new List<ScheduleItemContract>();
}

public class ScheduleItemContract
{
    public string? DownloadLink { get; set; }
    public AdContract? Ad { get; set; }
}
