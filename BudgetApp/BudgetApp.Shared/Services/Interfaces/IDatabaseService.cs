using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services.Interfaces;

/// <summary>
/// Pure data-access layer for SQLite operations.
/// Contains no business logic — only CRUD and simple queries.
/// </summary>
public interface IDatabaseService
{
    // Categories
    List<Category> GetCategories();
    Category? GetCategory(int categoryId);
    void AddCategory(Category category);
    void UpdateCategory(Category category);
    void DeleteCategory(int categoryId);
    bool HasTransactionsForCategory(int categoryId);
    bool HasRecurringTransactionsForCategory(int categoryId);

    // Transactions
    List<Transaction> GetTransactions(int month, int year);
    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction transaction);
    void DeleteTransaction(int transactionId);
    Transaction? GetTransaction(int transactionId);

    // Budgets
    List<Budget> GetBudgets(int month, int year);
    Budget? GetBudget(int categoryId, int month, int year);
    void UpsertBudget(int categoryId, int month, int year, decimal amount);
    void DeleteBudget(int categoryId, int month, int year);

    // Recurring Transactions
    List<RecurringTransaction> GetRecurringTransactions();
    RecurringTransaction? GetRecurringTransaction(int id);
    void AddRecurringTransaction(RecurringTransaction recurring);
    void UpdateRecurringTransaction(RecurringTransaction recurring);
    void DeleteRecurringTransaction(int id);

    // User Settings
    UserSettings GetSettings();
    void UpdateSettings(UserSettings settings);

    // Sinking Funds
    List<SinkingFund> GetSinkingFunds();
    SinkingFund? GetSinkingFund(int id);
    void AddSinkingFund(SinkingFund fund);
    void UpdateSinkingFund(SinkingFund fund);
    void DeleteSinkingFund(int id);

    // Sinking Fund Transactions
    List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId);
    SinkingFundTransaction? GetSinkingFundTransaction(int transactionId);
    void AddSinkingFundTransaction(SinkingFundTransaction transaction);
    void DeleteSinkingFundTransaction(int transactionId);
    decimal GetTotalSinkingFundContributions(int month, int year);
}
