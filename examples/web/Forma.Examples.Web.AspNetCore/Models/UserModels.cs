using Forma.Abstractions;

namespace Forma.Examples.Web.AspNetCore.Models;

// DTOs for API responses
public record UserDto(int Id, string Name, string Email, DateTime CreatedAt);
public record CreateUserRequest(string Name, string Email);
public record UserCreatedResponse(int Id, string Message);

// Commands and Queries
public record CreateUserCommand(string Name, string Email) : IRequest<UserCreatedResponse>;
public record GetUserQuery(int UserId) : IRequest<UserDto>;
public record GetAllUsersQuery : IRequest<List<UserDto>>;
public record UpdateUserCommand(int UserId, string Name, string Email) : IRequest<UserDto>;
public record DeleteUserCommand(int UserId) : IRequest;