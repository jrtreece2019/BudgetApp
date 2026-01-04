using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public interface IBudgetService
{
    // Categories
    List<Category> GetCategories();
    Category? GetCategory(int categoryId);
    void AddCategory(Category category);
    void UpdateCategory(Category category);
    void DeleteCategory(int categoryId);
    bool CanDeleteCategory(int categoryId);
    
    // Transactions
    List<Transaction> GetTransactions(int month, int year);
    void AddTransaction(Transaction transaction);
    void UpdateTransaction(Transaction transaction);
    void DeleteTransaction(int transactionId);
    Transaction? GetTransaction(int transactionId);
    
    // Budgets
    List<Budget> GetBudgets(int month, int year);
    decimal GetTotalBudget(int month, int year);
    decimal GetTotalSpent(int month, int year);
    decimal GetSpentByCategory(int categoryId, int month, int year);
    decimal GetBudgetByCategory(int categoryId, int month, int year);
    bool IsBudgetCustom(int categoryId, int month, int year);
    void UpdateBudget(int categoryId, int month, int year, decimal amount);
    void ResetBudgetToDefault(int categoryId, int month, int year);
    
    // Recurring Transactions
    List<RecurringTransaction> GetRecurringTransactions();
    RecurringTransaction? GetRecurringTransaction(int id);
    void AddRecurringTransaction(RecurringTransaction recurring);
    void UpdateRecurringTransaction(RecurringTransaction recurring);
    void DeleteRecurringTransaction(int id);
    void ProcessRecurringTransactions();
}

