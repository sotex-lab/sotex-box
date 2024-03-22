using System.Net;

namespace model.Core;

public class Device : Entity<Guid>
{
    public string? UtilityName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public IPAddress? Ip { get; set; }
}
