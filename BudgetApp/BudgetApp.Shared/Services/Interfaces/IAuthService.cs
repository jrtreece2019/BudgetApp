namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Handles user authentication (register, login, logout, token management).
///
/// Unlike the other services (ICategoryService, etc.) which work with local SQLite,
/// this service talks to the remote BudgetApp.Api over HTTP. It manages JWT tokens
/// that prove the user's identity to the server.
///
/// USAGE IN PAGES:
///   @inject IAuthService AuthService
///   if (!AuthService.IsAuthenticated) { /* show login prompt */ }
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// True if the user is currently logged in (has a valid, non-expired access token).
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The logged-in user's email, or null if not authenticated.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Creates a new user account and logs them in.
    /// Returns null on success, or an error message on failure.
    /// </summary>
    Task<string?> RegisterAsync(string email, string password);

    /// <summary>
    /// Logs in an existing user.
    /// Returns null on success, or an error message on failure.
    /// </summary>
    Task<string?> LoginAsync(string email, string password);

    /// <summary>
    /// Logs the user out by clearing stored tokens.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Gets the current access token for use in API calls.
    /// Automatically refreshes the token if it's expired.
    /// Returns null if the user is not authenticated.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// Requests a password reset for the given email.
    /// Returns null on success, or an error message on failure.
    /// In development, also returns the reset token for testing.
    /// </summary>
    Task<(string? Error, string? Token)> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets the password using a reset token.
    /// Returns null on success, or an error message on failure.
    /// </summary>
    Task<string?> ResetPasswordAsync(string email, string token, string newPassword);
}
