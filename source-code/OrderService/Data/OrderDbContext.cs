using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
        });
    }

    public void SeedDefaultOrders()
    {
        Database.EnsureCreated();

        if (!Orders.Any())
        {
            var o1 = new Order
            {
                UserId = 2,
                CustomerName = "Jane Doe",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                Status = "Delivered",
                TotalAmount = 1499.99m,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 1, ProductName = "Dell XPS 15 Laptop", Quantity = 1, UnitPrice = 1499.99m }
                }
            };

            var o2 = new Order
            {
                UserId = 2,
                CustomerName = "Jane Doe",
                OrderDate = DateTime.UtcNow.AddHours(-12),
                Status = "Processing",
                TotalAmount = 449.98m,
                Items = new List<OrderItem>
                {
                    new() { ProductId = 2, ProductName = "Sony WH-1000XM5 Headphones", Quantity = 1, UnitPrice = 349.99m },
                    new() { ProductId = 3, ProductName = "Logitech MX Master 3S Mouse", Quantity = 1, UnitPrice = 99.99m }
                }
            };

            Orders.AddRange(o1, o2);
            SaveChanges();
        }
    }
}
