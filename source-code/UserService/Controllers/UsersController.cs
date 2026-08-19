using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserDbContext _db;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserDbContext db, ILogger<UsersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/users
    /// Returns all registered users (excluding password hashes).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAllUsers()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.CreatedAt,
                u.UpdatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// GET /api/v1/users/{id}
    /// Retrieves a single user profile by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetUserById(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// POST /api/v1/users
    /// Creates a new user.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreateUser([FromBody] User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Name))
        {
            return BadRequest(new { message = "Name and email are required." });
        }

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower());
        if (exists)
        {
            return Conflict(new { message = $"User with email '{user.Email}' already exists." });
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            user.PasswordHash = UserDbContext.HashPassword("Default@123");
        }

        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// PUT /api/v1/users/{id}
    /// Updates an existing user's information.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> UpdateUser(int id, [FromBody] User updatedUser)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Name))
        {
            user.Name = updatedUser.Name;
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Email) && updatedUser.Email != user.Email)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == updatedUser.Email.ToLower() && u.Id != id);
            if (emailExists)
            {
                return Conflict(new { message = $"Email '{updatedUser.Email}' is already in use." });
            }
            user.Email = updatedUser.Email.Trim().ToLower();
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Role))
        {
            user.Role = updatedUser.Role;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        });
    }

    /// <summary>
    /// DELETE /api/v1/users/{id}
    /// Removes a user by ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"User with ID {id} successfully deleted." });
    }
}
