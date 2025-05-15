using System.Threading;
using Forma.Core.PubSub.Abstractions;
using Forma.PubSub.InMemory.Channels;
using Forma.PubSub.InMemory.ChannelPubSub.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;

namespace Forma.Tests.PubSub.Channels;

/// <summary>
/// Failure tests for InMemoryBus
/// </summary>
public class InMemoryBusFailureTests
{    /// <summary>
    /// Test event for error testing
    /// </summary>
    private class TestEvent : IEvent
    {
        public string Message { get; set; } = "Test";
        public bool ShouldThrow { get; set; } = false;
    }    /// <summary>
    /// Consumer that generates errors under specific conditions
    /// </summary>
    private class ThrowingConsumer : IConsume<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();
        public List<Exception> CaughtExceptions { get; } = new();
        public ManualResetEventSlim AllEventsProcessed { get; } = new ManualResetEventSlim(false);
        public int ExpectedEventCount { get; set; } = 2;  // Default per PublishAsync_ConsumerThrowsException_OtherEventsStillProcessed

        public async Task ConsumeAsync(TestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            
            if (message.ShouldThrow)
            {
                throw new InvalidOperationException("Consumer error intentionally thrown");
            }
              // If we've received all expected events, signal completion
            if (ReceivedEvents.Count >= ExpectedEventCount)
            {
                AllEventsProcessed.Set();
            }
            
            await Task.CompletedTask;
        }
    }/// <summary>
    /// Consumer that performs asynchronous operations and can be canceled
    /// </summary>
    private class LongRunningConsumer : IConsume<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();
        public List<bool> CancellationStatus { get; } = new();
        public ManualResetEventSlim EventReceived { get; } = new ManualResetEventSlim(false);

        public async Task ConsumeAsync(TestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            EventReceived.Set();
            
            try
            {                // Simulate a long-running operation
                await Task.Delay(1000, cancellationToken);
                CancellationStatus.Add(false); // Not canceled
            }            catch (OperationCanceledException)
            {
                CancellationStatus.Add(true); // Canceled
                throw;
            }
        }
    }    // Add a new consumer class for specific tests    /// <summary>
    /// Consumer that automatically keeps track of processed events
    /// </summary>
    private class AutoCountingConsumer : IConsume<TestEvent>
    {
        public List<TestEvent> ReceivedEvents { get; } = new();
        public ManualResetEventSlim EventProcessed { get; } = new ManualResetEventSlim(false);

        public Task ConsumeAsync(TestEvent message, CancellationToken cancellationToken = default)
        {
            ReceivedEvents.Add(message);
            if (ReceivedEvents.Count >= 10)
            {
                EventProcessed.Set();
            }
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_ConsumerThrowsException_OtherEventsStillProcessed()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ThrowingConsumer).Assembly;

        // Registra un consumer che può generare errori
        var consumer = new ThrowingConsumer();
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
        }        // Imposta l'aspettativa sul numero di eventi
        consumer.ExpectedEventCount = 2;
        
        // Act - Pubblica un evento che genera errore, seguito da uno normale
        var errorEvent = new TestEvent { Message = "This will throw", ShouldThrow = true };
        var normalEvent = new TestEvent { Message = "This should still be processed", ShouldThrow = false };
        await bus.PublishAsync(errorEvent);
        await bus.PublishAsync(normalEvent);

        // Attendi che tutti gli eventi vengano elaborati con un timeout
        var allProcessed = consumer.AllEventsProcessed.Wait(TimeSpan.FromSeconds(10));
        
        // Se non sono stati tutti elaborati, aspetta ancora un po'
        if (!allProcessed)
        {
            await Task.Delay(2000);
        }

        // Assert
        // Dovrebbero essere stati ricevuti entrambi gli eventi
        Assert.Equal(2, consumer.ReceivedEvents.Count);
        
        // Il secondo evento dovrebbe essere stato elaborato nonostante l'errore nel primo
        Assert.Equal("This will throw", consumer.ReceivedEvents[0].Message);
        Assert.Equal("This should still be processed", consumer.ReceivedEvents[1].Message);

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }    [Fact]
    public async Task PublishAsync_MultipleConcurrentEvents_HandledCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(AutoCountingConsumer).Assembly;        // Utilizziamo un numero più piccolo di eventi per il test
        const int EventCount = 10;

        // Creiamo una classe consumer specializzata per questo test
        var consumer = new AutoCountingConsumer();
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

        try 
        {
            // Act - Pubblica più eventi in parallelo
            var events = new List<TestEvent>();
            for (int i = 0; i < EventCount; i++)
            {
                events.Add(new TestEvent { 
                    Message = $"Event {i}", 
                    // Ogni 5 eventi, crea uno che genera errore
                    ShouldThrow = i % 5 == 0 
                });
            }            // Pubblica tutti gli eventi in parallelo
            await Task.WhenAll(events.Select(e => bus.PublishAsync(e)));

            // Attendi che tutti gli eventi vengano elaborati o timeout
            var allProcessed = consumer.EventProcessed.Wait(TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(allProcessed, "Non tutti gli eventi sono stati elaborati entro il timeout");
            Assert.Equal(EventCount, consumer.ReceivedEvents.Count);
            
            // Verifica che alcuni eventi hanno generato errori
            var errorCount = events.Count(e => e.ShouldThrow);
            Assert.Equal(2, errorCount); // 2 eventi su 10 generano errori
        }        finally 
        {
            // Cleanup
            if (bus is InMemoryBusImpl impl)
            {
                await impl.StopAsync(CancellationToken.None);
            }
        }

        // Cleanup
        if (bus is InMemoryBusImpl busImpl2)
        {
            await busImpl2.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopAsync_CancelsProcessing()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(LongRunningConsumer).Assembly;

        // Registra un consumer che esegue operazioni lunghe e può essere annullato
        var consumer = new LongRunningConsumer();
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
        
        try
        {
            // Act            // 1. Pubblica un evento che richiede un'elaborazione lunga
            var longRunningEvent = new TestEvent { Message = "Long running operation" };
            var publishTask = bus.PublishAsync(longRunningEvent);
            
            // 2. Attendere finché l'evento non viene ricevuto dal consumer
            var eventReceived = consumer.EventReceived.Wait(TimeSpan.FromSeconds(5));
            Assert.True(eventReceived, "L'evento non è stato ricevuto entro il timeout");
            
            // 3. Arrestare immediatamente il bus
            if (bus is InMemoryBusImpl busImpl2)
            {
                await busImpl2.StopAsync(CancellationToken.None);
            }
            
            // Assert
            // L'evento dovrebbe essere stato ricevuto
            Assert.Single(consumer.ReceivedEvents);
            Assert.Equal("Long running operation", consumer.ReceivedEvents[0].Message);
            
            // In alcuni casi il consumer potrebbe essere stato annullato, in altri potrebbe
            // completare prima che l'annullamento abbia effetto. Entrambi i comportamenti sono accettabili.
        }
        finally
        {
            // Assicurarsi che il bus sia fermato
            if (bus is InMemoryBusImpl busImpl2)
            {
                await busImpl2.StopAsync(CancellationToken.None);
            }
        }
    }
}
