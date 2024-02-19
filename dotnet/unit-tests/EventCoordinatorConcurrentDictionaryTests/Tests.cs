using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using Shouldly;
using SseHandler;
using SseHandler.EventCoordinators;

namespace unit_tests;

[TestClass]
public class EventCoordinatorConcurrentDictionaryTests
{
    [TestMethod]
    public void Should_AddConnection()
    {
        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary();

        var result = eventCoordinator.Add(Guid.NewGuid().ToString(), new MemoryStream());

        result.IsSuccessful.ShouldBeTrue();
    }

    [TestMethod]
    public void Should_Not_AddConnection_KeyExists()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnection = new Connection("test", new MemoryStream());
        connections.TryAdd(testConnection.Id, testConnection);

        var otherConnection = new Connection("test", new MemoryStream());

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);

        var result = eventCoordinator.Add(otherConnection.Id, otherConnection.Stream);

        result.IsSuccessful.ShouldBeFalse();
        result.Error.ShouldBe(EventCoordinatorError.DuplicateKey);
    }

    [TestMethod]
    public void Should_RemoveConnection()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnection = new Connection("test", new MemoryStream());
        connections.TryAdd(testConnection.Id, testConnection);

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);

        var result = eventCoordinator.Remove(testConnection.Id);

        result.IsSuccessful.ShouldBeTrue();
        testConnection.CancellationTokenSource.IsCancellationRequested.ShouldBeTrue();
        connections.Count.ShouldBe(0);
    }

    [TestMethod]
    public void Should_Not_RemoveConnection()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnection = new Connection("test", new MemoryStream());
        connections.TryAdd(testConnection.Id, testConnection);

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);

        var result = eventCoordinator.Remove("other");

        result.IsSuccessful.ShouldBeFalse();
        connections.Count.ShouldBe(1);
    }

    [TestMethod]
    public void Should_RemoveAll()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnections = new List<Connection>()
        {
            new Connection("test", new MemoryStream()),
            new Connection("other", new MemoryStream())
        };

        foreach (var connection in testConnections)
        {
            connections.TryAdd(connection.Id, connection);
        }

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);

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

    [TestMethod]
    public async Task Should_WriteToStream()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnection = new Connection("test", new MemoryStream());
        connections.TryAdd(testConnection.Id, testConnection);

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);
        var message = new TestMessage { Message = "test" };
        var result = await eventCoordinator.SendMessage(testConnection.Id, message);

        result.IsSuccessful.ShouldBeTrue();
        testConnection.Stream.Seek(0, SeekOrigin.Begin);
        var outputStream = new MemoryStream();
        testConnection.Stream.CopyTo(outputStream);

        var deserializedMessage = JsonConvert.DeserializeObject<TestMessage>(
            Encoding.UTF8.GetString(outputStream.ToArray())
        );
        deserializedMessage!.Message.ShouldBe(message.Message);
    }

    [TestMethod]
    public async Task Should_Not_WriteToStream()
    {
        var connections = new ConcurrentDictionary<string, Connection>();
        var testConnection = new Connection("test", new MemoryStream());
        connections.TryAdd(testConnection.Id, testConnection);

        IEventCoordinator eventCoordinator = new EventCoordinatorConcurrentDictionary(connections);
        var message = new TestMessage { Message = "test" };
        var result = await eventCoordinator.SendMessage("other", message);

        result.IsSuccessful.ShouldBeFalse();
        result.Error.ShouldBe(EventCoordinatorError.KeyNotFound);
    }
}
