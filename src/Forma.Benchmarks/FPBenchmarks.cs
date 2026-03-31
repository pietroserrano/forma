using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Forma.Core.FP;

namespace Forma.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FPBenchmarks
{
    private Result<int, string> _successResult = null!;
    private Result<int, string> _failureResult = null!;
    private Option<int> _someOption = null!;
    private Option<int> _noneOption = null!;

    [GlobalSetup]
    public void Setup()
    {
        _successResult = Result<int, string>.Success(42);
        _failureResult = Result<int, string>.Failure("Error");
        _someOption = Option<int>.Some(42);
        _noneOption = Option<int>.None();
    }

    #region Result Benchmarks

    [Benchmark]
    public Result<int, string> Result_CreateSuccess()
    {
        return Result<int, string>.Success(42);
    }

    [Benchmark]
    public Result<int, string> Result_CreateFailure()
    {
        return Result<int, string>.Failure("Error");
    }

    [Benchmark]
    public Result<int, string> Result_Then_Success()
    {
        return _successResult.Then(x => Result<int, string>.Success(x * 2));
    }

    [Benchmark]
    public Result<int, string> Result_Then_Failure()
    {
        return _failureResult.Then(x => Result<int, string>.Success(x * 2));
    }

    [Benchmark]
    public Result<int, string> Result_ChainedThen_Success()
    {
        return _successResult
            .Then(x => Result<int, string>.Success(x * 2))
            .Then(x => Result<int, string>.Success(x + 10))
            .Then(x => Result<int, string>.Success(x - 5));
    }

    [Benchmark]
    public Result<int, string> Result_ChainedThen_Failure()
    {
        return _failureResult
            .Then(x => Result<int, string>.Success(x * 2))
            .Then(x => Result<int, string>.Success(x + 10))
            .Then(x => Result<int, string>.Success(x - 5));
    }

    [Benchmark]
    public Result<int, string> Result_Do_Success()
    {
        var count = 0;
        return _successResult.Do(_ => count++);
    }

    [Benchmark]
    public Result<int, string> Result_Do_Failure()
    {
        var count = 0;
        return _failureResult.Do(_ => count++);
    }

    [Benchmark]
    public Result<int, string> Result_Validate_Success_Valid()
    {
        return _successResult.Validate(x => x > 10, () => "Too small");
    }

    [Benchmark]
    public Result<int, string> Result_Validate_Success_Invalid()
    {
        return _successResult.Validate(x => x > 100, () => "Too small");
    }

