using Microsoft.AspNetCore.Mvc;
using WarehouseStorageAPI.Models;
using WarehouseStorageAPI.Services;

namespace WarehouseStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LedController : ControllerBase
{
    private readonly ILedControllerService _ledService;

    public LedController(ILedControllerService ledService)
    {
        _ledService = ledService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { IsConnected = _ledService.IsConnected });
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand(LedCommand command)
    {
        var success = await _ledService.SendCommandAsync(command);
        if (!success)
            return StatusCode(500, "Failed to send command");

        return Ok(new { Message = "Command sent successfully" });
    }

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection()
    {
        var success = await _ledService.TestConnectionAsync();
        if (!success)
            return StatusCode(500, "LED controller not responding");

        return Ok(new { Message = "LED test successful" });
    }

    [HttpPost("off")]
    public async Task<IActionResult> TurnOffAll()
    {
        var success = await _ledService.TurnOffAllAsync();
        if (!success)
            return StatusCode(500, "Failed to turn off LEDs");

        return Ok(new { Message = "All LEDs turned off" });
    }

    [HttpPost("highlight/{location}")]
    public async Task<IActionResult> HighlightLocation(string location, [FromQuery] string color = "blue")
    {
        var success = await _ledService.HighlightLocationAsync(location, color);
        if (!success)
            return StatusCode(500, "Failed to highlight location");

        return Ok(new { Message = $"Location {location} highlighted" });
    }
    
    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        return Ok(new 
        { 
            IsConnected = _ledService.IsConnected,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            AvailablePorts = System.IO.Ports.SerialPort.GetPortNames(),
            Message = _ledService.IsConnected ? "Hardware connected" : "Running in mock mode"
        });
    }
}