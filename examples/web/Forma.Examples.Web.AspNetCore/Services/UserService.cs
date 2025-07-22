using Microsoft.EntityFrameworkCore;
using Forma.Examples.Web.AspNetCore.Models;
using Forma.Examples.Web.AspNetCore.Data;
using Forma.Examples.Web.AspNetCore.Data.Entities;

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
    private readonly FormaExamplesDbContext _dbContext;

    public UserService(ILogger<UserService> logger, FormaExamplesDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<UserDto> CreateUserAsync(string name, string email)
    {
        var user = new User
        {
            Name = name,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("User created: {UserId}", user.Id);
        return MapToDto(user);
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        _logger.LogInformation("User {UserId} retrieved: {Found}", userId, user != null);
        return user != null ? MapToDto(user) : null;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _dbContext.Users.ToListAsync();
        _logger.LogInformation("Retrieved {Count} users", users.Count);
        return users.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, string name, string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for update", userId);
            return null;
        }

        user.Name = name;
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} updated", userId);
        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for deletion", userId);
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} deleted", userId);
        return true;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto(user.Id, user.Name, user.Email, user.CreatedAt);
    }
}