using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Services.Decorators;

// Logging decorator
public class LoggingUserServiceDecorator : IUserService
{
    private readonly IUserService _userService;
    private readonly ILogger<LoggingUserServiceDecorator> _logger;

    public LoggingUserServiceDecorator(IUserService userService, ILogger<LoggingUserServiceDecorator> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(string name, string email)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", name, email);
        var start = DateTime.UtcNow;
        
        try
        {
            var result = await _userService.CreateUserAsync(name, email);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("User created successfully in {Duration}ms", duration.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "Failed to create user after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        _logger.LogInformation("Getting user: {UserId}", userId);
        var start = DateTime.UtcNow;
        
        try
        {
            var result = await _userService.GetUserAsync(userId);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("User retrieved in {Duration}ms. Found: {Found}", duration.TotalMilliseconds, result != null);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "Failed to get user after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        _logger.LogInformation("Getting all users");
        var start = DateTime.UtcNow;
        
        try
        {
            var result = await _userService.GetAllUsersAsync();
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("Retrieved {Count} users in {Duration}ms", result.Count, duration.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "Failed to get all users after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, string name, string email)
    {
        _logger.LogInformation("Updating user {UserId}: {Name} ({Email})", userId, name, email);
        var start = DateTime.UtcNow;
        
        try
        {
            var result = await _userService.UpdateUserAsync(userId, name, email);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("User updated in {Duration}ms. Success: {Success}", duration.TotalMilliseconds, result != null);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "Failed to update user after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        _logger.LogInformation("Deleting user: {UserId}", userId);
        var start = DateTime.UtcNow;
        
        try
        {
            var result = await _userService.DeleteUserAsync(userId);
            var duration = DateTime.UtcNow - start;
            _logger.LogInformation("User deletion completed in {Duration}ms. Success: {Success}", duration.TotalMilliseconds, result);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - start;
            _logger.LogError(ex, "Failed to delete user after {Duration}ms", duration.TotalMilliseconds);
            throw;
        }
    }
}

// Validation decorator
public class ValidationUserServiceDecorator : IUserService
{
    private readonly IUserService _userService;
    private readonly ILogger<ValidationUserServiceDecorator> _logger;

    public ValidationUserServiceDecorator(IUserService userService, ILogger<ValidationUserServiceDecorator> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(string name, string email)
    {
        ValidateUserInput(name, email);
        return await _userService.CreateUserAsync(name, email);
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Invalid user ID: {UserId}", userId);
            throw new ArgumentException("User ID must be positive", nameof(userId));
        }
        
        return await _userService.GetUserAsync(userId);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _userService.GetAllUsersAsync();
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, string name, string email)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Invalid user ID for update: {UserId}", userId);
            throw new ArgumentException("User ID must be positive", nameof(userId));
        }
        
        ValidateUserInput(name, email);
        return await _userService.UpdateUserAsync(userId, name, email);
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Invalid user ID for deletion: {UserId}", userId);
            throw new ArgumentException("User ID must be positive", nameof(userId));
        }
        
        return await _userService.DeleteUserAsync(userId);
    }

    private void ValidateUserInput(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("Invalid name provided: empty or whitespace");
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            _logger.LogWarning("Invalid email provided: {Email}", email);
            throw new ArgumentException("Email must be valid", nameof(email));
        }
    }
}

// Caching decorator (simple in-memory cache)
public class CachingUserServiceDecorator : IUserService
{
    private readonly IUserService _userService;
    private readonly ILogger<CachingUserServiceDecorator> _logger;
    private readonly Dictionary<int, (UserDto User, DateTime CachedAt)> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public CachingUserServiceDecorator(IUserService userService, ILogger<CachingUserServiceDecorator> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(string name, string email)
    {
        var result = await _userService.CreateUserAsync(name, email);
        
        // Cache the created user
        _cache[result.Id] = (result, DateTime.UtcNow);
        _logger.LogDebug("Cached user {UserId}", result.Id);
        
        return result;
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        // Check cache first
        if (_cache.TryGetValue(userId, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _cacheExpiry)
            {
                _logger.LogDebug("Cache hit for user {UserId}", userId);
                return cached.User;
            }
            else
            {
                _cache.Remove(userId);
                _logger.LogDebug("Cache expired for user {UserId}", userId);
            }
        }

        _logger.LogDebug("Cache miss for user {UserId}", userId);
        var result = await _userService.GetUserAsync(userId);
        
        if (result != null)
        {
            _cache[userId] = (result, DateTime.UtcNow);
            _logger.LogDebug("Cached user {UserId}", userId);
        }
        
        return result;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        // For simplicity, don't cache the full list
        return await _userService.GetAllUsersAsync();
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, string name, string email)
    {
        var result = await _userService.UpdateUserAsync(userId, name, email);
        
        if (result != null)
        {
            // Update cache
            _cache[userId] = (result, DateTime.UtcNow);
            _logger.LogDebug("Updated cache for user {UserId}", userId);
        }
        
        return result;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var result = await _userService.DeleteUserAsync(userId);
        
        if (result)
        {
            // Remove from cache
            _cache.Remove(userId);
            _logger.LogDebug("Removed user {UserId} from cache", userId);
        }
        
        return result;
    }
}