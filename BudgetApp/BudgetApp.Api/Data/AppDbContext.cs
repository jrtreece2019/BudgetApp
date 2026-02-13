using BudgetApp.Api.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Data;

/// <summary>
/// The EF Core DbContext for the entire API. Inherits from IdentityDbContext
/// which automatically creates the ASP.NET Identity tables (AspNetUsers,
/// AspNetRoles, etc.) alongside our custom domain tables.
///
/// Each DbSet below becomes a table in PostgreSQL.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Domain tables
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<SinkingFund> SinkingFunds => Set<SinkingFund>();
    public DbSet<SinkingFundTransaction> SinkingFundTransactions => Set<SinkingFundTransaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Plaid integration tables
    public DbSet<PlaidItem> PlaidItems => Set<PlaidItem>();
    public DbSet<PlaidAccount> PlaidAccounts => Set<PlaidAccount>();
    public DbSet<ImportedTransaction> ImportedTransactions => Set<ImportedTransaction>();

    // Subscription / paywall
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    /// <summary>
    /// OnModelCreating is where we configure table relationships, indexes, and
    /// constraints using the Fluent API. This is like defining your database schema.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // IMPORTANT: Must call base to set up Identity tables.
        base.OnModelCreating(builder);

        // ── Category ──────────────────────────────────────────────
        builder.Entity<Category>(e =>
        {
            // Composite unique index: each user's SyncIds must be unique.
            // This prevents duplicate records during sync.
            e.HasIndex(c => new { c.UserId, c.SyncId }).IsUnique();

            // Index on UserId for fast "get all categories for this user" queries.
            e.HasIndex(c => c.UserId);

            // Store the enum as an integer in PostgreSQL.
            e.Property(c => c.Type).HasConversion<int>();

            // Set up the FK relationship to AppUser.
            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Transaction ───────────────────────────────────────────
        builder.Entity<Transaction>(e =>
        {
            e.HasIndex(t => new { t.UserId, t.SyncId }).IsUnique();
            e.HasIndex(t => t.UserId);
            e.Property(t => t.Type).HasConversion<int>();

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // FK to Category. Restrict delete so you can't delete a category
            // that has transactions (mirrors CanDeleteCategory logic).
            e.HasOne(t => t.Category)
             .WithMany()
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Budget ────────────────────────────────────────────────
        builder.Entity<Budget>(e =>
        {
            e.HasIndex(b => new { b.UserId, b.SyncId }).IsUnique();
            e.HasIndex(b => b.UserId);

            e.HasOne(b => b.User)
             .WithMany()
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(b => b.Category)
             .WithMany()
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RecurringTransaction ──────────────────────────────────
        builder.Entity<RecurringTransaction>(e =>
        {
            e.HasIndex(r => new { r.UserId, r.SyncId }).IsUnique();
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Type).HasConversion<int>();
            e.Property(r => r.Frequency).HasConversion<int>();

            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Category)
             .WithMany()
             .HasForeignKey(r => r.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── UserSettings ──────────────────────────────────────────
        builder.Entity<UserSettings>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.SyncId }).IsUnique();
            // One settings row per user.
            e.HasIndex(s => s.UserId).IsUnique();

            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SinkingFund ───────────────────────────────────────────
        builder.Entity<SinkingFund>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.SyncId }).IsUnique();
            e.HasIndex(f => f.UserId);
            e.Property(f => f.Status).HasConversion<int>();

            e.HasOne(f => f.User)
             .WithMany()
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SinkingFundTransaction ────────────────────────────────
        builder.Entity<SinkingFundTransaction>(e =>
        {
            e.HasIndex(t => new { t.UserId, t.SyncId }).IsUnique();
            e.HasIndex(t => t.SinkingFundId);
            e.Property(t => t.Type).HasConversion<int>();

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.SinkingFund)
             .WithMany()
             .HasForeignKey(t => t.SinkingFundId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RefreshToken ──────────────────────────────────────────
        builder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(r => r.Token).IsUnique();
            e.HasIndex(r => r.UserId);

            e.HasOne(r => r.User)
             .WithMany()
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlaidItem ───────────────────────────────────────────
        builder.Entity<PlaidItem>(e =>
        {
            // Each Plaid item_id should be unique in our system.
            e.HasIndex(p => p.PlaidItemId).IsUnique();
            e.HasIndex(p => p.UserId);

            e.HasOne(p => p.User)
             .WithMany()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // One PlaidItem has many PlaidAccounts.
            e.HasMany(p => p.Accounts)
             .WithOne(a => a.Item)
             .HasForeignKey(a => a.PlaidItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlaidAccount ────────────────────────────────────────
        builder.Entity<PlaidAccount>(e =>
        {
            e.HasIndex(a => a.PlaidAccountId).IsUnique();
            e.HasIndex(a => a.UserId);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // Optional FK to Category for auto-categorization.
            e.HasOne(a => a.DefaultCategory)
             .WithMany()
             .HasForeignKey(a => a.DefaultCategoryId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── ImportedTransaction ─────────────────────────────────
        builder.Entity<ImportedTransaction>(e =>
        {
            // Plaid transaction IDs are unique per user.
            e.HasIndex(t => new { t.UserId, t.PlaidTransactionId }).IsUnique();
            e.HasIndex(t => t.UserId);
            e.HasIndex(t => t.PlaidAccountId);

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.PlaidAccount)
             .WithMany()
             .HasForeignKey(t => t.PlaidAccountId)
             .OnDelete(DeleteBehavior.Cascade);

            // Optional FK to the budget Transaction it was converted into.
            e.HasOne(t => t.LinkedTransaction)
             .WithMany()
             .HasForeignKey(t => t.LinkedTransactionId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // ── Subscription ────────────────────────────────────────
        builder.Entity<Subscription>(e =>
        {
            e.HasIndex(s => s.UserId);

            // Store transaction ID should be unique (one purchase = one record).
            e.HasIndex(s => s.StoreTransactionId).IsUnique();

            e.Property(s => s.Status).HasConversion<int>();

            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
