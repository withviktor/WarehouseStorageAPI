using Microsoft.AspNetCore.Mvc;
using WarehouseStorageAPI.Models;
using WarehouseStorageAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace WarehouseStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // Only admin users can access LED controls
public class LedController : ControllerBase
{
    private readonly ILedControllerService _ledService;
    private readonly IUserActionService _userActionService;

    public LedController(ILedControllerService ledService, IUserActionService userActionService)
    {
        _ledService = ledService;
        _userActionService = userActionService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var isConnected = _ledService.IsConnected;
        
        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Check LED Status",
            $"Checked LED controller connection status: {(isConnected ? "Connected" : "Disconnected")}",
            "LED",
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(new { IsConnected = isConnected });
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand(LedCommand command)
    {
        var success = await _ledService.SendCommandAsync(command);
        
        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Send LED Command",
            $"Sent LED command: {command} (Success: {success})",
            "LED",
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        if (!success)
            return StatusCode(500, "Failed to send command");

        return Ok(new { Message = "Command sent successfully" });
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection()
    {
        var success = await _ledService.TestConnectionAsync();
        
        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Test LED Connection",
            $"Tested LED controller connection (Success: {success})",
            "LED",
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        if (!success)
            return StatusCode(500, "LED controller not responding");

        return Ok(new { Message = "LED test successful" });
    }

    [HttpPost("off")]
    public async Task<IActionResult> TurnOffAll()
    {
        var success = await _ledService.TurnOffAllAsync();
        
        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Turn Off All LEDs",
            $"Turned off all LEDs (Success: {success})",
            "LED",
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        if (!success)
            return StatusCode(500, "Failed to turn off LEDs");

        return Ok(new { Message = "All LEDs turned off" });
    }
}