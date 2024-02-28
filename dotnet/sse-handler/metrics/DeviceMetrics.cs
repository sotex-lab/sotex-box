using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace SseHandler.Metrics;

public static class DeviceMetricsExtensions
{
    public static void AddDeviceMetrics(this IServiceCollection services)
    {
        services.AddSingleton<IDeviceMetrics>(x => new DeviceMetrics(
            x.GetRequiredService<IMeterFactory>()
        ));
    }
}

public interface IDeviceMetrics
{
    public static string MeterName { get; } = "Sotex.Web";

    void Connected(string key);
    void Disconnected(string key);
}

public class DeviceMetrics : IDeviceMetrics
{
    private readonly Dictionary<string, Measurement<int>> _measurements;
    private readonly Mutex _mutex = new();

    private class FakeMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);

        public void Dispose() { }
    }

    public DeviceMetrics()
        : this(new FakeMeterFactory()) { }

    public DeviceMetrics(IMeterFactory meterFactory)
    {
        _measurements = new Dictionary<string, Measurement<int>>();
        meterFactory
            .Create("Sotex.Web")
            .CreateObservableCounter(
                "sotex.web.devices",
                () => _measurements.Values,
                "num",
                "The number of devices currently connected to the backend instance"
            );
    }

    private KeyValuePair<string, object?> Tag(string key) =>
        new KeyValuePair<string, object?>("id", key);

    public void Connected(string key)
    {
        _mutex.WaitOne();
        _measurements[key] = new Measurement<int>(1, Tag(key));
        _mutex.ReleaseMutex();
    }

    public void Disconnected(string key)
    {
        _mutex.WaitOne();
        _measurements[key] = new Measurement<int>(0, Tag(key));
        _mutex.ReleaseMutex();
    }
}
