using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;

namespace Forma.Benchmarks
{
    /// <summary>
    /// Benchmark di confronto tra Forma.Decorator e Scrutor
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class DecoratorComparisonBenchmarks
    {
        private ServiceProvider _formaBenchmarkProvider = null!;
        private ServiceProvider _scrutorBenchmarkProvider = null!;
        private const int NumServices = 1000;

        [GlobalSetup]
        public void Setup()
        {
            // Configurazione per Forma.Decorator
            var formaServices = new ServiceCollection();
            
            // Registrazione dei servizi di base
            for (int i = 0; i < NumServices; i++)
            {
                formaServices.AddTransient<ITestService, TestServiceImplementation>();
            }

            // Decora con Forma.Decorator
            Decorator.Extensions.ServiceCollectionExtensions.Decorate<ITestService, TestServiceDecorator>(formaServices);
            _formaBenchmarkProvider = formaServices.BuildServiceProvider();

            // Configurazione per Scrutor
            var scrutorServices = new ServiceCollection();
            
            // Registrazione dei servizi di base
            for (int i = 0; i < NumServices; i++)
            {
                scrutorServices.AddTransient<ITestService, TestServiceImplementation>();
            }
            
            // Decora con Scrutor
            ServiceCollectionExtensions.Decorate<ITestService, TestServiceDecorator>(scrutorServices);
            _scrutorBenchmarkProvider = scrutorServices.BuildServiceProvider();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _formaBenchmarkProvider.Dispose();
            _scrutorBenchmarkProvider.Dispose();
        }

        [Benchmark]
        [BenchmarkCategory("Registration")]
        public void FormaDecoratorRegistration()
        {
            var services = new ServiceCollection();
            
            // Registrazione dei servizi di base
            for (int i = 0; i < NumServices; i++)
            {
                services.AddTransient<ITestService, TestServiceImplementation>();
            }

            // Decora con Forma.Decorator
            Decorator.Extensions.ServiceCollectionExtensions.Decorate<ITestService, TestServiceDecorator>(services);
        }

        [Benchmark]
        [BenchmarkCategory("Registration")]
        public void ScrutorDecoratorRegistration()
        {
            var services = new ServiceCollection();
            
            // Registrazione dei servizi di base
            for (int i = 0; i < NumServices; i++)
            {
                services.AddTransient<ITestService, TestServiceImplementation>();
            }

            // Decora con Scrutor
            ServiceCollectionExtensions.Decorate<ITestService, TestServiceDecorator>(services);
        }

        [Benchmark]
        [BenchmarkCategory("Resolution")]
        public void FormaDecoratorResolution()
        {
            for (int i = 0; i < 100; i++)
            {
                var service = _formaBenchmarkProvider.GetService<ITestService>();
                service!.Execute();
            }
        }

        [Benchmark]
        [BenchmarkCategory("Resolution")]
        public void ScrutorDecoratorResolution()
        {
            for (int i = 0; i < 100; i++)
            {
                var service = _scrutorBenchmarkProvider.GetService<ITestService>();
                service!.Execute();
            }
        }

        // Interfaccia e implementazioni per il test
        public interface ITestService
        {
            void Execute();
        }

        public class TestServiceImplementation : ITestService
        {
            public void Execute() { /* Do nothing for benchmark */ }
        }

        public class TestServiceDecorator : ITestService
        {
            private readonly ITestService _inner;

            public TestServiceDecorator(ITestService inner)
            {
                _inner = inner;
            }

            public void Execute()
            {
                // Semplicemente delega all'implementazione interna
                _inner.Execute();
            }
        }
    }
}
