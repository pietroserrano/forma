using Microsoft.AspNetCore.Mvc;
using Forma.Abstractions;
using Forma.Core.Abstractions;
using Forma.Examples.Web.AspNetCore.Models;

namespace Forma.Examples.Web.AspNetCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRequestMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IRequestMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        try
        {
            var users = await _mediator.SendAsync(new GetAllUsersQuery());
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        try
        {
            var user = await _mediator.SendAsync(new GetUserQuery(id));
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {id} not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserCreatedResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var command = new CreateUserCommand(request.Name, request.Email);
            var result = await _mediator.SendAsync(command);
            return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] CreateUserRequest request)
    {
        try
        {
            var command = new UpdateUserCommand(id, request.Name, request.Email);
            var result = await _mediator.SendAsync(command);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {id} not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            await _mediator.SendAsync(new DeleteUserCommand(id));
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"User with ID {id} not found");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
