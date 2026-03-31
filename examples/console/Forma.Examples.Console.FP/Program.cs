using Forma.Core.FP;

Console.WriteLine("=== Forma.Core.FP Examples ===\n");

// ============================================
// Example 1: Basic Result Pipeline
// ============================================
Console.WriteLine("1. Basic Result Pipeline");
Console.WriteLine("   Processing: \"42\"");

var successPipeline = ParseInt("42")
    .Then(MultiplyByTwo)
    .Then(ConvertToMessage)
    .OnSuccess(result => Console.WriteLine($"   ✓ Success: {result}"))
    .OnError(error => Console.WriteLine($"   ✗ Error: {error.Message}"));

Console.WriteLine("   Processing: \"invalid\"");
var failurePipeline = ParseInt("invalid")
    .Then(MultiplyByTwo)
    .Then(ConvertToMessage)
    .OnSuccess(result => Console.WriteLine($"   ✓ Success: {result}"))
    .OnError(error => Console.WriteLine($"   ✗ Error: {error.Message}"));

Console.WriteLine();

// ============================================
// Example 2: Option Pipeline with Validation
// ============================================
Console.WriteLine("2. Option Pipeline with Validation");
Console.WriteLine("   Processing: \"25\"");

var validOption = ParseIntOption("25")
    .Then(x => Option<int>.Some(x * 2))
    .Validate(x => x > 10)
    .Then(x => Option<string>.Some($"Valid value: {x}"))
    .Match(
        some: result => $"   ✓ {result}",
        none: () => "   ✗ Value too small or invalid"
    );
Console.WriteLine(validOption);

Console.WriteLine("   Processing: \"3\"");
var invalidOption = ParseIntOption("3")
    .Then(x => Option<int>.Some(x * 2))
    .Validate(x => x > 10)
    .Then(x => Option<string>.Some($"Valid value: {x}"))
    .Match(
        some: result => $"   ✓ {result}",
        none: () => "   ✗ Value too small or invalid"
    );
Console.WriteLine(invalidOption);

Console.WriteLine();

// ============================================
// Example 3: Form Validation
// ============================================
Console.WriteLine("3. Form Validation");

var validUser = ValidateUser("john_doe", "john@example.com", "password123")
    .Match(
        onSuccess: user => $"   ✓ User validated: {user.Username} ({user.Email})",
        onFailure: error => $"   ✗ {error.Message}"
    );
Console.WriteLine(validUser);

var invalidUser = ValidateUser("jo", "invalid", "123")
    .Match(
        onSuccess: user => $"   ✓ User validated: {user.Username} ({user.Email})",
        onFailure: error => $"   ✗ {error.Message}"
    );
Console.WriteLine(invalidUser);

Console.WriteLine();

// ============================================
// Example 4: Chaining Multiple Operations
// ============================================
Console.WriteLine("4. Chaining Multiple Operations (Calculator)");

var calculation = Calculate("10", "+", "5")
    .Match(
        onSuccess: result => $"   ✓ Result: {result}",
        onFailure: error => $"   ✗ {error.Message}"
    );
Console.WriteLine(calculation);

var invalidCalc = Calculate("10", "/", "0")
    .Match(
        onSuccess: result => $"   ✓ Result: {result}",
        onFailure: error => $"   ✗ {error.Message}"
    );
Console.WriteLine(invalidCalc);

Console.WriteLine();

// ============================================
// Example 5: Option - Safe Dictionary Access
// ============================================
Console.WriteLine("5. Safe Dictionary Access");

var settings = new Dictionary<string, string>
{
    ["host"] = "localhost",
    ["port"] = "8080"
};

var host = GetSetting(settings, "host")
    .Match(
        some: value => $"   ✓ Host: {value}",
        none: () => "   ✗ Host not configured"
    );
Console.WriteLine(host);

var timeout = GetSetting(settings, "timeout")
    .Match(
        some: value => $"   ✓ Timeout: {value}",
        none: () => "   ✗ Timeout not configured (using default)"
    );
Console.WriteLine(timeout);

Console.WriteLine();

