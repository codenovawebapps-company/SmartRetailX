using Microsoft.AspNetCore.Mvc;
using UserService.Models;
using System.Collections.Concurrent;

namespace UserService.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    // Public so AuthController can share the same in-memory store
    public static readonly ConcurrentDictionary<int, User> Users = new();
    private static readonly ConcurrentDictionary<int, User> _users = Users;
    private static int _nextId = 1;

    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] User user)
    {
        if (user.Id <= 0)
        {
            user.Id = Interlocked.Increment(ref _nextId);
        }
        else
        {
            int currentId;
            do
            {
                currentId = _nextId;
                if (user.Id < currentId) break;
            } while (Interlocked.CompareExchange(ref _nextId, user.Id + 1, currentId) != currentId);
        }

        _users[user.Id] = user;
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpGet("{id}")]
    public ActionResult<User> GetUserById(int id)
    {
        if (_users.TryGetValue(id, out var user))
        {
            return Ok(user);
        }
        return NotFound(new { message = $"User with ID {id} not found." });
    }
}
