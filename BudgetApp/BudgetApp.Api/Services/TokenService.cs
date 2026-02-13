using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BudgetApp.Api.Models.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BudgetApp.Api.Services;

/// <summary>
/// Handles JWT access token and refresh token generation.
///
/// HOW JWT WORKS (simplified):
/// 1. A JWT is a signed string with three parts: Header.Payload.Signature
/// 2. The Payload contains "claims" -- pieces of info like UserId, Email, expiry time
/// 3. The Signature proves the token wasn't tampered with (signed with our secret key)
/// 4. The client sends this token in every API request
/// 5. The server verifies the signature and reads the claims -- no database lookup needed
///
/// This is why JWTs are fast: the server doesn't need to hit the database on every request
/// to check "is this user logged in?" The signed token IS the proof.
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Creates a signed JWT access token containing the user's ID and email as claims.
    /// </summary>
    public (string token, DateTime expiresAt) GenerateAccessToken(AppUser user)
    {
        // Claims are key-value pairs baked into the token.
        // The client (and server middleware) can read these without a database call.
        var claims = new List<Claim>
        {
            // "sub" (subject) is the standard JWT claim for "who is this token for?"
            new(JwtRegisteredClaimNames.Sub, user.Id),
            // "email" claim so the client can display the user's email.
            new(JwtRegisteredClaimNames.Email, user.Email!),
            // "jti" (JWT ID) is a unique ID for this specific token.
            // Useful for revocation if needed.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // The signing key is a secret only our server knows.
        // Anyone with this key could forge tokens, so it must stay in appsettings (or Key Vault).
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        // HmacSha256 is the algorithm used to create the signature.
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(
            double.Parse(_config["Jwt:AccessTokenExpiryMinutes"] ?? "15"));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>
    /// Creates a cryptographically random refresh token.
    /// Unlike JWTs, refresh tokens are opaque strings -- they have no meaning on their own.
    /// They're just a random key that maps to a database row.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
