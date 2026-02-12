# BudgetApp Feature Documentation

> **Living document** -- update this file whenever a feature is added or materially changed.
> Last updated: February 11, 2026

---

## Table of Contents

- [Dashboard (Home)](#dashboard-home)
- [Categories](#categories)
- [Category Detail](#category-detail)
- [Transactions](#transactions)
- [Add / Edit Transaction](#add--edit-transaction)
- [Recurring Transactions](#recurring-transactions)
- [Add / Edit Recurring Transaction](#add--edit-recurring-transaction)
- [Sinking Funds](#sinking-funds)
- [Sinking Fund Detail](#sinking-fund-detail)
- [Add / Edit Sinking Fund](#add--edit-sinking-fund)
- [Reports](#reports)
- [Theme (Dark / Light Mode)](#theme-dark--light-mode)
- [Navigation](#navigation)
- [Shared Components](#shared-components)
- [Service Layer](#service-layer)

---

## Dashboard (Home)

**Route:** `/`
**Files:** `Pages/Home.razor`, `Pages/Home.razor.cs`, `Pages/Home.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `IBudgetService`, `IRecurringTransactionService`, `ISettingsService`, `ISinkingFundService`

The dashboard is the primary landing page and provides a full monthly overview of the user's finances.

**Capabilities:**
- Displays expected vs. actual income for the selected month
- Shows total budget allocation and remaining amount
- Spending progress bar across all categories
- Category groups (Fixed, Discretionary, Savings) in expandable sections with per-category spent/budget summaries
- Sinking funds summary with total goal and total saved
- Recent transactions list (latest 5)
- Month navigation via query parameters (`?month=&year=`)

**Notable behavior:**
- In-place editing of expected monthly income
- Paycheck modal to record income (one-time or recurring, with frequency and day-of-month options)
- Processes due recurring transactions on page load
- Section expand/collapse state is persisted across navigation
- FAB navigates to Add Transaction

---

## Categories

**Route:** `/categories`
**Files:** `Pages/Categories.razor`, `Pages/Categories.razor.cs`, `Pages/Categories.razor.css`
**Services:** `ICategoryService`

Manages the user's budget categories, organized into three groups: Fixed, Discretionary, and Savings.

**Capabilities:**
- View all categories grouped by type in expandable sections
- Add a new category via modal
- Edit an existing category via modal
- Delete a category (only when no transactions reference it)

**Modal fields:** Category type toggle, name, icon (via `IconPicker`), color (via `ColorPicker`), default monthly budget, live preview card.

---

## Category Detail

**Routes:** `/category/{CategoryId:int}`, `/category/{CategoryId:int}/{Month:int}/{Year:int}`
**Files:** `Pages/CategoryDetail.razor`, `Pages/CategoryDetail.razor.cs`, `Pages/CategoryDetail.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `IBudgetService`

Shows detailed information for a single category in the selected month.

**Capabilities:**
- Header with gradient based on category color
- Spent vs. budgeted amount with progress bar
- In-place budget editing with option to reset to the category default
- List of transactions for that category in the selected month
- Delete individual transactions
- Month navigation

**Notable behavior:**
- FAB navigates to Add Transaction with the category pre-selected
- Clicking a transaction navigates to the edit page
- Back button returns to Home with the current month/year preserved

---

## Transactions

**Route:** `/transactions`
**Files:** `Pages/Transactions.razor`, `Pages/Transactions.razor.cs`, `Pages/Transactions.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `ISettingsService`

Full transaction list for the selected month with filtering and search.

**Capabilities:**
- Summary bar: expected income, received paychecks, total spent, remaining
- Search transactions by description
- Filter by category (All, Income, or specific category)
- Month navigation
- Click a row to edit the transaction
- Delete button per transaction

**Notable behavior:**
- FAB navigates to Add Transaction
- Supports `returnUrl` query parameter for post-navigation

---

## Add / Edit Transaction

**Routes:** `/add` (add), `/edit/{TransactionId:int}` (edit)
**Files:** `Pages/AddTransaction.razor`, `Pages/AddTransaction.razor.cs`, `Pages/AddTransaction.razor.css`
**Services:** `ICategoryService`, `ITransactionService`

Form to create or modify a transaction.

**Capabilities:**
- Expense / Income toggle
- Amount input
- Description input
- Category selection via chip grid (expenses only; hidden for income)
- Date picker
- Validation before save

**Notable behavior:**
- Same page handles both add and edit based on route
- Supports `returnUrl` query parameter to navigate back to the originating page
- Defaults return to `/` or `/category/{id}` when editing

---

## Recurring Transactions

**Route:** `/recurring`
**Files:** `Pages/RecurringTransactions.razor`, `Pages/RecurringTransactions.razor.cs`, `Pages/RecurringTransactions.razor.css`
**Services:** `ICategoryService`, `IRecurringTransactionService`

Lists all recurring transactions (bills, subscriptions, scheduled transfers) with their status and next due date.

**Capabilities:**
- Info card explaining recurring transactions
- List of all recurring items with icon, description, amount, frequency, and next due date
- Status indicators (Active / Paused)
- Click a row to edit

**Notable behavior:**
- FAB navigates to Add Recurring
- Empty state shown when no recurring transactions exist

---

## Add / Edit Recurring Transaction

**Routes:** `/recurring/add` (add), `/recurring/edit/{RecurringId:int}` (edit)
**Files:** `Pages/EditRecurring.razor`, `Pages/EditRecurring.razor.cs`, `Pages/EditRecurring.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `IRecurringTransactionService`

Form to create or modify a recurring transaction.

**Capabilities:**
- Amount and description inputs
- Category selection via chip grid
- Frequency selector: Weekly, Biweekly, Monthly, Yearly
- Day of month input
- Start date picker
- Active/Paused toggle (edit mode only)
- Delete button (edit mode only)

**Notable behavior:**
- In add mode, the first transaction is created immediately upon save
- Next due date calculated using `RecurrenceCalculator`

---

## Sinking Funds

**Route:** `/sinking-funds`
**Files:** `Pages/SinkingFunds.razor`, `Pages/SinkingFunds.razor.cs`, `Pages/SinkingFunds.razor.css`
**Services:** `ISinkingFundService`

Dashboard for savings goals (sinking funds), showing overall progress and individual fund cards.

**Capabilities:**
- Summary: total goal amount and total saved across all funds
- Funds grouped by status: Active, Paused, Completed
- Each fund card shows: icon, name, progress bar, saved/goal, monthly contribution, months remaining
- "Behind" indicator when a fund is not on track

**Notable behavior:**
- Empty state with call-to-action when no funds exist
- FAB navigates to New Sinking Fund
- Click a fund card to navigate to its detail page

---

## Sinking Fund Detail

**Route:** `/sinking-fund/{FundId:int}/detail`
**Files:** `Pages/SinkingFundDetail.razor`, `Pages/SinkingFundDetail.razor.cs`, `Pages/SinkingFundDetail.razor.css`
**Services:** `ISinkingFundService`

Detailed view of a single sinking fund with contribution/withdrawal history.

**Capabilities:**
- Progress card: saved vs. goal, percentage, progress bar
- Add Money button (contribution modal)
- Withdraw button (withdrawal modal)
- Transaction history list (contributions and withdrawals with dates and notes)
- Delete individual transactions
- Status badge (Active / Paused / Completed)

**Notable behavior:**
- Modal for both contributions and withdrawals: amount, date, optional note
- Edit button in header navigates to Edit Sinking Fund page

---

## Add / Edit Sinking Fund

**Routes:** `/sinking-fund/new` (add), `/sinking-fund/{FundId:int}` (edit)
**Files:** `Pages/EditSinkingFund.razor`, `Pages/EditSinkingFund.razor.cs`, `Pages/EditSinkingFund.razor.css`
**Services:** `ISinkingFundService`

Form to create or modify a sinking fund.

**Capabilities:**
- Name, icon (via `IconPicker`), color (via `ColorPicker`)
- Goal amount and monthly contribution
- Auto-contribute toggle
- Start date and target date
- Status controls: Active / Paused / Completed (edit mode only)
- Delete button (edit mode only)

**Notable behavior:**
- Setting a target date calculates the suggested monthly contribution, with an "Apply" button
- Displays months remaining to reach goal

---

## Reports

**Route:** `/reports`
**Files:** `Pages/Reports.razor`, `Pages/Reports.razor.cs`, `Pages/Reports.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `IBudgetService`

Monthly financial reports with visual summaries.

**Capabilities:**
- Summary cards: total spent, total income, net (income minus expenses)
- Spending by category: horizontal bar chart showing each category's spending relative to the highest
- Budget status: per-category spent vs. budget with remaining/over indicators
- Month navigation

---

## Theme (Dark / Light Mode)

**Files:** `Services/ThemeService.cs`, `Layout/MainLayout.razor`, `Layout/HamburgerMenu.razor`, `wwwroot/app.css`

Supports dark and light themes with a toggle in the hamburger menu.

**Capabilities:**
- Toggle between dark and light mode
- Theme persisted to localStorage via JavaScript interop (`themeHelper.getTheme` / `themeHelper.setTheme`)
- All colours defined as CSS variables in `app.css` under `:root, .theme-dark` and `.theme-light` selectors

**How it works:**
- `ThemeService` manages state and exposes `CurrentTheme`, `IsDarkMode`, `ThemeClass`, `ToggleTheme()`
- `MainLayout` subscribes to `OnThemeChanged` and applies the theme class to the app container
- `HamburgerMenu` renders a toggle button calling `ThemeService.ToggleTheme()`

---

## Navigation

**Files:** `Layout/HamburgerMenu.razor`, `Layout/HamburgerMenu.razor.css`

Slide-out hamburger menu accessible from all pages.

**Links:**
| Destination | Route |
|-------------|-------|
| Home | `/` |
| All Transactions | `/transactions` |
| Recurring | `/recurring` |
| Sinking Funds | `/sinking-funds` |
| Reports | `/reports` |
| Categories | `/categories` |

**Behavior:**
- Highlights the currently active route
- Includes theme toggle at the bottom
- Closes automatically on navigation

**Route Map:**

| Route | Page |
|-------|------|
| `/` | Home |
| `/transactions` | Transactions |
| `/add` | Add Transaction |
| `/edit/{id}` | Edit Transaction |
| `/categories` | Categories |
| `/category/{id}` | Category Detail |
| `/category/{id}/{month}/{year}` | Category Detail (specific month) |
| `/recurring` | Recurring Transactions |
| `/recurring/add` | Add Recurring |
| `/recurring/edit/{id}` | Edit Recurring |
| `/sinking-funds` | Sinking Funds |
| `/sinking-fund/new` | New Sinking Fund |
| `/sinking-fund/{id}` | Edit Sinking Fund |
| `/sinking-fund/{id}/detail` | Sinking Fund Detail |
| `/reports` | Reports |

---

## Shared Components

All reusable components live in `BudgetApp.Shared/Components/`. Each has a `.razor` file, a `.razor.css` file, and uses `[Parameter]` / `EventCallback` for its API.

| Component | Purpose |
|-----------|---------|
| `PageHeader` | Back button + title + optional right-side action slot |
| `MonthNavigator` | Previous/next month arrows with formatted label |
| `FloatingActionButton` | Fixed-position FAB with icon and optional label |
| `EmptyState` | Placeholder when a list has no items -- icon, message, optional child content |
| `ExpandableSection` | Collapsible section with icon, title, summary, count badge |
| `TransactionListItem` | Transaction row: icon, description, date, amount, optional click handler, optional actions slot |
| `ModalBase` | Overlay modal with title, body slot, optional footer slot, close button |
| `IconPicker` | Grid of emoji icons with selected state and optional highlight colour |
| `ColorPicker` | Grid of colour swatches with selected state |

---

## Service Layer

All service interfaces live in `Services/Interfaces/` and implementations in `Services/Sqlite*.cs`.

| Interface | Implementation | Responsibility |
|-----------|---------------|----------------|
| `IDatabaseService` | `DatabaseService` | Pure SQLite CRUD operations (no business logic) |
| `ICategoryService` | `SqliteCategoryService` | Category management, deletion safety checks |
| `ITransactionService` | `SqliteTransactionService` | Transaction CRUD with sorting |
| `IBudgetService` | `SqliteBudgetService` | Budget queries, custom/default detection, budget reset |
| `IRecurringTransactionService` | `SqliteRecurringTransactionService` | Recurring transaction management, due-date processing |
| `ISettingsService` | `SqliteSettingsService` | User settings, monthly income tracking |
| `ISinkingFundService` | `SqliteSinkingFundService` | Sinking fund CRUD, contributions, withdrawals, balance management |

**Shared helpers:**
- `Helpers/DateHelpers` -- `FormatDate()`, `GetOrdinal()` for consistent date display
- `Helpers/RecurrenceCalculator` -- `CalculateNextDueDate()`, `GetNextMonthlyDate()` for recurring transaction scheduling
