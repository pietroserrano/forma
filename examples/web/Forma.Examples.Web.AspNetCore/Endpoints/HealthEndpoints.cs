namespace Forma.Examples.Web.AspNetCore.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Add a simple health check endpoint
        endpoints.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
            .WithTags("Health");
    }
}