using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace model.Core;

public class Ad
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }
    public AdScope AdScope { get; set; }
    public string? ObjectId { get; set; }
    public IEnumerable<Tag> Tags { get; set; } = new List<Tag>();
}

public enum AdScope
{
    Global,
    Channel,
    Private
}
