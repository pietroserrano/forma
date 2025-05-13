using BenchmarkDotNet.Running;
using Forma.Benchmarks;
using System;

Console.WriteLine("Running benchmarks to compare Forma.Mediator and MediatR");

// Uncomment the benchmark you want to run:

// Option 1: Run only the original Forma.Mediator benchmarks
// BenchmarkRunner.Run<RequestMediatorBenchmarks>();

// Option 2: Run the comparison benchmarks between Forma.Mediator and MediatR
BenchmarkRunner.Run<Forma.Benchmarks.MediatRComparisonBenchmarks>();

// Option 3: Run all benchmarks
// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAll();
