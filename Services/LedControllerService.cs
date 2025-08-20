using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Services;

public class LedControllerService : ILedControllerService, IDisposable
{
    private SerialPort? _serialPort;
    private readonly ILogger<LedControllerService> _logger;
    private readonly Dictionary<string, int> _locationToZoneMap;
    private readonly bool _isDevelopment;
    private readonly bool _forceHardwareMode;
    private readonly bool _isWireless;
    private readonly string _picoIp;
    private readonly int _picoPort;

    public bool IsConnected => _isWireless || (_serialPort?.IsOpen ?? false);

    public LedControllerService(
        ILogger<LedControllerService> logger,
        IWebHostEnvironment env,
        IConfiguration config
    )
    {
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
        _forceHardwareMode = config.GetValue<bool>("TestWithRealLEDs", false);
        _isWireless = config.GetValue<bool>("LedController:IsWireless", false);
        _picoIp = config.GetValue<string>("LedController:PicoIp", "192.168.1.50");
        _picoPort = config.GetValue<int>("LedController:PicoPort", 5000);

        _locationToZoneMap = InitializeLocationMapping();

        // Try hardware connection if not in development OR if forced
        if (!_isDevelopment || _forceHardwareMode)
        {
            if (_isWireless)
            {
                _logger.LogInformation(
                    $"Using wireless mode for LED controller ({_picoIp}:{_picoPort})"
                );
            }
            else
            {
                _logger.LogInformation("Attempting to connect to LED hardware via Serial...");
                TryConnectToHardware();
            }
        }
        else
        {
            _logger.LogInformation("Running in development mode - LED controller mocked");
        }
    }

    private void TryConnectToHardware()
    {
        try
        {
            var availablePorts = SerialPort.GetPortNames();
            _logger.LogInformation(
                $"Available serial ports: {string.Join(", ", availablePorts)}"
            );

            var portName = FindPicoPort();
            if (!string.IsNullOrEmpty(portName))
            {
                _logger.LogInformation($"Attempting to connect to {portName}");

                _serialPort = new SerialPort(portName, 9600)
                {
                    ReadTimeout = 2000,
                    WriteTimeout = 2000,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.Open();

                // Give the device time to initialize
                Thread.Sleep(2000);

                _logger.LogInformation(
                    $"Successfully connected to LED controller on {portName}"
                );
            }
            else
            {
                _logger.LogWarning(
                    "No Pico device found. Available ports: " +
                    string.Join(", ", availablePorts)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LED controller");
            _serialPort?.Dispose();
        }
    }

    private string? FindPicoPort()
    {
        var ports = SerialPort.GetPortNames();
        _logger.LogInformation($"All available ports: {string.Join(", ", ports)}");

        // On macOS, always prefer cu.usbmodem over tty.usbmodem
        var cuPort = ports.FirstOrDefault(port => port.Contains("cu.usbmodem"));
        if (cuPort != null)
        {
            _logger.LogInformation($"Found Pico port (cu): {cuPort}");
            return cuPort;
        }

        // On Linux (Pi), it's usually /dev/ttyACM*
        var linuxPicoPort = ports.FirstOrDefault(port => port.Contains("ttyACM"));
        if (linuxPicoPort != null)
        {
            _logger.LogInformation($"Found Pico port (Linux): {linuxPicoPort}");
            return linuxPicoPort;
        }

        _logger.LogWarning("No Pico-like ports found");
        return null;
    }

    private Dictionary<string, int> InitializeLocationMapping()
    {
        return new Dictionary<string, int>
        {
            { "A1-01", 1 }, { "A1-02", 1 }, { "A1-03", 1 },
            { "A2-01", 2 }, { "A2-02", 2 }, { "A2-03", 2 }
        };
    }

    public async Task<bool> SendCommandAsync(LedCommand command)
    {
        // Use mock mode if in development and not forcing hardware
        if (_isDevelopment && !_forceHardwareMode)
        {
            _logger.LogInformation(
                $"[MOCK] LED Command - Zone: {command.Zone}, Color: {command.Color}, " +
                $"Action: {command.Action}"
            );
            await Task.Delay(100);
            return true;
        }

        if (_isWireless)
        {
            return await SendCommandOverTcpAsync(command);
        }
        else
        {
            return await SendCommandOverSerialAsync(command);
        }
    }

    private async Task<bool> SendCommandOverTcpAsync(LedCommand command)
    {
        try
        {
            using var client = new TcpClient();
            client.ReceiveTimeout = 2000;
            client.SendTimeout = 2000;

            await client.ConnectAsync(_picoIp, _picoPort);

            var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var data = Encoding.UTF8.GetBytes(json);
            await client.GetStream().WriteAsync(data);
            await client.GetStream().FlushAsync();

            _logger.LogInformation($"Sent LED command over TCP: {json}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send LED command over TCP");
            return false;
        }
    }

    private async Task<bool> SendCommandOverSerialAsync(LedCommand command)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("LED controller not connected - cannot send command");
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(command, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var data = Encoding.UTF8.GetBytes(json + "\n");
            await _serialPort!.BaseStream.WriteAsync(data);
            await _serialPort.BaseStream.FlushAsync();

            _logger.LogInformation($"Sent LED command over Serial: {json}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send LED command over Serial");
            return false;
        }
    }

    public async Task<bool> HighlightLocationAsync(string location, string color = "blue")
    {
        if (!_locationToZoneMap.TryGetValue(location, out var zone))
        {
            _logger.LogWarning($"Unknown location: {location}");
            return false;
        }

        var command = new LedCommand
        {
            Zone = zone,
            Color = color,
            Action = "blink",
            Duration = 10000,
            Brightness = 200
        };

        return await SendCommandAsync(command);
    }

    public async Task<bool> TurnOffAllAsync()
    {
        var command = new LedCommand
        {
            Zone = 0,
            Color = "off",
            Action = "off"
        };

        return await SendCommandAsync(command);
    }

    public async Task<bool> TestConnectionAsync()
    {
        if (_isDevelopment && !_forceHardwareMode)
        {
            _logger.LogInformation("[MOCK] LED test connection successful");
            await Task.Delay(500);
            return true;
        }

        if (!IsConnected && !_isWireless)
        {
            _logger.LogWarning("Cannot test connection - LED controller not connected");
            return false;
        }

        var command = new LedCommand
        {
            Zone = 1,
            Color = "green",
            Action = "pulse",
            Duration = 3000
        };

        return await SendCommandAsync(command);
    }

    public void Dispose()
    {
        try
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing LED controller");
        }
    }
}