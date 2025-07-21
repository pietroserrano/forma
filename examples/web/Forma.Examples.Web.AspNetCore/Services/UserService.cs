using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(string name, string email);
    Task<UserDto?> GetUserAsync(int userId);
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> UpdateUserAsync(int userId, string name, string email);
    Task<bool> DeleteUserAsync(int userId);
}

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    
    // In-memory storage for demonstration
    private static readonly List<UserDto> _users = new();
    private static int _nextId = 1;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public Task<UserDto> CreateUserAsync(string name, string email)
    {
        var user = new UserDto(_nextId++, name, email, DateTime.UtcNow);
        _users.Add(user);
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        return Task.FromResult(user);
    }

    public Task<UserDto?> GetUserAsync(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        _logger.LogInformation("User {UserId} retrieved: {Found}", userId, user != null);
        return Task.FromResult(user);
    }

    public Task<List<UserDto>> GetAllUsersAsync()
    {
        _logger.LogInformation("Retrieved {Count} users", _users.Count);
        return Task.FromResult(_users.ToList());
    }

    public Task<UserDto?> UpdateUserAsync(int userId, string name, string email)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == userId);
        if (existingUser == null)
        {
            _logger.LogWarning("User {UserId} not found for update", userId);
            return Task.FromResult<UserDto?>(null);
        }

        var updatedUser = new UserDto(userId, name, email, existingUser.CreatedAt);
        var index = _users.IndexOf(existingUser);
        _users[index] = updatedUser;
        
        _logger.LogInformation("User {UserId} updated", userId);
        return Task.FromResult<UserDto?>(updatedUser);
    }

    public Task<bool> DeleteUserAsync(int userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for deletion", userId);
            return Task.FromResult(false);
        }

        _users.Remove(user);
        _logger.LogInformation("User {UserId} deleted", userId);
        return Task.FromResult(true);
    }
}