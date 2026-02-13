using System.Text;
using BudgetApp.Api.Data;
using BudgetApp.Api.Models.Entities;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────
// Register the EF Core DbContext with the PostgreSQL provider.
// The connection string comes from appsettings.json (or appsettings.Development.json).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Identity ──────────────────────────────────────────────────
// This sets up the user management system: password hashing, user storage,
// validation rules, etc. It uses our AppDbContext to store users in PostgreSQL.
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Password rules -- strong but not annoying.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // Don't force special characters
    options.Password.RequiredLength = 8;

    // Require email to be unique.
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────────────
// Tell ASP.NET Core how to validate incoming JWT tokens on every request.
// When a request comes in with "Authorization: Bearer <token>", this middleware
// automatically validates the signature, checks expiry, and sets the User principal.
builder.Services.AddAuthentication(options =>
{
    // These two lines tell the app: "use JWT as the default way to authenticate."
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Validate the signing key (ensures token wasn't tampered with).
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

        // Validate the issuer (who created the token).
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        // Validate the audience (who the token is for).
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // Validate the expiry time.
        ValidateLifetime = true,

        // Zero tolerance on expiry (no grace period).
        ClockSkew = TimeSpan.Zero
    };
});

// ── Custom Services ───────────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SyncService>();

// PlaidService needs an HttpClient to call the Plaid API.
// AddHttpClient<PlaidService> creates a named HttpClient that DI injects automatically.
builder.Services.AddHttpClient<PlaidService>();

// SubscriptionService validates receipts with Apple/Google via HttpClient.
builder.Services.AddHttpClient<SubscriptionService>();

// ── Controllers ───────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── CORS ──────────────────────────────────────────────────────────────
// Cross-Origin Resource Sharing: allows your Blazor Web client (running on
// a different port) to call this API. Without this, the browser blocks the request.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()   // In production, restrict to your actual domain
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────
// The order matters! Each request flows through these in sequence.

app.UseHttpsRedirection();

// CORS must come before auth.
app.UseCors("AllowClient");

// Authentication (who are you?) must come before Authorization (are you allowed?).
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes (e.g., [Route("api/[controller]")] on AuthController
// becomes /api/auth).
app.MapControllers();

// ── Auto-Migrate Database ─────────────────────────────────────────────
// In development, automatically apply pending EF Core migrations on startup.
// In production, you'd run migrations as a separate deployment step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
