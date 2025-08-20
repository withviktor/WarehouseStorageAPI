using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Services;

public interface IActivityLogService
{
    Task LogActivityAsync(int userId, string action, string entityType, int? entityId = null, string? details = null, string? ipAddress = null, string? userAgent = null);
    Task LogLoginAsync(int userId, string ipAddress, string userAgent, bool success);
    Task LogStorageOperationAsync(int userId, string operation, int itemId, string details, string ipAddress, string userAgent);
}