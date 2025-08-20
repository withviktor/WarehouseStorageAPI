using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly WarehouseContext _context;
    private readonly ILogger<ActivityLogService> _logger;

    public ActivityLogService(WarehouseContext context, ILogger<ActivityLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(int userId, string action, string entityType, int? entityId = null, string? details = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var activity = new UserActivity
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity for user {UserId}: {Action} on {EntityType}", userId, action, entityType);
        }
    }

    public async Task LogLoginAsync(int userId, string ipAddress, string userAgent, bool success)
    {
        var action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED";
        var details = $"Login attempt from {ipAddress}";
        await LogActivityAsync(userId, action, "Authentication", null, details, ipAddress, userAgent);
    }

    public async Task LogStorageOperationAsync(int userId, string operation, int itemId, string details, string ipAddress, string userAgent)
    {
        await LogActivityAsync(userId, operation, "StorageItem", itemId, details, ipAddress, userAgent);
    }
}