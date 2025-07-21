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

// Register core services first
builder.Services.AddScoped<IUserService, UserService>();

// Apply decorators using Forma.Decorator library
// Order: Inner (UserService) -> Caching -> Validation -> Logging (Outer)
builder.Services.Decorate<IUserService, CachingUserServiceDecorator>();
builder.Services.Decorate<IUserService, ValidationUserServiceDecorator>();
builder.Services.Decorate<IUserService, LoggingUserServiceDecorator>();

// Add Forma Chains
// Register chain handlers for order processing
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.OrderValidationHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.InventoryCheckHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.OrderPricingHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.OrderCreationHandler>();

// Register chain handlers for payment processing
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.PaymentValidationHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.PaymentFraudDetectionHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.PaymentProcessingHandler>();
builder.Services.AddTransient<Forma.Examples.Web.AspNetCore.Chains.PaymentNotificationHandler>();

// Configure order processing chain
builder.Services.AddChain<Forma.Examples.Web.AspNetCore.Models.OrderProcessingRequest, Forma.Examples.Web.AspNetCore.Models.OrderProcessingResponse>(
    typeof(Forma.Examples.Web.AspNetCore.Chains.OrderValidationHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.InventoryCheckHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.OrderPricingHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.OrderCreationHandler));

// Configure payment processing chain
builder.Services.AddChain<Forma.Examples.Web.AspNetCore.Models.PaymentProcessingRequest>(
    typeof(Forma.Examples.Web.AspNetCore.Chains.PaymentValidationHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.PaymentFraudDetectionHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.PaymentProcessingHandler),
    typeof(Forma.Examples.Web.AspNetCore.Chains.PaymentNotificationHandler));

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
