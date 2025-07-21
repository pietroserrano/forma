using Forma.Mediator.Extensions;
using Forma.Decorator.Extensions;
using Forma.Chains.Extensions;
using Forma.Examples.Web.AspNetCore.Services;
using Forma.Examples.Web.AspNetCore.Services.Decorators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Forma Mediator
builder.Services.AddRequestMediator(config =>
{
    // Register handlers from this assembly
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly);
});

// Add Forma Decorator (if available)
// Note: Check if Forma.Decorator provides DI extensions
// For now, we'll manually configure decorators

// Add Forma Chains (for future pipeline examples)
// builder.Services.AddChains();

// Register core services
builder.Services.AddScoped<UserService>();

// Apply decorators manually in order: Logging -> Validation -> Caching -> UserService
builder.Services.AddScoped<IUserService>(provider =>
{
    var userService = provider.GetRequiredService<UserService>();
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    
    // Apply decorators in reverse order (inner to outer)
    IUserService decorated = userService;
    decorated = new CachingUserServiceDecorator(decorated, loggerFactory.CreateLogger<CachingUserServiceDecorator>());
    decorated = new ValidationUserServiceDecorator(decorated, loggerFactory.CreateLogger<ValidationUserServiceDecorator>());
    decorated = new LoggingUserServiceDecorator(decorated, loggerFactory.CreateLogger<LoggingUserServiceDecorator>());
    
    return decorated;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .WithTags("Health");

app.Run();
