using BudgetApp.Services;
using BudgetApp.Shared.Services;
using BudgetApp.Shared.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace BudgetApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the BudgetApp.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            // SQLite database — register via the IDatabaseService interface
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget.db3");
            builder.Services.AddSingleton<IDatabaseService>(new DatabaseService(dbPath));

            // Split service registrations (one per domain concern)
            builder.Services.AddSingleton<ICategoryService, SqliteCategoryService>();
            builder.Services.AddSingleton<ITransactionService, SqliteTransactionService>();
            builder.Services.AddSingleton<IBudgetService, SqliteBudgetService>();
            builder.Services.AddSingleton<IRecurringTransactionService, SqliteRecurringTransactionService>();
            builder.Services.AddSingleton<ISettingsService, SqliteSettingsService>();
            builder.Services.AddSingleton<ISinkingFundService, SqliteSinkingFundService>();
            builder.Services.AddSingleton<ThemeService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
