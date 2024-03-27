using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using SseHandler;
using SseHandler.EventCoordinators;
using SseHandler.Metrics;
using SseHandler.Serializers;

[MemoryDiagnoser]
public class Benchmarks
{
    // Ammount of active devices
    [Params(100, 10_000, 100_000)]
    public int InitialSize { get; set; }

    // Implementation that is running
    [Params(
        typeof(EventCoordinatorConcurrentDictionary),
        typeof(EventCoordinatorMutex),
        typeof(EventCoordinatorReaderWriterLock)
    )]
    public Type Implementation { get; set; }

    private static Dictionary<Type, IDictionary<Guid, Connection>> ConnectionsMapping =
        new Dictionary<Type, IDictionary<Guid, Connection>>
        {
            [typeof(EventCoordinatorConcurrentDictionary)] =
                new ConcurrentDictionary<Guid, Connection>(),
            [typeof(EventCoordinatorMutex)] = new Dictionary<Guid, Connection>(),
            [typeof(EventCoordinatorReaderWriterLock)] = new Dictionary<Guid, Connection>()
        };

    private IEventCoordinator eventCoordinator;
    private List<Guid> guids = Enumerable.Range(0, 1000).Select(_ => Guid.NewGuid()).ToList();

    [GlobalSetup]
    public void GlobalSetup()
    {
        IDictionary<Guid, Connection> connections = ConnectionsMapping[Implementation];
        if (connections.Count > 0)
        {
            connections.Clear();
        }
        for (int i = 0; i < InitialSize; i++)
        {
            var connection = new Connection(Guid.NewGuid(), new MemoryStream());
            connections.TryAdd(connection.Id, connection);
        }
        eventCoordinator = (IEventCoordinator)
            Activator.CreateInstance(Implementation, connections, new DeviceMetrics());
    }

    [Benchmark]
    public void Add_One()
    {
        eventCoordinator.Add(Guid.NewGuid(), new MemoryStream());
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public void Add_N(int amount)
    {
        Parallel.ForEach(
            Enumerable.Range(0, amount),
            (i) =>
            {
                eventCoordinator.Add(guids[i], new MemoryStream());
            }
        );
    }
}
