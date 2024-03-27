using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace persistence.Converters;

public class DateTimeValueConverter : ValueConverter<DateTime, long>
{
    public DateTimeValueConverter()
        : base(
            dt => new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeMilliseconds(),
            ts => DateTimeOffset.FromUnixTimeMilliseconds(ts).UtcDateTime
        ) { }
}
