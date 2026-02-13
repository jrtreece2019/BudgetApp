namespace BudgetApp.Api.Models.Entities;

/// <summary>
/// These enums are duplicated from BudgetApp.Shared.Models because the API project
/// intentionally does NOT reference BudgetApp.Shared (to avoid pulling in SQLite
/// dependencies). The numeric values must stay in sync with the shared versions
/// so that data serializes correctly during sync.
/// </summary>

public enum CategoryType
{
    Fixed = 0,
    Discretionary = 1,
    Savings = 2
}

public enum TransactionType
{
    Expense = 0,
    Income = 1
}

public enum RecurrenceFrequency
{
    Weekly = 0,
    Biweekly = 1,
    Monthly = 2,
    Yearly = 3
}

public enum SinkingFundStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2
}

public enum SinkingFundTransactionType
{
    Contribution = 0,
    Withdrawal = 1
}
