using System.ComponentModel.DataAnnotations;

namespace WarehouseStorageAPI.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty; // The login ID like 1002412
    
    [Required]
    [StringLength(100)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime LastLogin { get; set; }
    
    // Navigation property for user actions
    public ICollection<UserAction> UserActions { get; set; } = new List<UserAction>();
}

public enum UserRole
{
    User = 0,
    Admin = 1
}
