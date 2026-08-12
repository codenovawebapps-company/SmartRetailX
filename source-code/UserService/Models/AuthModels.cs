namespace UserService.Models;

public class LoginRequest
{
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public int    UserId  { get; set; }
    public string Email   { get; set; } = "";
    public string Role    { get; set; } = "";
    public string Token   { get; set; } = "";
    public string Message { get; set; } = "";
}
