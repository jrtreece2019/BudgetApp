using BudgetApp.Shared.Models;

namespace BudgetApp.Shared.Services;

public class SqliteBudgetService : IBudgetService
{
    private readonly DatabaseService _db;

    public SqliteBudgetService(DatabaseService db)
    {
        _db = db;
    }

    // Categories
    public List<Category> GetCategories()
    {
        return _db.GetCategories();
    }

    public Category? GetCategory(int categoryId)
    {
        return _db.GetCategory(categoryId);
    }

    public void AddCategory(Category category)
    {
        _db.AddCategory(category);
    }

    public void UpdateCategory(Category category)
    {
        _db.UpdateCategory(category);
    }

    public void DeleteCategory(int categoryId)
    {
        _db.DeleteCategory(categoryId);
    }

    public bool CanDeleteCategory(int categoryId)
    {
        // Can't delete if there are transactions or recurring transactions using this category
        return !_db.HasTransactionsForCategory(categoryId) && 
               !_db.HasRecurringTransactionsForCategory(categoryId);
    }

    // Transactions
    public List<Transaction> GetTransactions(int month, int year)
    {
        var transactions = _db.GetTransactions(month, year);
        return transactions.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToList();
    }

    public void AddTransaction(Transaction transaction)
    {
        _db.AddTransaction(transaction);
    }

    public void UpdateTransaction(Transaction transaction)
    {
        _db.UpdateTransaction(transaction);
    }

    public void DeleteTransaction(int transactionId)
    {
        _db.DeleteTransaction(transactionId);
    }

    public Transaction? GetTransaction(int transactionId)
    {
        return _db.GetTransaction(transactionId);
    }

    // Budgets
    public List<Budget> GetBudgets(int month, int year)
    {
        return _db.GetBudgets(month, year);
    }

    public decimal GetTotalBudget(int month, int year)
    {
        // Sum effective budget for each category (custom or default)
        var categories = _db.GetCategories();
        decimal total = 0;
        foreach (var category in categories)
        {
            total += GetBudgetByCategory(category.Id, month, year);
        }
        return total;
    }

    public decimal GetTotalSpent(int month, int year)
    {
        return _db.GetTransactions(month, year)
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetSpentByCategory(int categoryId, int month, int year)
    {
        return _db.GetTransactions(month, year)
            .Where(t => t.CategoryId == categoryId && t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);
    }

    public decimal GetBudgetByCategory(int categoryId, int month, int year)
    {
        // First check for a month-specific custom budget
        var budget = _db.GetBudget(categoryId, month, year);
        if (budget != null)
        {
            return budget.Amount;
        }
        
        // Fall back to the category's default budget
        var category = _db.GetCategory(categoryId);
        return category?.DefaultBudget ?? 0;
    }

    public bool IsBudgetCustom(int categoryId, int month, int year)
    {
        // Returns true if there's a month-specific budget entry (not using default)
        return _db.GetBudget(categoryId, month, year) != null;
    }

    public void UpdateBudget(int categoryId, int month, int year, decimal amount)
    {
        _db.UpdateBudget(categoryId, month, year, amount);
    }

    public void ResetBudgetToDefault(int categoryId, int month, int year)
    {
        // Remove the month-specific budget entry so it falls back to the default
        _db.DeleteBudget(categoryId, month, year);
    }

    // Recurring Transactions
    public List<RecurringTransaction> GetRecurringTransactions()
    {
        return _db.GetRecurringTransactions();
    }

    public RecurringTransaction? GetRecurringTransaction(int id)
    {
        return _db.GetRecurringTransaction(id);
    }

    public void AddRecurringTransaction(RecurringTransaction recurring)
    {
        _db.AddRecurringTransaction(recurring);
    }

    public void UpdateRecurringTransaction(RecurringTransaction recurring)
    {
        _db.UpdateRecurringTransaction(recurring);
    }

    public void DeleteRecurringTransaction(int id)
    {
        _db.DeleteRecurringTransaction(id);
    }

    public void ProcessRecurringTransactions()
    {
        var today = DateTime.Today;
        var recurringList = _db.GetRecurringTransactions()
            .Where(r => r.IsActive && r.NextDueDate <= today)
            .ToList();

        foreach (var recurring in recurringList)
        {
            // Generate transactions for all missed dates up to today
            while (recurring.NextDueDate <= today)
            {
                // Create the transaction
                var transaction = new Transaction
                {
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    CategoryId = recurring.CategoryId,
                    Type = recurring.Type,
                    Date = recurring.NextDueDate
                };
                _db.AddTransaction(transaction);

                // Calculate next due date
                recurring.NextDueDate = CalculateNextDueDate(recurring);
            }

            // Update the recurring transaction with new NextDueDate
            _db.UpdateRecurringTransaction(recurring);
        }
    }

    private DateTime CalculateNextDueDate(RecurringTransaction recurring)
    {
        var current = recurring.NextDueDate;
        
        return recurring.Frequency switch
        {
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Biweekly => current.AddDays(14),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(current, recurring.DayOfMonth),
            RecurrenceFrequency.Yearly => current.AddYears(1),
            _ => current.AddMonths(1)
        };
    }

    private DateTime GetNextMonthlyDate(DateTime current, int dayOfMonth)
    {
        var nextMonth = current.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth, daysInMonth);
        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }

    // Income & Settings
    public decimal GetMonthlyIncome()
    {
        return _db.GetMonthlyIncome();
    }

    public void SetMonthlyIncome(decimal income)
    {
        _db.SetMonthlyIncome(income);
    }

    public decimal GetTotalIncome(int month, int year)
    {
        // Sum of all income transactions for the month
        return _db.GetTransactions(month, year)
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);
    }

    // Sinking Funds
    public List<SinkingFund> GetSinkingFunds()
    {
        return _db.GetSinkingFunds();
    }

    public SinkingFund? GetSinkingFund(int id)
    {
        return _db.GetSinkingFund(id);
    }

    public void AddSinkingFund(SinkingFund fund)
    {
        _db.AddSinkingFund(fund);
    }

    public void UpdateSinkingFund(SinkingFund fund)
    {
        _db.UpdateSinkingFund(fund);
    }

    public void DeleteSinkingFund(int id)
    {
        _db.DeleteSinkingFund(id);
    }

    public List<SinkingFundTransaction> GetSinkingFundTransactions(int fundId)
    {
        return _db.GetSinkingFundTransactions(fundId);
    }

    public void AddSinkingFundTransaction(SinkingFundTransaction transaction)
    {
        _db.AddSinkingFundTransaction(transaction);
    }

    public void DeleteSinkingFundTransaction(int transactionId)
    {
        _db.DeleteSinkingFundTransaction(transactionId);
    }

    public decimal GetTotalSinkingFundContributions(int month, int year)
    {
        return _db.GetTotalSinkingFundContributions(month, year);
    }
}

