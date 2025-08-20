using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseStorageAPI.DTOs;
using WarehouseStorageAPI.Services;

namespace WarehouseStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
    {
        var result = await _authService.LoginAsync(loginDto);
        if (result == null)
            return Unauthorized("Invalid credentials");

        // Log login action
        var userId = await GetUserIdByLoginId(result.UserId);
        if (userId > 0)
        {
            await _authService.LogUserActionAsync(
                userId, 
                "User Login", 
                $"User {result.FirstName} {result.LastName} logged in",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );
        }

        return Ok(result);
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        try
        {
            var user = await _authService.CreateUserAsync(createUserDto);
            
            // Log user creation action
            var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
            await _authService.LogUserActionAsync(
                currentUserId,
                "Create User",
                $"Created new user: {user.FirstName} {user.LastName} ({user.UserId})",
                "User",
                user.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _authService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _authService.GetUserAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var success = await _authService.DeactivateUserAsync(id);
        if (!success)
            return NotFound();

        // Log user deactivation
        var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
        await _authService.LogUserActionAsync(
            currentUserId,
            "Deactivate User",
            $"Deactivated user with ID: {id}",
            "User",
            id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }

    [HttpGet("actions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserActionDto>>> GetUserActions(
        [FromQuery] int? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var actions = await _authService.GetUserActionsAsync(userId, page, pageSize);
        return Ok(actions);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim == null)
            return Unauthorized();

        var user = await _authService.GetUserAsync(int.Parse(userIdClaim));
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    private async Task<int> GetUserIdByLoginId(string loginId)
    {
        var user = await _authService.GetUsersAsync();
        var foundUser = user.FirstOrDefault(u => u.UserId == loginId);
        return foundUser?.Id ?? 0;
    }
}
