namespace SseHandler;

public class Connection
{
    public Guid Id { get; }
    public Stream Stream { get; }
    public CancellationTokenSource CancellationTokenSource { get; }

    public Connection(Guid id, Stream stream)
    {
        Id = id;
        Stream = stream;
        CancellationTokenSource = new CancellationTokenSource();
    }
}
