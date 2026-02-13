using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Services;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class Settings : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private ISyncService SyncService { get; set; } = default!;
    [Inject] private ISubscriptionService SubscriptionService { get; set; } = default!;
    [Inject] private ThemeService ThemeService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Change password state
    private bool ShowChangePassword { get; set; }
    private string NewPassword { get; set; } = string.Empty;
    private string ConfirmPassword { get; set; } = string.Empty;
    private bool IsChangingPassword { get; set; }
    private string? PasswordMessage { get; set; }
    private bool PasswordSuccess { get; set; }

    private bool IsPasswordValid =>
        NewPassword.Length >= 8 &&
        NewPassword == ConfirmPassword;

    private void GoBack()
    {
        Navigation.NavigateTo("/");
    }

    private void GoToLogin()
    {
        Navigation.NavigateTo("/login");
    }

    private void GoToUpgrade()
    {
        Navigation.NavigateTo("/upgrade");
    }

    private void GoToConnectBank()
    {
        Navigation.NavigateTo("/connect-bank");
    }

    private void ToggleTheme()
    {
        ThemeService.ToggleTheme();
    }

    private void ToggleChangePassword()
    {
        ShowChangePassword = !ShowChangePassword;
        if (!ShowChangePassword)
        {
            // Reset form when collapsing
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            PasswordMessage = null;
        }
    }

    private async Task ChangePassword()
    {
        if (!IsPasswordValid) return;

        IsChangingPassword = true;
        PasswordMessage = null;

        try
        {
            // Re-register with the same email overwrites the password on the server.
            // A dedicated change-password endpoint would be better, but this works
            // with the existing API surface. The user is already authenticated so
            // the AuthService can attach their token.
            var error = await AuthService.RegisterAsync(AuthService.UserEmail!, NewPassword);
            if (error == null)
            {
                PasswordSuccess = true;
                PasswordMessage = "Password updated successfully.";
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                PasswordSuccess = false;
                PasswordMessage = error;
            }
        }
        catch
        {
            PasswordSuccess = false;
            PasswordMessage = "Failed to update password. Please try again.";
        }
        finally
        {
            IsChangingPassword = false;
        }
    }

    private async Task HandleLogout()
    {
        SyncService.StopAutoSync();
        await AuthService.LogoutAsync();
        Navigation.NavigateTo("/login");
    }

    private async Task ManualSync()
    {
        if (SyncService.IsSyncing) return;

        await SyncService.SyncAsync();
        StateHasChanged();
    }

    private string FormatLastSynced()
    {
        if (SyncService.LastSyncedAt == DateTime.MinValue)
        {
            return "Never";
        }

        var elapsed = DateTime.UtcNow - SyncService.LastSyncedAt;

        if (elapsed.TotalMinutes < 1) return "Just now";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} min ago";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} hr ago";
        return SyncService.LastSyncedAt.ToLocalTime().ToString("MMM d, h:mm tt");
    }
}
