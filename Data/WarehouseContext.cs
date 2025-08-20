using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Data;

public class WarehouseContext : DbContext
{
    public WarehouseContext(DbContextOptions<WarehouseContext> options) 
        : base(options) { }

    public DbSet<StorageItem> StorageItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserAction> UserActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure StorageItem
        modelBuilder.Entity<StorageItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Location);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Configure InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.StorageItem)
                .WithMany()
                .HasForeignKey(e => e.StorageItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Role).HasConversion<int>();
        });

        // Configure UserAction
        modelBuilder.Entity<UserAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserActions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Timestamp);
        });

        // No HasData calls - all seeding moved to DatabaseSeeder
    }
}