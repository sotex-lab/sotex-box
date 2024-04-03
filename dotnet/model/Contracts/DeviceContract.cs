using System.Net;

namespace model.Contracts;

public class DeviceContract
{
    public Guid Id { get; set; }
    public required string UtilityName { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedDate { get; set; }
}
