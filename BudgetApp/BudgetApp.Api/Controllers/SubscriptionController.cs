using System.Security.Claims;
using System.Text.Json;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

/// <summary>
/// API endpoints for subscription management and in-app purchase validation.
///
/// ENDPOINTS:
///   GET  /api/subscription/status    → Check if the user has premium
///   POST /api/subscription/validate  → Validate a store receipt and activate premium
///   POST /api/subscription/apple-webhook   → Apple Server-to-Server notifications
///   POST /api/subscription/google-webhook  → Google Play RTDN notifications
///
/// THE FLOW:
/// 1. Client calls GET /status on app open to check if user is premium
/// 2. If not premium, client shows an upgrade button
/// 3. User taps "Upgrade" → mobile OS handles payment
/// 4. Client sends receipt to POST /validate → server validates with store
/// 5. Server responds with updated subscription status
/// 6. Client unlocks premium features
///
/// Store webhooks keep the server in sync even when the app isn't open
/// (renewals, cancellations, etc.).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly SubscriptionService _subscriptionService;

    public SubscriptionController(SubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Returns the current user's subscription status.
    /// The client calls this on app startup and after any purchase.
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<ActionResult<SubscriptionStatusDto>> GetStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var status = await _subscriptionService.GetStatusAsync(userId);
        return Ok(status);
    }

    /// <summary>
    /// Validates a purchase receipt from Apple or Google and activates premium.
    ///
    /// The client sends this immediately after a successful in-app purchase.
    /// If validation succeeds, the server creates/updates the subscription
    /// and returns the new status.
    /// </summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<ActionResult<SubscriptionStatusDto>> ValidateReceipt(
        [FromBody] ValidateReceiptRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrEmpty(request.Store) || string.IsNullOrEmpty(request.ReceiptData))
            return BadRequest(new { message = "Store and receipt data are required." });

        var status = await _subscriptionService.ValidateAndSaveAsync(userId, request);
        return Ok(status);
    }

    /// <summary>
    /// Apple App Store Server Notifications V2.
    /// Apple sends POST requests here when subscription events occur
    /// (renewal, cancellation, billing issue, etc.).
    ///
    /// No [Authorize] — Apple calls this directly.
    ///
    /// SECURITY NOTE: In production, verify the JWT signature using
    /// Apple's public key to ensure the notification is genuine.
    /// </summary>
    [HttpPost("apple-webhook")]
    public async Task<IActionResult> AppleWebhook([FromBody] JsonElement payload)
    {
        // TODO: Verify Apple JWT signature in production.
        await _subscriptionService.HandleAppleNotificationAsync(payload);
        return Ok();
    }

    /// <summary>
    /// Google Play Real-time Developer Notifications via Pub/Sub.
    /// Google sends POST requests here when subscription events occur.
    ///
    /// No [Authorize] — Google calls this directly.
    /// </summary>
    [HttpPost("google-webhook")]
    public async Task<IActionResult> GoogleWebhook([FromBody] JsonElement payload)
    {
        await _subscriptionService.HandleGoogleNotificationAsync(payload);
        return Ok();
    }
}
