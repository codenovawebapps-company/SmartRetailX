using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);
        });
    }

    public void SeedDefaultProducts()
    {
        Database.EnsureCreated();

        if (!Products.Any())
        {
            Products.AddRange(
                new Product
                {
                    Name = "Dell XPS 15 Laptop",
                    Description = "High-performance laptop with 15.6-inch 4K OLED display, Intel i9, 32GB RAM, 1TB SSD",
                    Price = 1499.99m,
                    Category = "Electronics",
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=500&auto=format&fit=crop&q=60",
                    Stock = 25,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Sony WH-1000XM5 Headphones",
                    Description = "Industry-leading noise-canceling wireless headphones with 30-hour battery life",
                    Price = 349.99m,
                    Category = "Audio",
                    ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500&auto=format&fit=crop&q=60",
                    Stock = 50,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Logitech MX Master 3S Mouse",
                    Description = "Ergonomic wireless performance mouse with quiet clicks and 8K DPI sensor",
                    Price = 99.99m,
                    Category = "Accessories",
                    ImageUrl = "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?w=500&auto=format&fit=crop&q=60",
                    Stock = 80,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Keychron K2 Mechanical Keyboard",
                    Description = "Wireless mechanical keyboard with RGB backlighting and Gateron G Pro switches",
                    Price = 89.99m,
                    Category = "Accessories",
                    ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500&auto=format&fit=crop&q=60",
                    Stock = 45,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "LG UltraFine 27-inch 4K Monitor",
                    Description = "IPS display with UHD resolution, HDR400, and USB-C 90W power delivery",
                    Price = 449.99m,
                    Category = "Electronics",
                    ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500&auto=format&fit=crop&q=60",
                    Stock = 30,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Herman Miller Ergonomic Chair",
                    Description = "Ergonomic mesh office chair with lumbar support and multi-angle adjustment",
                    Price = 699.99m,
                    Category = "Furniture",
                    ImageUrl = "https://images.unsplash.com/photo-1580481077114-1e09dfa98f12?w=500&auto=format&fit=crop&q=60",
                    Stock = 15,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            SaveChanges();
        }
    }
}
