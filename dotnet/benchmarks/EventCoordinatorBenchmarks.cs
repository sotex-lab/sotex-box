using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using SseHandler;
using SseHandler.EventCoordinators;

[MemoryDiagnoser]
public class Benchmarks
{
    // Ammount of active devices
    [Params(100, 10_000, 100_000)]
    public int InitialSize { get; set; }

    // Implementation that is running
    [Params(typeof(EventCoordinatorConcurrentDictionary), typeof(EventCoordinatorMutex), typeof(EventCoordinatorReaderWriterLock))]
    public Type Implementation { get; set; }

    private static Dictionary<Type, IDictionary<string, Connection>> ConnectionsMapping = new Dictionary<Type, IDictionary<string, Connection>>
    {
        [typeof(EventCoordinatorConcurrentDictionary)] = new ConcurrentDictionary<string, Connection>(),
        [typeof(EventCoordinatorMutex)] = new Dictionary<string, Connection>(),
        [typeof(EventCoordinatorReaderWriterLock)] = new Dictionary<string, Connection>()
    };

    private IEventCoordinator eventCoordinator;

    [GlobalSetup]
    public void GlobalSetup()
    {
        IDictionary<string, Connection> connections = ConnectionsMapping[Implementation];
        if (connections.Count > 0)
        {
            connections.Clear();
        }
        for (int i = 0; i < InitialSize; i++)
        {
            var connection = new Connection(i.ToString(), new MemoryStream());
            connections.TryAdd(connection.Id, connection);
        }
        eventCoordinator = (IEventCoordinator)Activator.CreateInstance(Implementation, connections);
    }

    [Benchmark]
    public void Add_One()
    {
        eventCoordinator.Add(InitialSize.ToString(), new MemoryStream());
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public void Add_N(int amount)
    {
        Parallel.ForEach(Enumerable.Range(0, amount), (i) =>
        {
            eventCoordinator.Add((InitialSize + i).ToString(), new MemoryStream());
        });
    }
}
