using System.Security.Claims;
using BudgetApp.Api.Models.DTOs;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

/// <summary>
/// API endpoints for Plaid bank connection management.
///
/// ENDPOINTS:
///   POST /api/plaid/link-token      → Get a Plaid Link token (start bank connection flow)
///   POST /api/plaid/exchange-token   → Exchange public token for access token (finish flow)
///   GET  /api/plaid/banks            → List connected banks and accounts
///   DELETE /api/plaid/banks/{id}     → Disconnect a bank
///   GET  /api/plaid/transactions     → Get unprocessed imported transactions
///   POST /api/plaid/process          → Convert imported transactions to budget transactions
///   POST /api/plaid/webhook          → Receive Plaid webhook notifications (no auth)
///
/// All endpoints except webhook require JWT authentication.
/// The webhook endpoint is unauthenticated because Plaid calls it directly.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PlaidController : ControllerBase
{
    private readonly PlaidService _plaidService;
    private readonly SubscriptionService _subscriptionService;

    public PlaidController(PlaidService plaidService, SubscriptionService subscriptionService)
    {
        _plaidService = plaidService;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Step 1 of bank connection: get a link token for Plaid Link UI.
    /// REQUIRES PREMIUM — this is the paywall gate. If the user doesn't
    /// have an active subscription, they get a 403 Forbidden response.
    /// </summary>
    [HttpPost("link-token")]
    [Authorize]
    public async Task<ActionResult<CreateLinkTokenResponse>> CreateLinkToken()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // ── PAYWALL GATE ──────────────────────────────────────────
        if (!await _subscriptionService.IsPremiumAsync(userId))
            return StatusCode(403, new { message = "Premium subscription required to connect a bank account." });

        // Pass the user's email so Plaid can skip email verification too.
        var userEmail = User.FindFirstValue(ClaimTypes.Email);
        var linkToken = await _plaidService.CreateLinkTokenAsync(userId, userEmail);
        if (linkToken == null)
            return StatusCode(502, new { message = "Failed to create Plaid link token." });

        return Ok(new CreateLinkTokenResponse { LinkToken = linkToken });
    }

    /// <summary>
    /// Step 2 of bank connection: exchange the public token from Plaid Link.
    /// After the user selects their bank and logs in, Plaid Link returns a
    /// temporary public token. We exchange it for a permanent access token.
    /// </summary>
    [HttpPost("exchange-token")]
    [Authorize]
    public async Task<ActionResult<ExchangePublicTokenResponse>> ExchangePublicToken(
        [FromBody] ExchangePublicTokenRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var result = await _plaidService.ExchangePublicTokenAsync(userId, request.PublicToken);
        if (result == null)
            return StatusCode(502, new { message = "Failed to exchange Plaid token." });

        return Ok(result);
    }

    /// <summary>
    /// Returns all connected banks and their accounts for the current user.
    /// </summary>
    [HttpGet("banks")]
    [Authorize]
    public async Task<ActionResult<List<ConnectedBankDto>>> GetConnectedBanks()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var banks = await _plaidService.GetConnectedBanksAsync(userId);
        return Ok(banks);
    }

    /// <summary>
    /// Disconnects a bank and removes all its data.
    /// </summary>
    [HttpDelete("banks/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DisconnectBank(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var success = await _plaidService.DisconnectBankAsync(userId, id);
        if (!success) return NotFound(new { message = "Bank connection not found." });

        return NoContent();
    }

    /// <summary>
    /// Returns imported transactions that haven't been categorized yet.
    /// The client shows these in a "Review Transactions" screen.
    /// </summary>
    [HttpGet("transactions")]
    [Authorize]
    public async Task<ActionResult<List<ImportedTransactionDto>>> GetUnprocessedTransactions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var transactions = await _plaidService.GetUnprocessedTransactionsAsync(userId);
        return Ok(transactions);
    }

    /// <summary>
    /// Processes all unprocessed imported transactions: converts them into
    /// regular budget Transactions that will be synced to the client.
    /// </summary>
    [HttpPost("process")]
    [Authorize]
    public async Task<ActionResult> ProcessTransactions()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var count = await _plaidService.ProcessImportedTransactionsAsync(userId);
        return Ok(new { processed = count });
    }

    /// <summary>
    /// Plaid webhook endpoint. Plaid POSTs here when events occur
    /// (new transactions available, connection errors, etc.).
    ///
    /// NO [Authorize] -- Plaid calls this directly, not through our client.
    ///
    /// SECURITY NOTE: In production, you should verify the webhook signature
    /// using Plaid's webhook verification to ensure the request actually
    /// came from Plaid. See: https://plaid.com/docs/api/webhooks/webhook-verification/
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] PlaidWebhookPayload payload)
    {
        // TODO: Verify webhook signature in production.
        await _plaidService.HandleWebhookAsync(payload);
        return Ok();
    }
}
