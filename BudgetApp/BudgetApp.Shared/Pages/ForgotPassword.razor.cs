using Microsoft.AspNetCore.Components;
using BudgetApp.Shared.Services.Interfaces;

namespace BudgetApp.Shared.Pages;

public partial class ForgotPassword : ComponentBase
{
    [Inject] private IAuthService AuthService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // Step 1: Request reset
    private string Email { get; set; } = string.Empty;

    // Step 2: Reset password
    private bool ShowResetForm { get; set; }
    private string ResetToken { get; set; } = string.Empty;
    private string NewPassword { get; set; } = string.Empty;
    private string ConfirmPassword { get; set; } = string.Empty;

    // UI state
    private bool IsLoading { get; set; }
    private string? Message { get; set; }
    private bool IsSuccess { get; set; }

    private bool IsEmailValid => Email.Contains('@') && Email.Contains('.');
    private bool IsResetValid =>
        !string.IsNullOrWhiteSpace(ResetToken) &&
        NewPassword.Length >= 8 &&
        NewPassword == ConfirmPassword;

    private async Task HandleForgotPassword()
    {
        if (!IsEmailValid) return;

        IsLoading = true;
        Message = null;

        var (error, token) = await AuthService.ForgotPasswordAsync(Email);

        if (error == null)
        {
            IsSuccess = true;

            if (!string.IsNullOrEmpty(token))
            {
                // Development mode: token returned directly for testing.
                // Pre-fill the reset token field so the developer doesn't have to
                // manually copy it.
                ResetToken = token;
                Message = "Reset code generated (dev mode). Enter your new password below.";
            }
            else
            {
                Message = "If an account with that email exists, a reset code has been sent.";
            }

            ShowResetForm = true;
        }
        else
        {
            IsSuccess = false;
            Message = error;
        }

        IsLoading = false;
    }

    private async Task HandleResetPassword()
    {
        if (!IsResetValid) return;

        IsLoading = true;
        Message = null;

        var error = await AuthService.ResetPasswordAsync(Email, ResetToken, NewPassword);

        if (error == null)
        {
            IsSuccess = true;
            Message = "Password reset successfully! Redirecting to sign in...";

            // Navigate to login after a short delay so the user sees the success message.
            await Task.Delay(1500);
            Navigation.NavigateTo("/login");
        }
        else
        {
            IsSuccess = false;
            Message = error;
        }

        IsLoading = false;
    }
}
