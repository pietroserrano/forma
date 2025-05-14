using BenchmarkDotNet.Running;
using Forma.Benchmarks;

// Uncomment the benchmark you want to run:

// Option 1: Run only the original Forma.Mediator benchmarks
// BenchmarkRunner.Run<RequestMediatorBenchmarks>();

// Option 2: Run the comparison benchmarks between Forma.Mediator and MediatR
// BenchmarkRunner.Run<MediatRComparisonBenchmarks>();

// Option 3: Run the comparison benchmarks between Forma.Decorator and Scrutor
BenchmarkRunner.Run<DecoratorComparisonBenchmarks>();

// Option 4: Run all benchmarks
// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();
