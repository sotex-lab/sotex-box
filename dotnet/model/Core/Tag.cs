using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace model.Core;

public class Tag : Entity<Guid>
{
    public string Name { get; set; } = "";
    public IEnumerable<Ad> Ads { get; set; } = new List<Ad>();
}
