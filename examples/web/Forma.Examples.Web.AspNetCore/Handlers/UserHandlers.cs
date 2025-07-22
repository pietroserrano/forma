using Forma.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;
using Forma.Examples.Web.AspNetCore.Services;

namespace Forma.Examples.Web.AspNetCore.Handlers;

public class CreateUserCommandHandler : IHandler<CreateUserCommand, UserCreatedResponse>
{
    private readonly IUserService _userService;
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(IUserService userService, ILogger<CreateUserCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserCreatedResponse> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {Name} with email {Email}", request.Name, request.Email);
        
        var user = await _userService.CreateUserAsync(request.Name, request.Email);
        
        return new UserCreatedResponse(user.Id, $"User '{user.Name}' created successfully");
    }
}

public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(IUserService userService, ILogger<GetUserQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserDto> HandleAsync(GetUserQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", request.UserId);
        
        var user = await _userService.GetUserAsync(request.UserId);
        
        if (user == null)
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
            
        return user;
    }
}

public class GetAllUsersQueryHandler : IHandler<GetAllUsersQuery, List<UserDto>>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(IUserService userService, ILogger<GetAllUsersQueryHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<List<UserDto>> HandleAsync(GetAllUsersQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all users");
        
        return await _userService.GetAllUsersAsync();
    }
}

public class UpdateUserCommandHandler : IHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserService _userService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(IUserService userService, ILogger<UpdateUserCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<UserDto> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user {UserId}: {Name} with email {Email}", request.UserId, request.Name, request.Email);
        
        var user = await _userService.UpdateUserAsync(request.UserId, request.Name, request.Email);
        
        if (user == null)
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
            
        return user;
    }
}

public class DeleteUserCommandHandler : IHandler<DeleteUserCommand>
{
    private readonly IUserService _userService;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(IUserService userService, ILogger<DeleteUserCommandHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", request.UserId);
        
        var success = await _userService.DeleteUserAsync(request.UserId);
        
        if (!success)
            throw new KeyNotFoundException($"User with ID {request.UserId} not found");
    }
}