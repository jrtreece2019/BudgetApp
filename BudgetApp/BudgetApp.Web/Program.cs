using BudgetApp.Shared.Services;
using BudgetApp.Shared.Services.Interfaces;
using BudgetApp.Web.Components;
using BudgetApp.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the BudgetApp.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// SQLite database — register via the IDatabaseService interface
var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BudgetApp", "budget.db3");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
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
// The base URL should match where the API is running (check BudgetApp.Api/Properties/launchSettings.json).
builder.Services.AddHttpClient("BudgetApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7012/");
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(BudgetApp.Shared._Imports).Assembly);

app.Run();
