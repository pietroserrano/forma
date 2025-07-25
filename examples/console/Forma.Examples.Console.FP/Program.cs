using Forma.FP.Abstractions;

var pipeline = Step1("5")
    .Then(Step2)
    .Then(Step3)
    .OnSuccess(result => Console.WriteLine(result))
    .OnError(error => Console.WriteLine($"Errore: {error.Message}"));


var optionPipeline = OptionStep1("5")
    .Then(OptionStep2)
    .Then(OptionStep3)
    .Match(
        some: result => result,
        none: () => "Too small"
    );

Console.WriteLine($"Option Result: {optionPipeline}");

// Funzioni che restituiscono Option
static Option<int> OptionStep1(string s)
{
    if (int.TryParse(s, out int val)) return Option<int>.Some(val);
    return Option<int>.None();
}

static Option<int> OptionStep2(int x) => Option<int>.Some(x * 2);

static Option<string> OptionStep3(int x)
{
    if (x > 10) return Option<string>.Some($"OK: {x}");
    return Option<string>.None();
}



static Result<int, Exception> Step1(string s)
{
    if (int.TryParse(s, out int val)) return Result<int, Exception>.Success(val);
    return Result<int, Exception>.Failure(new FormatException("Not a number"));
}

static Result<int, Exception> Step2(int x) => Result<int, Exception>.Success(x * 2);

static Result<string, Exception> Step3(int x)
{
    if (x > 10) return Result<string, Exception>.Success($"OK: {x}");
    return Result<string, Exception>.Failure(new Exception("Too small"));
}
