using System.Threading.Channels;
using Forma.Core.PubSub.Abstractions;
using Forma.PubSub.InMemory.Channels;
using Forma.PubSub.InMemory.ChannelPubSub.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forma.Tests.PubSub.Channels;

/// <summary>
/// Success tests for InMemoryBus
/// </summary>
public class InMemoryBusSuccessTests
{    /// <summary>
    /// Test event
    /// </summary>
    private class TestEvent : IEvent
    {
        public string Message { get; set; } = "Test";
        public int Value { get; set; } = 42;
    }    /// <summary>
    /// Another event type to test multiple consumers
    /// </summary>
    private class AnotherTestEvent : IEvent
    {
        public string Data { get; set; } = "Another";
    }    /// <summary>
    /// Test consumer that records received events
    /// </summary>
    private class TestConsumer : IConsume<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();

        public Task ConsumeAsync(TestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            return Task.CompletedTask;
        }
    }    /// <summary>
    /// Test consumer for the second event type
    /// </summary>
    private class AnotherTestConsumer : IConsume<AnotherTestEvent>
    {
        public List<AnotherTestEvent> ReceivedEvents { get; } = new();

        public Task ConsumeAsync(AnotherTestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task AddFormaPubSubInMemory_RegistersComponents()
    {        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;        // Add logging services
        services.AddLogging(configure => configure.AddConsole());
        
        // Act
        services.AddFormaPubSubInMemory(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        var bus = provider.GetService<IBus>();
        Assert.NotNull(bus);
        Assert.IsType<InMemoryBusImpl>(bus);
    }

    [Fact]    public async Task PublishAsync_WithConsumer_ConsumerReceivesEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;        // Register a test consumer
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus e il logging
        services.AddLogging(configure => configure.AddConsole());
        services.AddFormaPubSubInMemory(assembly);
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();        // Start the bus manually (in a real app it would be managed by the host)
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }        // Act
        var testEvent = new TestEvent { Message = "Hello World", Value = 123 };
        await bus.PublishAsync(testEvent);

        // Wait a short period to allow for asynchronous processing
        await Task.Delay(100);

        // Assert
        Assert.Single(consumer.ReceivedEvents);
        var receivedEvent = consumer.ReceivedEvents.First();
        Assert.Equal("Hello World", receivedEvent.Message);
        Assert.Equal(123, receivedEvent.Value);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PublishAsync_MultipleEvents_EventsAreProcessedInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;        // Registra un consumer di test
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus e il logging
        services.AddLogging(configure => configure.AddConsole());
        services.AddFormaPubSubInMemory(assembly);
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus manualmente
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act - Pubblica più eventi
        var events = new List<TestEvent>
        {
            new TestEvent { Message = "First", Value = 1 },
            new TestEvent { Message = "Second", Value = 2 },
            new TestEvent { Message = "Third", Value = 3 }
        };

        foreach (var evt in events)
        {
            await bus.PublishAsync(evt);
        }

        // Attendi un breve periodo per consentire l'elaborazione asincrona
        await Task.Delay(100);

        // Assert
        Assert.Equal(3, consumer.ReceivedEvents.Count);
        Assert.Equal("First", consumer.ReceivedEvents[0].Message);
        Assert.Equal(1, consumer.ReceivedEvents[0].Value);
        Assert.Equal("Second", consumer.ReceivedEvents[1].Message);
        Assert.Equal(2, consumer.ReceivedEvents[1].Value);
        Assert.Equal("Third", consumer.ReceivedEvents[2].Message);
        Assert.Equal(3, consumer.ReceivedEvents[2].Value);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PublishAsync_MultipleConsumersForSameEvent_AllConsumersReceiveEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;        // Registra due consumer per lo stesso tipo di evento
        var consumer1 = new TestConsumer();
        var consumer2 = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer1);
        services.AddSingleton<IConsume<TestEvent>>(consumer2);
        
        // Aggiungi il bus e il logging
        services.AddLogging(configure => configure.AddConsole());
        services.AddFormaPubSubInMemory(assembly);
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act
        var testEvent = new TestEvent { Message = "Broadcast", Value = 999 };
        await bus.PublishAsync(testEvent);

        // Attendi un breve periodo per l'elaborazione asincrona
        await Task.Delay(100);

        // Assert
        Assert.Single(consumer1.ReceivedEvents);
        Assert.Equal("Broadcast", consumer1.ReceivedEvents[0].Message);
        Assert.Equal(999, consumer1.ReceivedEvents[0].Value);
        
        Assert.Single(consumer2.ReceivedEvents);
        Assert.Equal("Broadcast", consumer2.ReceivedEvents[0].Message);
        Assert.Equal(999, consumer2.ReceivedEvents[0].Value);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PublishAsync_DifferentEventTypes_ConsumersReceiveOnlyTheirEvents()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;        // Registra consumer per tipi di eventi diversi
        var testConsumer = new TestConsumer();
        var anotherConsumer = new AnotherTestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(testConsumer);
        services.AddSingleton<IConsume<AnotherTestEvent>>(anotherConsumer);
        
        // Aggiungi il bus e il logging
        services.AddLogging(configure => configure.AddConsole());
        services.AddFormaPubSubInMemory(assembly);
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act
        var testEvent = new TestEvent { Message = "Type 1", Value = 1 };
        var anotherEvent = new AnotherTestEvent { Data = "Type 2" };
        
        await bus.PublishAsync(testEvent);
        await bus.PublishAsync(anotherEvent);

        // Attendi un breve periodo per l'elaborazione asincrona
        await Task.Delay(100);

        // Assert
        Assert.Single(testConsumer.ReceivedEvents);
        Assert.Equal("Type 1", testConsumer.ReceivedEvents[0].Message);
        
        Assert.Single(anotherConsumer.ReceivedEvents);
        Assert.Equal("Type 2", anotherConsumer.ReceivedEvents[0].Data);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }
}
