using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using Shouldly;
using SseHandler;
using SseHandler.EventCoordinators;

namespace unit_tests;

[TestClass]
public class EventCoordinatorsTests
{
    public static IEnumerable<object[]> GetObjects()
    {
        var testConnection = new Connection(Guid.NewGuid().ToString(), new MemoryStream());
        var otherConnection = new Connection(Guid.NewGuid().ToString(), new MemoryStream());

        var concurrentDictionary = new ConcurrentDictionary<string, Connection>();
        concurrentDictionary.TryAdd(testConnection.Id, testConnection);
        concurrentDictionary.TryAdd(otherConnection.Id, otherConnection);

        var dictionary = new Dictionary<string, Connection>();
        dictionary[testConnection.Id] = testConnection;
        dictionary[otherConnection.Id] = otherConnection;

        yield return new object[]
        {
            new EventCoordinatorConcurrentDictionary(concurrentDictionary),
            concurrentDictionary
        };
        yield return new object[] { new EventCoordinatorReaderWriterLock(dictionary), dictionary };
        yield return new object[] { new EventCoordinatorMutex(dictionary), dictionary };
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public void Should_AddConnection(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var result = eventCoordinator.Add(Guid.NewGuid().ToString(), new MemoryStream());

        result.IsSuccessful.ShouldBeTrue();
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public void Should_Not_AddConnection_KeyExists(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var existingConnection = connections.First().Value;

        var result = eventCoordinator.Add(existingConnection.Id, existingConnection.Stream);

        result.IsSuccessful.ShouldBeFalse();
        result.Error.ShouldBe(EventCoordinatorError.DuplicateKey);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public void Should_RemoveConnection(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var beginningCount = connections.Count;
        var firstConnection = connections.First().Value;
        var result = eventCoordinator.Remove(firstConnection.Id);

        result.IsSuccessful.ShouldBeTrue();
        firstConnection.CancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
        connections.Count.ShouldBe(beginningCount - 1);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public void Should_Not_RemoveConnection(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var beginningCount = connections.Count;
        var result = eventCoordinator.Remove(Guid.NewGuid().ToString());

        result.IsSuccessful.ShouldBeFalse();
        connections.Count.ShouldBe(beginningCount);
        result.Error.ShouldBe(EventCoordinatorError.KeyNotFound);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public void Should_RemoveAll(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        eventCoordinator.RemoveAll();

        connections.Count.ShouldBe(0);
        foreach (var connection in connections.Values)
        {
            connection.CancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
        }
    }

    private class TestMessage
    {
        public required string Message { get; set; }
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public async Task Should_WriteToStream(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var firstConnection = connections.First().Value;
        var message = new TestMessage { Message = "test" };
        var result = await eventCoordinator.SendMessage(firstConnection.Id, message);

        result.IsSuccessful.ShouldBeTrue();
        firstConnection.Stream.Seek(0, SeekOrigin.Begin);
        var outputStream = new MemoryStream();
        firstConnection.Stream.CopyTo(outputStream);

        var decodedString = Encoding.UTF8.GetString(outputStream.ToArray());
        decodedString.ShouldStartWith("data: ");
        decodedString = decodedString.Split("data: ")[1];
        var deserializedMessage = JsonConvert.DeserializeObject<TestMessage>(decodedString);
        deserializedMessage!.Message.ShouldBe(message.Message);
    }

    [DataTestMethod]
    [DynamicData(nameof(GetObjects), DynamicDataSourceType.Method)]
    public async Task Should_Not_WriteToStream(
        IEventCoordinator eventCoordinator,
        IDictionary<string, Connection> connections
    )
    {
        var message = new TestMessage { Message = "test" };
        var result = await eventCoordinator.SendMessage(Guid.NewGuid().ToString(), message);

        result.IsSuccessful.ShouldBeFalse();
        result.Error.ShouldBe(EventCoordinatorError.KeyNotFound);
    }
}
