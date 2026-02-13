namespace BudgetApp.Shared.Models;

/// <summary>
/// DTOs (Data Transfer Objects) for communicating with the backend Auth API.
/// These are plain classes that define the shape of JSON sent/received over HTTP.
/// They mirror the server-side DTOs in BudgetApp.Api.Models.DTOs.
/// </summary>

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned by the API after successful login/register.
/// The client stores these tokens and uses them for authenticated API calls.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
