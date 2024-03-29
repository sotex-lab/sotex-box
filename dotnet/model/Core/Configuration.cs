namespace model.Core;

public class Configuration : Entity<string>
{
    public string? Key { get; set; }
    public string? Value { get; set; }
}
