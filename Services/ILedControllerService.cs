using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Services;

public interface ILedControllerService
{
    Task<bool> SendCommandAsync(LedCommand command);
    Task<bool> HighlightLocationAsync(string location, string color);
    Task<bool> TurnOffAllAsync();
    Task<bool> TestConnectionAsync();
    bool IsConnected { get; }
}