using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using SseHandler.Serializers;

namespace SseHandler.Metrics;

internal static class DeviceMetricsExtensions
{
    internal static void AddDeviceMetrics(
        this IServiceCollection services,
        EventSerializer eventSerializer
    )
    {
        services.AddSingleton<IDeviceMetrics>(x => new DeviceMetrics(
            x.GetRequiredService<IMeterFactory>(),
            eventSerializer
        ));
    }
}

public interface IDeviceMetrics
{
    public static string MeterName { get; } = "Sotex.Web";

    void Connected(Guid key);
    void Disconnected(Guid key);
    void Sent(Guid key, object message);
}

public class DeviceMetrics : IDeviceMetrics
{
    private class FakeMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose() { }
    }

    private class MetricBag
    {
        public Measurement<int> IsConnected { get; set; }
        public Measurement<long> SentBytes { get; set; }
    }

    private readonly Dictionary<Guid, MetricBag> _measurements;
    private readonly EventSerializer _eventSerializer;
    private readonly Mutex _mutex = new();

    public DeviceMetrics()
        : this(new FakeMeterFactory(), new JsonEventSerializer()) { }

    public DeviceMetrics(IMeterFactory meterFactory, EventSerializer eventSerializer)
    {
        _eventSerializer = eventSerializer;
        _measurements = new Dictionary<Guid, MetricBag>();
        var meter = meterFactory.Create("Sotex.Web");
        meter.CreateObservableCounter(
            "sotex.web.devices",
            () => _measurements.Values.Select(x => x.IsConnected),
            "num",
            "The number of devices currently connected to the backend instance"
        );

        meter.CreateObservableCounter(
            "sotex.web.sent.bytes",
            () => _measurements.Values.Select(x => x.SentBytes),
            "B",
            "Amount of bytes sent to device via server sent events"
        );
    }

    private KeyValuePair<string, object?> Tag(Guid key) =>
        new KeyValuePair<string, object?>("id", key);

    public void Connected(Guid key)
    {
        _mutex.WaitOne();
        if (!_measurements.ContainsKey(key))
            _measurements[key] = new MetricBag();
        _measurements[key].IsConnected = new Measurement<int>(1, Tag(key));
        _measurements[key].SentBytes = new Measurement<long>(0, Tag(key));
        _mutex.ReleaseMutex();
    }

    public void Disconnected(Guid key)
    {
        if (!_measurements.ContainsKey(key))
            return;
        _mutex.WaitOne();
        _measurements[key].IsConnected = new Measurement<int>(0, Tag(key));
        _measurements[key].SentBytes = new Measurement<long>(0, Tag(key));
        _mutex.ReleaseMutex();
    }

    public void Sent(Guid key, object message)
    {
        if (!_measurements.ContainsKey(key))
            return;
        var serialized = _eventSerializer.SerializeData(message);
        long bytes = serialized.Length * sizeof(char);
        _mutex.WaitOne();
        _measurements[key].SentBytes = new Measurement<long>(
            _measurements[key].SentBytes.Value + bytes,
            Tag(key)
        );
        _mutex.ReleaseMutex();
    }
}
