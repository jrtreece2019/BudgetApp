namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Stores refresh tokens in the database. A refresh token lets the client
/// get a new JWT without re-entering their password. We store them server-side
/// so we can:
///   1. Revoke them (user logs out or changes password)
///   2. Rotate them (each use generates a new one, invalidating the old)
///   3. Expire them (7-day lifetime)
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>
    /// The actual token string (a random Base64 value).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Which user this token belongs to (FK to AppUser).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Set to true when the token is used (rotated) or explicitly revoked.
    /// A revoked token can never be used again.
    /// </summary>
    public bool IsRevoked { get; set; }

    // Navigation property -- EF Core uses this to set up the FK relationship.
    public AppUser User { get; set; } = null!;
}
