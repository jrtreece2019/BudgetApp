using System.Security.Claims;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

/// <summary>
/// The sync endpoint. This is the single API the client calls to exchange data.
///
/// POST /api/sync
///   - Requires authentication (JWT Bearer token)
///   - Body: SyncRequest { LastSyncedAt, ClientChanges }
///   - Returns: SyncResponse { ServerChanges, SyncedAt }
///
/// The client calls this:
///   1. On app open (to pull any changes from other devices)
///   2. After local changes (to push them to the server)
///   3. Periodically on a timer (to stay in sync)
///
/// The [Authorize] attribute ensures only logged-in users can sync.
/// The UserId comes from the JWT claims (set by TokenService).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly SyncService _syncService;

    public SyncController(SyncService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost]
    public async Task<ActionResult<SyncResponse>> Sync([FromBody] SyncRequest request)
    {
        // Extract the user's ID from the JWT. This was set as the "sub" claim
        // in TokenService.GenerateAccessToken().
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var response = await _syncService.ProcessSyncAsync(userId, request);
        return Ok(response);
    }
}
