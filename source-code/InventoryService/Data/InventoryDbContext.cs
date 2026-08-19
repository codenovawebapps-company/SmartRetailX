using Microsoft.EntityFrameworkCore;
using InventoryService.Models;

namespace InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> Items => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.Property(e => e.ProductId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProductName).HasMaxLength(150);
        });
    }

    public void SeedDefaultInventory()
    {
        Database.EnsureCreated();

        if (!Items.Any())
        {
            Items.AddRange(
                new InventoryItem { ProductId = "1", ProductName = "Dell XPS 15 Laptop", AvailableStock = 25, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "2", ProductName = "Sony WH-1000XM5 Headphones", AvailableStock = 50, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "3", ProductName = "Logitech MX Master 3S Mouse", AvailableStock = 80, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "4", ProductName = "Keychron K2 Mechanical Keyboard", AvailableStock = 45, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "5", ProductName = "LG UltraFine 27-inch 4K Monitor", AvailableStock = 30, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "6", ProductName = "Herman Miller Ergonomic Chair", AvailableStock = 15, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "PROD001", ProductName = "Generic Product 1", AvailableStock = 100, ReservedStock = 0, UpdatedAt = DateTime.UtcNow },
                new InventoryItem { ProductId = "PROD002", ProductName = "Generic Product 2", AvailableStock = 50, ReservedStock = 0, UpdatedAt = DateTime.UtcNow }
            );
            SaveChanges();
        }
    }
}
