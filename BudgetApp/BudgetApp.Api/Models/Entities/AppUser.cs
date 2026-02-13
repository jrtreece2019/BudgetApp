using Microsoft.AspNetCore.Identity;

namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// Extends the built-in IdentityUser with app-specific fields.
/// IdentityUser already gives us: Id, UserName, Email, PasswordHash, etc.
/// We just add a CreatedAt timestamp so we know when the user signed up.
/// </summary>
public class AppUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
