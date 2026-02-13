using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Models;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

/// <summary>
/// Code-behind for the Upgrade page.
///
/// Shows:
/// - If premium: current subscription details and premium features
/// - If free: upgrade offer with plan selection and subscribe button
///
/// THE PURCHASE FLOW:
/// 1. User selects a plan (monthly or yearly)
/// 2. User taps "Subscribe Now"
/// 3. HandleSubscribe() fires
/// 4. In production, this would:
///    a. Call the native MAUI in-app purchase API (StoreKit for iOS, Google Play Billing for Android)
///    b. The OS shows its payment sheet
///    c. On success, the OS returns a receipt
///    d. We send the receipt to our API via ISubscriptionService.ValidateReceiptAsync()
///    e. The API validates with Apple/Google and activates premium
///
/// For now (sandbox), step 4a-c is simulated — we generate a fake receipt
/// and send it directly to the API. The server auto-accepts sandbox receipts.
///
/// On web, we'd show a "Subscribe in the mobile app" message or integrate
/// Stripe for web payments.
/// </summary>
public partial class Upgrade : ComponentBase
{
    [Inject] private ISubscriptionService SubscriptionService { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private SubscriptionStatus Status { get; set; } = new();
    private List<SubscriptionPlan> Plans { get; set; } = new();
    private SubscriptionPlan? SelectedPlan { get; set; }
    private bool IsProcessing { get; set; }
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!AuthService.IsAuthenticated)
        {
            Navigation.NavigateTo("/login");
            return;
        }

        Plans = SubscriptionService.GetAvailablePlans();

        // Pre-select the popular plan.
        SelectedPlan = Plans.FirstOrDefault(p => p.IsPopular) ?? Plans.FirstOrDefault();

        // Fetch latest subscription status from the server.
        Status = await SubscriptionService.RefreshStatusAsync();
    }

    private void SelectPlan(SubscriptionPlan plan)
    {
        SelectedPlan = plan;
    }

    /// <summary>
    /// Handles the subscribe button click.
    ///
    /// In production, this would call into the platform-specific in-app
    /// purchase APIs (MAUI's StoreKit / Google Play Billing wrappers).
    ///
    /// For development/sandbox, we simulate a purchase by sending a
    /// fake receipt directly to the server, which auto-accepts it.
    /// </summary>
    private async Task HandleSubscribe()
    {
        if (SelectedPlan == null || IsProcessing) return;

        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            // ──────────────────────────────────────────────────────────
            // TODO: Replace this block with real in-app purchase code.
            //
            // MAUI iOS:
            //   var result = await StoreKit.PurchaseAsync(SelectedPlan.ProductId);
            //   if (result.Success)
            //       await SubscriptionService.ValidateReceiptAsync(
            //           "apple", SelectedPlan.ProductId, result.Receipt);
            //
            // MAUI Android:
            //   var result = await GooglePlayBilling.PurchaseAsync(SelectedPlan.ProductId);
            //   if (result.Success)
            //       await SubscriptionService.ValidateReceiptAsync(
            //           "google", SelectedPlan.ProductId, result.PurchaseToken, result.OrderId);
            // ──────────────────────────────────────────────────────────

            // SANDBOX: Simulate a successful purchase.
            Status = await SubscriptionService.ValidateReceiptAsync(
                "apple",  // Simulating an Apple purchase
                SelectedPlan.ProductId,
                $"sandbox_receipt_{Guid.NewGuid():N}",
                $"sandbox_txn_{Guid.NewGuid():N}");

            if (!Status.IsPremium)
            {
                ErrorMessage = "Something went wrong. Please try again.";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "Purchase failed. Please try again.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }
}
