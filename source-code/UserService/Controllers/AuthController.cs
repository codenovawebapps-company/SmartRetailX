using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using System.Collections.Concurrent;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    // Shared in-memory user store (same as UsersController)
    private static readonly ConcurrentDictionary<int, User> _users =
        UsersController.Users;

    /// <summary>
    /// POST /api/v1/auth/login
    /// Validates email + password and returns a stub token.
    /// </summary>
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        // Find user by email (case-insensitive)
        var user = _users.Values
            .FirstOrDefault(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Stub token — replace with JWT in production
        var token = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Email}:{DateTime.UtcNow:O}"));

        return Ok(new LoginResponse
        {
            UserId  = user.Id,
            Email   = user.Email,
            Role    = user.Role,
            Token   = token,
            Message = "Login successful."
        });
    }
}
