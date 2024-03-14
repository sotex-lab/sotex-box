using model.Core;

namespace model.Contracts;

public class AdContract
{
    public AdScope Scope { get; set; }
    public IEnumerable<string> Tags { get; set; } = new List<string>();
    public Guid Id { get; set; }
}
