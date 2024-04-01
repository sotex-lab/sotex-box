using System.Runtime.InteropServices;
using DotNext;

namespace backend.Services.Batching;

public interface IDeviceBatcher<T>
{
    Result<IEnumerable<T>, BatchingError> NextBatch(
        IEnumerable<T> ids,
        char starting,
        uint maxChars
    );
    char NextKey(char starting, uint maxChars);
}

public enum BatchingError
{
    BadStartingChar = 1,
    InvalidMaxChars,
    Unknown,
}

public class DeviceBatcher<T> : IDeviceBatcher<T>
{
    private static List<char> chars = new[] { Enumerable.Range('A', 6), Enumerable.Range('0', 10) }
        .SelectMany(x => x)
        .Select(x => (char)x)
        .ToList();

    public DeviceBatcher() { }

    public Result<IEnumerable<T>, BatchingError> NextBatch(
        IEnumerable<T> ids,
        char starting,
        uint maxChars
    )
    {
        if (ids is null || !ids.Any())
            return new Result<IEnumerable<T>, BatchingError>(new List<T>());

        if (!chars.Contains(starting))
            return new Result<IEnumerable<T>, BatchingError>(BatchingError.BadStartingChar);

        if (maxChars == 0 || maxChars > 16)
            return new Result<IEnumerable<T>, BatchingError>(BatchingError.InvalidMaxChars);

        var indexOfStarting = chars.IndexOf(starting);
        var batch = chars
            .Select((x, i) => new { Index = i, Value = x })
            .Where(x => x.Index >= indexOfStarting && x.Index <= (indexOfStarting + maxChars - 1))
            .Select(x => x.Value)
            .ToList();

        return new Result<IEnumerable<T>, BatchingError>(
            ids.Where(x => batch.Contains(x!.ToString()![0]))
        );
    }

    public char NextKey(char starting, uint maxChars)
    {
        var index = chars.IndexOf(starting) + maxChars;
        return index >= 16 ? chars[0] : chars[(int)index];
    }
}