    [Benchmark]
    public string Result_Match_Success()
    {
        return _successResult.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e}"
        );
    }

    [Benchmark]
    public string Result_Match_Failure()
    {
        return _failureResult.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e}"
        );
    }

    [Benchmark]
    public Result<int, string> Result_OnSuccess()
    {
        var count = 0;
        return _successResult.OnSuccess(_ => count++);
    }

    [Benchmark]
    public Result<int, string> Result_OnError()
    {
        var count = 0;
        return _failureResult.OnError(_ => count++);
    }

    [Benchmark]
    public string Result_ComplexPipeline_Success()
    {
        return ParseInt("42")
            .Then(x => Result<int, string>.Success(x * 2))
            .Validate(x => x > 10, () => "Too small")
            .Then(x => Result<string, string>.Success($"Result: {x}"))
            .Match(
                onSuccess: s => s,
                onFailure: e => $"Error: {e}"
            );
    }

    [Benchmark]
    public string Result_ComplexPipeline_Failure()
    {
        return ParseInt("invalid")
            .Then(x => Result<int, string>.Success(x * 2))
            .Validate(x => x > 10, () => "Too small")
            .Then(x => Result<string, string>.Success($"Result: {x}"))
            .Match(
                onSuccess: s => s,
                onFailure: e => $"Error: {e}"
            );
    }

    #endregion

    #region Option Benchmarks

    [Benchmark]
    public Option<int> Option_CreateSome()
    {
        return Option<int>.Some(42);
    }

    [Benchmark]
    public Option<int> Option_CreateNone()
    {
        return Option<int>.None();
    }

    [Benchmark]
    public Option<int> Option_From_NonNull()
    {
        return Option<int>.From(42);
    }

    [Benchmark]
    public Option<int?> Option_From_Null()
    {
        return Option<int?>.From(null);
    }

    [Benchmark]
    public Option<int> Option_Then_Some()
    {
        return _someOption.Then(x => Option<int>.Some(x * 2));
    }

    [Benchmark]
    public Option<int> Option_Then_None()
    {
        return _noneOption.Then(x => Option<int>.Some(x * 2));
    }

    [Benchmark]
    public Option<int> Option_ChainedThen_Some()
    {
        return _someOption
            .Then(x => Option<int>.Some(x * 2))
            .Then(x => Option<int>.Some(x + 10))
            .Then(x => Option<int>.Some(x - 5));
    }

    [Benchmark]
    public Option<int> Option_ChainedThen_None()
    {
        return _noneOption
            .Then(x => Option<int>.Some(x * 2))
            .Then(x => Option<int>.Some(x + 10))
            .Then(x => Option<int>.Some(x - 5));
    }

    [Benchmark]
    public Option<int> Option_Do_Some()
    {
        var count = 0;
        return _someOption.Do(_ => count++);
    }

    [Benchmark]
    public Option<int> Option_Do_None()
    {
        var count = 0;
        return _noneOption.Do(_ => count++);
    }

    [Benchmark]
    public Option<int> Option_Validate_Some_Valid()
    {
        return _someOption.Validate(x => x > 10);
    }

    [Benchmark]
    public Option<int> Option_Validate_Some_Invalid()
    {
        return _someOption.Validate(x => x > 100);
    }

    [Benchmark]
    public string Option_Match_Some()
    {
        return _someOption.Match(
            some: x => $"Value: {x}",
            none: () => "No value"
        );
    }

    [Benchmark]
    public string Option_Match_None()
    {
        return _noneOption.Match(
            some: x => $"Value: {x}",
            none: () => "No value"
        );
    }

    [Benchmark]
    public string Option_ComplexPipeline_Some()
    {
        return ParseIntOption("42")
            .Then(x => Option<int>.Some(x * 2))
            .Validate(x => x > 10)
            .Then(x => Option<string>.Some($"Result: {x}"))
            .Match(
                some: s => s,
                none: () => "No value"
            );
    }

    [Benchmark]
    public string Option_ComplexPipeline_None()
    {
        return ParseIntOption("invalid")
            .Then(x => Option<int>.Some(x * 2))
            .Validate(x => x > 10)
            .Then(x => Option<string>.Some($"Result: {x}"))
            .Match(
                some: s => s,
                none: () => "No value"
            );
    }

    #endregion

    #region Async Benchmarks

    [Benchmark]
    public async Task<Result<int, string>> Result_ThenAsync_Success()
    {
        return await _successResult.ThenAsync(async x =>
        {
            await Task.Yield();
            return Result<int, string>.Success(x * 2);
        });
    }

    [Benchmark]
    public async Task<Result<int, string>> Result_ThenAsync_Failure()
    {
        return await _failureResult.ThenAsync(async x =>
        {
            await Task.Yield();
            return Result<int, string>.Success(x * 2);
        });
    }

    [Benchmark]
    public async Task<Option<int>> Option_ThenAsync_Some()
    {
        return await _someOption.ThenAsync(async x =>
        {
            await Task.Yield();
            return Option<int>.Some(x * 2);
        });
    }

    [Benchmark]
    public async Task<Option<int>> Option_ThenAsync_None()
    {
        return await _noneOption.ThenAsync(async x =>
        {
            await Task.Yield();
            return Option<int>.Some(x * 2);
        });
    }

    [Benchmark]
    public async Task<Result<int, string>> Result_DoAsync_Success()
    {
        return await Task.FromResult(_successResult).DoAsync(async _ =>
        {
            await Task.Yield();
        });
    }

    [Benchmark]
    public async Task<Option<int>> Option_DoAsync_Some()
    {
        return await _someOption.DoAsync(async _ =>
        {
            await Task.Yield();
        });
    }

    [Benchmark]
    public async Task<Result<int, string>> Result_ValidateAsync_Success()
    {
        return await Task.FromResult(_successResult).ValidateAsync(
            async x =>
            {
                await Task.Yield();
                return x > 10;
            },
            () => "Too small"
        );
    }

    [Benchmark]
    public async Task<Option<int>> Option_ValidateAsync_Some()
    {
        return await _someOption.ValidateAsync(async x =>
        {
            await Task.Yield();
            return x > 10;
        });
    }

    #endregion

    #region Helper Methods

    private static Result<int, string> ParseInt(string s)
    {
        if (int.TryParse(s, out int val))
            return Result<int, string>.Success(val);
        return Result<int, string>.Failure("Invalid number format");
    }

    private static Option<int> ParseIntOption(string s)
    {
        if (int.TryParse(s, out int val))
            return Option<int>.Some(val);
        return Option<int>.None();
    }

    #endregion
}
