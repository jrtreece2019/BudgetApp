using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

/// <summary>
/// A simple "who am I?" endpoint that requires authentication.
/// Useful for testing that JWT auth is working correctly.
///
/// The [Authorize] attribute means: "reject this request with 401 Unauthorized
/// unless the caller provides a valid JWT in the Authorization header."
///
/// If the JWT is valid, ASP.NET Core automatically populates User.Claims
/// with the claims we baked into the token (sub, email, jti).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    /// <summary>
    /// Returns the current user's ID and email from their JWT claims.
    /// GET /api/me
    /// </summary>
    [HttpGet]
    public IActionResult GetCurrentUser()
    {
        // ClaimTypes.NameIdentifier maps to the "sub" claim we set in TokenService.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new { userId, email });
    }
}
