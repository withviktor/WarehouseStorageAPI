using WarehouseStorageAPI.Services;
using System.Security.Claims;

namespace WarehouseStorageAPI.Services;

public interface IUserActionService
{
    Task LogActionAsync(ClaimsPrincipal user, string action, string? details = null, string? entityType = null, int? entityId = null, string? ipAddress = null);
    int GetCurrentUserId(ClaimsPrincipal user);
}

public class UserActionService : IUserActionService
{
    private readonly IAuthService _authService;

    public UserActionService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task LogActionAsync(ClaimsPrincipal user, string action, string? details = null, string? entityType = null, int? entityId = null, string? ipAddress = null)
    {
        var userId = GetCurrentUserId(user);
        if (userId > 0)
        {
            await _authService.LogUserActionAsync(userId, action, details, entityType, entityId, ipAddress);
        }
    }

    public int GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
