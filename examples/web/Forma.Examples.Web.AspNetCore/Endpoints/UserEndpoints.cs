using Forma.Core.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/users")
            .WithTags("Users");

        // Get all users
        users.MapGet("/", async (IRequestMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                var users = await mediator.SendAsync(new GetAllUsersQuery());
                return Results.Ok(users);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all users");
                return Results.Problem("Internal server error");
            }
        })
        .WithSummary("Get all users")
        .WithDescription("Retrieves a list of all users in the system")
        .Produces<List<UserDto>>();

        // Get user by ID
        users.MapGet("/{id:int}", async (int id, IRequestMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                var user = await mediator.SendAsync(new GetUserQuery(id));
                return Results.Ok(user);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"User with ID {id} not found");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user {UserId}", id);
                return Results.Problem("Internal server error");
            }
        })
        .WithSummary("Get a specific user by ID")
        .WithDescription("Retrieves a user by their unique identifier")
        .Produces<UserDto>()
        .Produces(404)
        .Produces(400);

        // Create user
        users.MapPost("/", async (CreateUserRequest request, IRequestMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                var command = new CreateUserCommand(request.Name, request.Email);
                var result = await mediator.SendAsync(command);
                return Results.Created($"/api/users/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user");
                return Results.Problem("Internal server error");
            }
        })
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user with the provided information")
        .Accepts<CreateUserRequest>("application/json")
        .Produces<UserCreatedResponse>(201)
        .Produces(400);

        // Update user
        users.MapPut("/{id:int}", async (int id, CreateUserRequest request, IRequestMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                var command = new UpdateUserCommand(id, request.Name, request.Email);
                var result = await mediator.SendAsync(command);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"User with ID {id} not found");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user {UserId}", id);
                return Results.Problem("Internal server error");
            }
        })
        .WithSummary("Update an existing user")
        .WithDescription("Updates an existing user with new information")
        .Accepts<CreateUserRequest>("application/json")
        .Produces<UserDto>()
        .Produces(404)
        .Produces(400);

        // Delete user
        users.MapDelete("/{id:int}", async (int id, IRequestMediator mediator, ILogger<Program> logger) =>
        {
            try
            {
                await mediator.SendAsync(new DeleteUserCommand(id));
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound($"User with ID {id} not found");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user {UserId}", id);
                return Results.Problem("Internal server error");
            }
        })
        .WithSummary("Delete a user")
        .WithDescription("Deletes a user from the system")
        .Produces(204)
        .Produces(404)
        .Produces(400);
    }
}