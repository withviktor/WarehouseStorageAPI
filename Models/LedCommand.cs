namespace WarehouseStorageAPI.Models;

public class LedCommand
{
    public int Zone { get; set; }
    public string Color { get; set; } = "white";
    public string Action { get; set; } = "on"; // "on", "off", "blink", "pulse"
    public int Duration { get; set; } = 5000; // milliseconds
    public int Brightness { get; set; } = 255; // 0-255
}