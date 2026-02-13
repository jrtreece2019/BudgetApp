using System.Net.Http.Json;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Services;

/// <summary>
/// Implementation of ISubscriptionService that talks to the BudgetApp.Api.
///
/// Follows the same pattern as the other Api*Service classes:
/// - Gets a JWT from IAuthService before each call
/// - Attaches it as a Bearer token
/// - Handles errors gracefully (returns default state if offline)
///
/// The CurrentStatus is cached in memory so pages don't have to
/// call the server every time they check IsPremium. Call RefreshStatusAsync()
/// on app startup to ensure it's up-to-date.
/// </summary>
public class ApiSubscriptionService : ISubscriptionService
{
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    public ApiSubscriptionService(HttpClient http, IAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public SubscriptionStatus CurrentStatus { get; private set; } = new();

    public bool IsPremium => CurrentStatus.IsPremium;

    public async Task<SubscriptionStatus> RefreshStatusAsync()
    {
        if (!await SetAuthHeaderAsync())
        {
            CurrentStatus = new SubscriptionStatus();
            return CurrentStatus;
        }

        try
        {
            var result = await _http.GetFromJsonAsync<SubscriptionStatus>(
                "api/subscription/status");
            CurrentStatus = result ?? new SubscriptionStatus();
        }
        catch (HttpRequestException)
        {
            // Offline — keep the last known status.
        }

        return CurrentStatus;
    }

    public async Task<SubscriptionStatus> ValidateReceiptAsync(
        string store, string productId, string receiptData, string? transactionId = null)
    {
        if (!await SetAuthHeaderAsync())
            return new SubscriptionStatus();

        try
        {
            var body = new
            {
                Store = store,
                ProductId = productId,
                ReceiptData = receiptData,
                TransactionId = transactionId
            };

            var response = await _http.PostAsJsonAsync("api/subscription/validate", body);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubscriptionStatus>();
                CurrentStatus = result ?? new SubscriptionStatus();
            }
        }
        catch (HttpRequestException)
        {
            // Offline — the receipt will need to be re-sent later.
        }

        return CurrentStatus;
    }

    public List<SubscriptionPlan> GetAvailablePlans()
    {
        // These must match what you configure in App Store Connect / Google Play Console.
        // Update the prices here when you set them in the stores.
        return new List<SubscriptionPlan>
        {
            new()
            {
                ProductId = "com.budgetapp.premium.monthly",
                Name = "Monthly",
                Price = "$4.99",
                Period = "month",
                IsPopular = false
            },
            new()
            {
                ProductId = "com.budgetapp.premium.yearly",
                Name = "Yearly",
                Price = "$39.99",
                Period = "year",
                Savings = "Save 33%",
                IsPopular = true
            }
        };
    }

    private async Task<bool> SetAuthHeaderAsync()
    {
        var token = await _auth.GetAccessTokenAsync();
        if (token == null) return false;

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return true;
    }
}
