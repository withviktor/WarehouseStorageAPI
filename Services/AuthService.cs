using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.DTOs;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Services;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<IEnumerable<UserDto>> GetUsersAsync();
    Task<UserDto?> GetUserAsync(int id);
    Task<bool> DeactivateUserAsync(int id);
    Task LogUserActionAsync(int userId, string action, string? details = null, string? entityType = null, int? entityId = null, string? ipAddress = null);
    Task<IEnumerable<UserActionDto>> GetUserActionsAsync(int? userId = null, int page = 1, int pageSize = 50);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hashedPassword);
}

public class AuthService : IAuthService
{
    private readonly WarehouseContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(WarehouseContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == loginDto.UserId && u.IsActive);

        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            return null;

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new LoginResponseDto
        {
            Token = token,
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString()
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        // Check if user ID already exists
        if (await _context.Users.AnyAsync(u => u.UserId == createUserDto.UserId))
            throw new InvalidOperationException("User ID already exists");

        // Parse role
        if (!Enum.TryParse<UserRole>(createUserDto.Role, true, out var role))
            throw new ArgumentException("Invalid role specified");

        // Generate random password
        var randomPassword = GenerateRandomPassword();
        var hashedPassword = HashPassword(randomPassword);

        var user = new User
        {
            UserId = createUserDto.UserId,
            PasswordHash = hashedPassword,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Log the password to console (in production, this should be sent securely to the user)
        Console.WriteLine($"New User Created:");
        Console.WriteLine($"User ID: {user.UserId}");
        Console.WriteLine($"Password: {randomPassword}");
        Console.WriteLine($"Role: {user.Role}");

        return new UserDto
        {
            Id = user.Id,
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        return await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLogin = u.LastLogin
            })
            .OrderBy(u => u.UserId)
            .ToListAsync();
    }

    public async Task<UserDto?> GetUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }

    public async Task<bool> DeactivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task LogUserActionAsync(int userId, string action, string? details = null, string? entityType = null, int? entityId = null, string? ipAddress = null)
    {
        var userAction = new UserAction
        {
            UserId = userId,
            Action = action,
            Details = details,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress
        };

        _context.UserActions.Add(userAction);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<UserActionDto>> GetUserActionsAsync(int? userId = null, int page = 1, int pageSize = 50)
    {
        var query = _context.UserActions
            .Include(ua => ua.User)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(ua => ua.UserId == userId.Value);

        return await query
            .OrderByDescending(ua => ua.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ua => new UserActionDto
            {
                Id = ua.Id,
                UserId = ua.User.UserId,
                UserName = $"{ua.User.FirstName} {ua.User.LastName}",
                Action = ua.Action,
                Details = ua.Details,
                EntityType = ua.EntityType,
                EntityId = ua.EntityId,
                Timestamp = ua.Timestamp,
                IpAddress = ua.IpAddress
            })
            .ToListAsync();
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "warehouse_salt"));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == hashedPassword;
    }

    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "your-super-secret-key-that-should-be-at-least-256-bits-long";
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("userLogin", user.UserId),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRandomPassword(int length = 12)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
