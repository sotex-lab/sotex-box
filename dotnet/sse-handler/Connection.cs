namespace SseHandler;

public class Connection
{
    public string Id { get; }
    public Stream Stream { get; }
    public CancellationTokenSource CancellationTokenSource { get; }

    public Connection(string id, Stream stream)
    {
        Id = id;
        Stream = stream;
        CancellationTokenSource = new CancellationTokenSource();
    }
}