// ============================================
// Example 6: Combining Results
// ============================================
Console.WriteLine("6. Combining Multiple Results");

var order = CreateOrder("john@example.com", "100.50", "USD")
    .Match(
        onSuccess: o => $"   ✓ Order created: {o.CustomerEmail}, {o.Amount} {o.Currency}",
        onFailure: error => $"   ✗ Order creation failed: {error.Message}"
    );
Console.WriteLine(order);

Console.WriteLine("\n=== Examples Completed ===");

// ============================================
// Helper Functions
// ============================================

static Result<int> ParseInt(string s)
{
    if (int.TryParse(s, out int val))
        return Result<int>.Success(val);
    return Result<int>.Failure(Error.Generic($"'{s}' is not a valid integer"));
}

static Result<int> MultiplyByTwo(int x) => 
    Result<int>.Success(x * 2);

static Result<string> ConvertToMessage(int x) => 
    Result<string>.Success($"The result is {x}");

static Option<int> ParseIntOption(string s)
{
    if (int.TryParse(s, out int val))
        return Option<int>.Some(val);
    return Option<int>.None();
}

static Option<string> GetSetting(Dictionary<string, string> settings, string key)
{
    return settings.TryGetValue(key, out var value) 
        ? Option<string>.Some(value) 
        : Option<string>.None();
}

static Result<UserInput> ValidateUser(string username, string email, string password)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        errors.Add("Username must be at least 3 characters");

    if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        errors.Add("Email must be valid");

    if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        errors.Add("Password must be at least 8 characters");

    return errors.Any()
        ? Result<UserInput>.Failure(Error.Generic("Validation errors:\n" + string.Join("\n", errors.Select(e => $"  - {e}"))))
        : Result<UserInput>.Success(new UserInput(username, email, password));
}

// Calculator
static Result<double> Calculate(string leftStr, string operation, string rightStr)
{
    return ParseDouble(leftStr)
        .Then(left => ParseDouble(rightStr)
            .Then(right => PerformOperation(left, right, operation))
        );
}

static Result<double> ParseDouble(string s)
{
    if (double.TryParse(s, out double val))
        return Result<double>.Success(val);
    return Result<double>.Failure(Error.Generic($"'{s}' is not a valid number"));
}

static Result<double> PerformOperation(double left, double right, string operation)
{
    return operation switch
    {
        "+" => Result<double>.Success(left + right),
        "-" => Result<double>.Success(left - right),
        "*" => Result<double>.Success(left * right),
        "/" when right != 0 => Result<double>.Success(left / right),
        "/" => Result<double>.Failure(Error.Generic("Division by zero")),
        _ => Result<double>.Failure(Error.Generic($"Unknown operation: {operation}"))
    };
}

static Result<Order> CreateOrder(string email, string amount, string currency)
{
    return ValidateEmail(email)
        .Then(_ => ValidateAmount(amount)
            .Then(validAmount => ValidateCurrency(currency)
                .Then(validCurrency => 
                    Result<Order>.Success(new Order(email, validAmount, validCurrency)))
            )
        );
}

static Result<string> ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return Result<string>.Failure(Error.Generic("Email is required"));
    if (!email.Contains("@"))
        return Result<string>.Failure(Error.Generic("Email must be valid"));
    return Result<string>.Success(email);
}

static Result<string> ValidateAmount(string amount)
{
    if (!decimal.TryParse(amount, out var val))
        return Result<string>.Failure(Error.Generic("Amount must be a valid number"));
    if (val <= 0)
        return Result<string>.Failure(Error.Generic("Amount must be positive"));
    return Result<string>.Success(amount);
}

static Result<string> ValidateCurrency(string currency)
{
    if (string.IsNullOrWhiteSpace(currency))
        return Result<string>.Failure(Error.Generic("Currency is required"));
    if (currency.Length != 3)
        return Result<string>.Failure(Error.Generic("Currency must be 3 characters (e.g., USD, EUR)"));
    return Result<string>.Success(currency);
}

// ============================================
// Type Declarations
// ============================================

record UserInput(string Username, string Email, string Password);
record Order(string CustomerEmail, string Amount, string Currency);
