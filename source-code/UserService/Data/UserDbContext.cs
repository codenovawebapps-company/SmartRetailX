using Microsoft.EntityFrameworkCore;
using UserService.Models;
using System.Security.Cryptography;
using System.Text;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
        });
    }

    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    public void SeedDefaultUsers()
    {
        Database.EnsureCreated();

        if (!Users.Any())
        {
            Users.AddRange(
                new User
                {
                    Name = "Admin User",
                    Email = "admin@smartretailx.com",
                    PasswordHash = HashPassword("Admin@123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Name = "Jane Doe",
                    Email = "jane@example.com",
                    PasswordHash = HashPassword("secret123"),
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Name = "Alice Smith",
                    Email = "alice@example.com",
                    PasswordHash = HashPassword("alice123"),
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
            SaveChanges();
        }
    }
}
