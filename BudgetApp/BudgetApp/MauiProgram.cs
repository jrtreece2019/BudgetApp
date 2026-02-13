using System.Reflection;
using BudgetApp.Services;
using BudgetApp.Shared.Services;
using BudgetApp.Shared.Services.Interfaces;
using Microsoft.Extensions.Configuration;
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

            // Load configuration from embedded appsettings.json (Resources/Raw)
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);

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
            builder.Services.AddSingleton<IExportService, ExportService>();

            // Backend API client — used by IAuthService (and future ISyncService) to call BudgetApp.Api.
            // The base URL is read from appsettings.json (Resources/Raw/appsettings.json).
            var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7012/";
            builder.Services.AddHttpClient("BudgetApi", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });
            builder.Services.AddSingleton<IAuthService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new ApiAuthService(factory.CreateClient("BudgetApi"));
            });
            builder.Services.AddSingleton<ISyncService>(sp =>
            {
                var db = sp.GetRequiredService<IDatabaseService>();
                var auth = sp.GetRequiredService<IAuthService>();
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new BudgetApp.Shared.Services.SyncService(db, auth, factory.CreateClient("BudgetApi"));
            });
            builder.Services.AddSingleton<IBankConnectionService>(sp =>
            {
                var auth = sp.GetRequiredService<IAuthService>();
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new ApiBankConnectionService(factory.CreateClient("BudgetApi"), auth);
            });
            builder.Services.AddSingleton<ISubscriptionService>(sp =>
            {
                var auth = sp.GetRequiredService<IAuthService>();
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new ApiSubscriptionService(factory.CreateClient("BudgetApi"), auth);
            });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
