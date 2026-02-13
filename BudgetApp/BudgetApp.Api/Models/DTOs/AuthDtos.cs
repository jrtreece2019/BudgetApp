using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Api.Models.DTOs;

/// <summary>
/// Sent by the client when a new user creates an account.
/// DataAnnotations ([Required], [EmailAddress], etc.) provide automatic
/// validation -- ASP.NET Core returns 400 Bad Request if these fail.
/// </summary>
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Sent by the client when an existing user logs in.
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Returned to the client after successful login or registration.
/// Contains everything the client needs to make authenticated API calls.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Short-lived JWT (15 min). Sent in the Authorization header for API calls.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Long-lived refresh token (7 days). Used to get a new AccessToken
    /// without re-entering the password.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// When the AccessToken expires. The client can use this to proactively
    /// refresh before the token expires, avoiding failed API calls.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Sent by the client when the AccessToken has expired and they need a new one.
/// </summary>
public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Sent by the client to request a password reset.
/// The server generates a reset token; in development mode the token is returned
/// in the response so it can be tested without email. In production, it would
/// be sent via email.
/// </summary>
public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Sent by the client with the reset token and the new password.
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string NewPassword { get; set; } = string.Empty;
}
