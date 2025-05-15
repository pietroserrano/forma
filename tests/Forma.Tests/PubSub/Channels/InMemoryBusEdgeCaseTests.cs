using System.Threading;
using System.Threading;
using Forma.Core.PubSub.Abstractions;
using Forma.PubSub.InMemory.Channels;
using Forma.PubSub.InMemory.ChannelPubSub.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forma.Tests.PubSub.Channels;

/// <summary>
/// Tests for edge cases of InMemoryBus
/// </summary>
public class InMemoryBusEdgeCaseTests
{    /// <summary>
    /// Test event for edge cases
    /// </summary>
    private class TestEvent : IEvent
    {
        public string? Message { get; set; }
    }
      /// <summary>
    /// Another test event for type verification
    /// </summary>
    private class AnotherTestEvent : IEvent
    {
        public string? Data { get; set; }
    }    /// <summary>
    /// Consumer that tracks when it's called
    /// </summary>
    private class TestConsumer : IConsume<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();
        public TaskCompletionSource<bool> ProcessingComplete { get; set; } = new TaskCompletionSource<bool>();
        public ManualResetEventSlim SecondEventProcessed { get; } = new ManualResetEventSlim(false);

        public Task ConsumeAsync(TestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            ProcessingComplete.TrySetResult(true);
              // If we've received at least 2 events, it means the second one has been processed
            if (ReceivedEvents.Count >= 2)
            {
                SecondEventProcessed.Set();
            }
            
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_WithNullEventData_ConsumerReceivesNullData()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;

        // Registra un consumer di test
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus
        services.AddFormaPubSubInMemory(assembly);
        services.AddSingleton<ILogger<InMemoryBusImpl>>(new NullLogger<InMemoryBusImpl>());
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus manualmente
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act - Pubblica un evento con dati nulli
        var testEvent = new TestEvent { Message = null };
        await bus.PublishAsync(testEvent);

        // Attendi che l'elaborazione sia completata
        await Task.WhenAny(consumer.ProcessingComplete.Task, Task.Delay(500));

        // Assert
        Assert.Single(consumer.ReceivedEvents);
        Assert.Null(consumer.ReceivedEvents[0].Message);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task PublishAsync_NoConsumersForEventType_EventIsIgnored()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;

        // Registra un consumer solo per TestEvent
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus
        services.AddFormaPubSubInMemory(assembly);
        services.AddSingleton<ILogger<InMemoryBusImpl>>(new NullLogger<InMemoryBusImpl>());
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus manualmente
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act - Pubblica un evento di un tipo per cui non ci sono consumer
        var anotherEvent = new AnotherTestEvent { Data = "No consumers" };
        await bus.PublishAsync(anotherEvent);
        
        // Pubblica anche un evento normale che dovrebbe essere ricevuto
        var normalEvent = new TestEvent { Message = "Should be received" };
        await bus.PublishAsync(normalEvent);

        // Attendi che l'elaborazione sia completata
        await Task.WhenAny(consumer.ProcessingComplete.Task, Task.Delay(500));

        // Assert
        // Il consumer dovrebbe ricevere solo l'evento del tipo corretto
        Assert.Single(consumer.ReceivedEvents);
        Assert.Equal("Should be received", consumer.ReceivedEvents[0].Message);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task MultipleStartStopCycles_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;

        // Registra un consumer di test
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus
        services.AddFormaPubSubInMemory(assembly);
        services.AddSingleton<ILogger<InMemoryBusImpl>>(new NullLogger<InMemoryBusImpl>());
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();
        var busImpl = (InMemoryBusImpl)bus;

        // Act & Assert - Primo ciclo
        await busImpl.StartAsync(CancellationToken.None);
        
        var event1 = new TestEvent { Message = "First cycle" };
        await bus.PublishAsync(event1);        // Attendi che l'elaborazione sia completata
        var tcs1 = consumer.ProcessingComplete;
        await Task.WhenAny(tcs1.Task, Task.Delay(3000));
        
        // Verifica eventi ricevuti con un po' di tolleranza e attesa
        for (int i = 0; i < 5; i++)
        {
            if (consumer.ReceivedEvents.Count >= 1)
                break;
            await Task.Delay(300);
        }
        
        // Il primo evento dovrebbe essere ricevuto
        Assert.Single(consumer.ReceivedEvents);
        Assert.Equal("First cycle", consumer.ReceivedEvents[0].Message);
        
        // Ferma il bus
        await busImpl.StopAsync(CancellationToken.None);
        
        // Crea un nuovo TCS per il prossimo ciclo
        consumer.ProcessingComplete = new TaskCompletionSource<bool>();
        
        // Act & Assert - Secondo ciclo
        await busImpl.StartAsync(CancellationToken.None);
        
        var event2 = new TestEvent { Message = "Second cycle" };
        await bus.PublishAsync(event2);        // Attendi che il secondo evento sia elaborato
        var secondProcessed = consumer.SecondEventProcessed.Wait(TimeSpan.FromSeconds(10));
          // Se il secondo evento non è stato elaborato entro il timeout, attendiamo ancora un po'
        if (!secondProcessed) 
        {
            await Task.Delay(2000);
        }
        
        // Il secondo evento dovrebbe essere ricevuto
        Assert.Equal(2, consumer.ReceivedEvents.Count);
        Assert.Equal("Second cycle", consumer.ReceivedEvents[1].Message);
        
        // Ferma il bus
        await busImpl.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_AfterChannelIsAlreadyCreated_StillWorks()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(TestConsumer).Assembly;

        // Registra un consumer di test
        var consumer = new TestConsumer();
        services.AddSingleton<IConsume<TestEvent>>(consumer);

        // Aggiungi il bus
        services.AddFormaPubSubInMemory(assembly);
        services.AddSingleton<ILogger<InMemoryBusImpl>>(new NullLogger<InMemoryBusImpl>());
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();

        // Avvia il bus manualmente
        if (bus is InMemoryBusImpl busImpl)
        {
            await busImpl.StartAsync(CancellationToken.None);
        }

        // Act - Prima pubblicazione, crea il canale
        var firstEvent = new TestEvent { Message = "First event" };
        await bus.PublishAsync(firstEvent);
        
        // Attendi che l'elaborazione della prima pubblicazione sia completata
        var tcs1 = consumer.ProcessingComplete;
        await Task.WhenAny(tcs1.Task, Task.Delay(500));
        
        // Crea un nuovo TCS per il prossimo evento
        consumer.ProcessingComplete = new TaskCompletionSource<bool>();

        // Seconda pubblicazione, usa il canale esistente
        var secondEvent = new TestEvent { Message = "Second event" };
        await bus.PublishAsync(secondEvent);
        
        // Attendi che l'elaborazione della seconda pubblicazione sia completata
        var tcs2 = consumer.ProcessingComplete;
        await Task.WhenAny(tcs2.Task, Task.Delay(500));

        // Assert
        Assert.Equal(2, consumer.ReceivedEvents.Count);
        Assert.Equal("First event", consumer.ReceivedEvents[0].Message);
        Assert.Equal("Second event", consumer.ReceivedEvents[1].Message);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }
}
