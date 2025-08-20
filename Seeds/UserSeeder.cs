using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace WarehouseStorageAPI.Seeds;

public class UserSeeder : ISeedData
{
    private readonly WarehouseContext _context;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(WarehouseContext context, ILogger<UserSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedAdminUserAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        // Check if admin user already exists
        if (await _context.Users.AnyAsync(u => u.UserId == "1000001"))
        {
            _logger.LogInformation("Admin user already exists, skipping creation");
            return;
        }

        var randomPassword = GenerateRandomPassword();
        var passwordHash = HashPassword(randomPassword);

        var adminUser = new User
        {
            UserId = "1000001",
            PasswordHash = passwordHash,
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        // Log the credentials securely
        _logger.LogWarning("=== INITIAL ADMIN USER CREATED ===");
        _logger.LogWarning("User ID: 1000001");
        _logger.LogWarning("Password: {Password}", randomPassword);
        _logger.LogWarning("Please save this password securely!");
        _logger.LogWarning("=====================================");

        Console.WriteLine("=== INITIAL ADMIN USER CREATED ===");
        Console.WriteLine($"User ID: 1000001");
        Console.WriteLine($"Password: {randomPassword}");
        Console.WriteLine("Please save this password securely!");
        Console.WriteLine("===================================");
    }

    private static string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "warehouse_salt"));
        return Convert.ToBase64String(hashedBytes);
    }
}