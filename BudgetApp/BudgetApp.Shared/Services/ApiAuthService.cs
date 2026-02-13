using System.Net.Http.Json;
using System.Text.Json;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Calls the BudgetApp.Api backend for authentication and manages JWT tokens.
///
/// HOW IT WORKS:
/// 1. User logs in → we send email/password to POST /api/auth/login
/// 2. API returns an AccessToken (JWT, 15 min) + RefreshToken (7 days)
/// 3. We store both tokens in memory
/// 4. When making future API calls, GetAccessTokenAsync() returns the JWT
/// 5. If the JWT is expired, we automatically refresh it using the RefreshToken
///
/// Token storage is in-memory for now. In a future update, MAUI could use
/// SecureStorage and Web could use ProtectedSessionStorage for persistence
/// across app restarts.
/// </summary>
public class ApiAuthService : IAuthService
{
    private readonly HttpClient _http;

    // Token storage (in-memory)
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _expiresAt;
    private string? _userEmail;

    public ApiAuthService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Checks if the user has a token that either hasn't expired yet,
    /// or can be refreshed (has a refresh token).
    /// </summary>
    public bool IsAuthenticated => _accessToken != null && _refreshToken != null;

    public string? UserEmail => _userEmail;

    public async Task<string?> RegisterAsync(string email, string password)
    {
        try
        {
            var request = new RegisterRequest { Email = email, Password = password };

            // HttpClient.PostAsJsonAsync serializes the object to JSON and sends it.
            var response = await _http.PostAsJsonAsync("api/auth/register", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    StoreTokens(result, email);
                    return null; // null = success
                }
                return "Unexpected response from server.";
            }

            // Try to read the error message from the API response body.
            return await ExtractErrorMessage(response);
        }
        catch (HttpRequestException)
        {
            return "Unable to connect to the server. Please check your internet connection.";
        }
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    StoreTokens(result, email);
                    return null; // null = success
                }
                return "Unexpected response from server.";
            }

            return await ExtractErrorMessage(response);
        }
        catch (HttpRequestException)
        {
            return "Unable to connect to the server. Please check your internet connection.";
        }
    }

    public Task LogoutAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _expiresAt = DateTime.MinValue;
        _userEmail = null;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a valid access token, refreshing automatically if expired.
    /// This is the method other services (like the future SyncService) will call
    /// to get a token for their API requests.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken == null || _refreshToken == null)
            return null;

        // If the token hasn't expired yet, return it directly.
        // We subtract 30 seconds as a buffer so we refresh slightly before actual expiry.
        if (DateTime.UtcNow < _expiresAt.AddSeconds(-30))
            return _accessToken;

        // Token is expired (or about to expire) -- try to refresh it.
        return await RefreshTokenAsync();
    }

    /// <summary>
    /// Sends the refresh token to the API to get a fresh access token.
    /// This is called automatically -- the user doesn't need to do anything.
    /// </summary>
    private async Task<string?> RefreshTokenAsync()
    {
        try
        {
            var request = new { RefreshToken = _refreshToken };
            var response = await _http.PostAsJsonAsync("api/auth/refresh", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result != null)
                {
                    _accessToken = result.AccessToken;
                    _refreshToken = result.RefreshToken;
                    _expiresAt = result.ExpiresAt;
                    return _accessToken;
                }
            }

            // Refresh failed -- user needs to log in again.
            await LogoutAsync();
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Stores the tokens from a successful login/register response.
    /// </summary>
    private void StoreTokens(AuthResponse result, string email)
    {
        _accessToken = result.AccessToken;
        _refreshToken = result.RefreshToken;
        _expiresAt = result.ExpiresAt;
        _userEmail = email;
    }

    public async Task<(string? Error, string? Token)> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new { Email = email };
            var response = await _http.PostAsJsonAsync("api/auth/forgot-password", request);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);

                // In development, the API returns the token so we can test the flow.
                string? token = null;
                if (doc.RootElement.TryGetProperty("token", out var tokenProp))
                {
                    token = tokenProp.GetString();
                }

                return (null, token);
            }

            return (await ExtractErrorMessage(response), null);
        }
        catch (HttpRequestException)
        {
            return ("Unable to connect to the server. Please check your internet connection.", null);
        }
    }

    public async Task<string?> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var request = new { Email = email, Token = token, NewPassword = newPassword };
            var response = await _http.PostAsJsonAsync("api/auth/reset-password", request);

            if (response.IsSuccessStatusCode)
            {
                return null; // success
            }

            return await ExtractErrorMessage(response);
        }
        catch (HttpRequestException)
        {
            return "Unable to connect to the server. Please check your internet connection.";
        }
    }

    /// <summary>
    /// Tries to extract a user-friendly error message from an API error response.
    /// The API returns JSON like { "message": "..." } or { "errors": ["..."] }.
    /// </summary>
    private static async Task<string> ExtractErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);

            // Try "message" field first (used by login/register errors).
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString() ?? "An error occurred.";

            // Try "errors" array (used by Identity validation errors).
            if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array)
            {
                var errorMessages = new List<string>();
                foreach (var error in errors.EnumerateArray())
                    errorMessages.Add(error.GetString() ?? "Unknown error.");
                return string.Join(" ", errorMessages);
            }

            return $"Request failed ({(int)response.StatusCode}).";
        }
        catch
        {
            return $"Request failed ({(int)response.StatusCode}).";
        }
    }
}
