using BudgetApp.Api.Data;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Models.Entities;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Controllers;

/// <summary>
/// Handles user registration, login, and token refresh.
///
/// FLOW:
/// 1. User registers → POST /api/auth/register → gets back AccessToken + RefreshToken
/// 2. User logs in   → POST /api/auth/login    → gets back AccessToken + RefreshToken
/// 3. Token expires  → POST /api/auth/refresh   → sends RefreshToken, gets new pair
///
/// The AccessToken (JWT) is sent in the Authorization header for all other API calls:
///   Authorization: Bearer eyJhbGciOi...
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<AppUser> userManager,
        TokenService tokenService,
        AppDbContext db,
        IConfiguration config)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Creates a new user account and returns auth tokens.
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Check if a user with this email already exists.
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        // Create the Identity user. UserManager handles password hashing automatically
        // (using PBKDF2 by default -- a secure, industry-standard algorithm).
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // Identity returns detailed errors (e.g., "password too weak").
            // We send them back so the client can display them.
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // Generate tokens and return them.
        var response = await CreateAuthResponse(user);
        return Ok(response);
    }

    /// <summary>
    /// Authenticates an existing user and returns auth tokens.
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        // Find the user by email.
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Deliberately vague error message -- don't reveal whether the email exists.
            // This prevents "email enumeration" attacks.
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Check the password. Identity compares the hash, not the plain text.
        var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!validPassword)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var response = await CreateAuthResponse(user);
        return Ok(response);
    }

    /// <summary>
    /// Exchanges an expired AccessToken + valid RefreshToken for a new token pair.
    /// POST /api/auth/refresh
    ///
    /// This uses "token rotation": the old refresh token is revoked and a new one
    /// is issued. This limits the damage if a refresh token is ever stolen.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        // Look up the refresh token in the database.
        var storedToken = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        // Validate: token must exist, not be revoked, and not be expired.
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token." });
        }

        // Revoke the old token (rotation -- it can never be used again).
        storedToken.IsRevoked = true;
        _db.RefreshTokens.Update(storedToken);

        // Issue a fresh pair of tokens.
        var response = await CreateAuthResponse(storedToken.User);
        return Ok(response);
    }

    /// <summary>
    /// Generates a password reset token for the given email.
    /// POST /api/auth/forgot-password
    ///
    /// In development, the reset token is returned in the response for testing.
    /// In production, you would send this token via email instead.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Always return 200 even if the user doesn't exist.
        // This prevents email enumeration attacks.
        if (user == null)
        {
            return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // In development, return the token directly so you can test the flow
        // without needing an email service.
        if (env.IsDevelopment())
        {
            return Ok(new { message = "Reset token generated.", token });
        }

        // TODO: In production, send the token via email here.
        // Example: await _emailService.SendPasswordResetEmail(user.Email, token);

        return Ok(new { message = "If an account with that email exists, a reset link has been sent." });
    }

    /// <summary>
    /// Resets the user's password using a reset token.
    /// POST /api/auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { message = "Invalid reset request." });
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { message = string.Join(" ", errors) });
        }

        return Ok(new { message = "Password has been reset successfully. You can now sign in." });
    }

    /// <summary>
    /// Helper method that generates both tokens and stores the refresh token in the DB.
    /// Used by both Register and Login to avoid duplicating this logic.
    /// </summary>
    private async Task<AuthResponse> CreateAuthResponse(AppUser user)
    {
        // Generate the JWT access token.
        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);

        // Generate a random refresh token and store it in the database.
        var refreshTokenString = _tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(
                double.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7"))
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = expiresAt
        };
    }
}
