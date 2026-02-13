# BudgetApp Feature Documentation

> **Living document** -- update this file whenever a feature is added or materially changed.
> Last updated: February 12, 2026

---

## Table of Contents

- [Dashboard (Home)](#dashboard-home)
- [Onboarding](#onboarding)
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
- [Settings](#settings)
- [Authentication](#authentication)
- [Connect Bank / Upgrade](#connect-bank--upgrade)
- [Theme (Dark / Light Mode)](#theme-dark--light-mode)
- [Navigation](#navigation)
- [Shared Components](#shared-components)
- [Data Export (CSV)](#data-export-csv)
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
- **No-categories tip:** if the user has zero categories (e.g., they skipped onboarding category setup), a prominent banner guides them to `/categories`

**Notable behavior:**
- In-place editing of expected monthly income
- Paycheck modal to record income (one-time or recurring, with frequency and day-of-month options)
- Processes due recurring transactions on page load
- Section expand/collapse state is persisted across navigation
- FAB navigates to Add Transaction
- **First-run redirect:** checks `HasCompletedOnboarding` flag in UserSettings; if false, redirects to `/onboarding`

---

## Onboarding

**Route:** `/onboarding`
**Files:** `Pages/Onboarding.razor`, `Pages/Onboarding.razor.cs`, `Pages/Onboarding.razor.css`
**Services:** `ICategoryService`, `ISettingsService`
**Layout:** `AuthLayout` (no hamburger menu)

A 3-step wizard shown on first launch to help users configure their budget.

**Step 1 -- Set Income:**
- Text input for monthly take-home pay
- "Continue" button (enabled when income > 0)
- "Skip setup" button

**Step 2 -- Choose Categories:**
- Lists all default categories grouped by type (Fixed, Discretionary, Savings)
- Each category has a **checkbox toggle** (on/off) and an **inline budget input**
- Budget summary updates dynamically based on selected categories
- "Looks Good!" proceeds to Step 3
- "Skip -- I'll set up categories later" deletes all default categories and proceeds to Step 3

**Step 3 -- Tips & Finish:**
- Quick-start tips (add transactions, set up recurring, create sinking funds, check reports)
- "Go to My Budget" finishes onboarding

**Onboarding completion:**
- Both `FinishOnboarding()` and `SkipOnboarding()` call `SettingsService.SetOnboardingComplete()`
- Sets `UserSettings.HasCompletedOnboarding = true` in the database
- User is never prompted again regardless of income amount
- If categories are skipped, the Home page shows a tip banner guiding the user to `/categories`

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
- Delete a category with **confirmation dialog** (only when no transactions reference it)

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
- Delete individual transactions with **confirmation dialog**
- Month navigation

**Notable behavior:**
- FAB navigates to Add Transaction with the category pre-selected (`?categoryId=`)
- Clicking a transaction navigates to the edit page with `returnUrl`
- Back button returns to Home with the current month/year preserved

---

## Transactions

**Route:** `/transactions`
**Files:** `Pages/Transactions.razor`, `Pages/Transactions.razor.cs`, `Pages/Transactions.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `ISettingsService`, `IExportService`

Full transaction list for the selected month with filtering, search, and export.

**Capabilities:**
- Summary bar: expected income, received paychecks, total spent, remaining
- Search transactions by description
- **Cross-month search toggle:** "Search all months" queries all non-deleted transactions via `SearchTransactions()`, ignoring the current month filter
- Filter by category (All, Income, or specific category)
- Month navigation (hidden when global search is active)
- Click a row to edit the transaction
- Delete button per transaction with **confirmation dialog**
- **Export to CSV** button in the page header

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
- Supports `categoryId` query parameter to pre-select a category (used from Category Detail "Add Transaction")
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
- Expense / Income toggle
- Amount and description inputs
- Category selection via chip grid (hidden for income -- income doesn't need a category)
- Frequency selector: Weekly, Biweekly, Monthly, Yearly
- Day of month input
- Start date picker
- Active/Paused toggle (edit mode only)
- Delete button with **confirmation dialog** (edit mode only)

**Notable behavior:**
- In add mode, the first transaction is created immediately upon save
- Next due date calculated using `RecurrenceCalculator`
- Income validation allows saving without a selected category

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
- Delete individual transactions with **confirmation dialog**
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
- Delete button with **confirmation dialog** (edit mode only)

**Notable behavior:**
- Setting a target date calculates the suggested monthly contribution, with an "Apply" button
- Displays months remaining to reach goal

---

## Reports

**Route:** `/reports`
**Files:** `Pages/Reports.razor`, `Pages/Reports.razor.cs`, `Pages/Reports.razor.css`
**Services:** `ICategoryService`, `ITransactionService`, `IBudgetService`, `IExportService`

Monthly financial reports with visual summaries and category history deep-dive.

**Capabilities:**
- Summary cards: total spent, total income, net (income minus expenses)
- Spending by category: horizontal bar chart showing each category's spending relative to the highest
- Budget status: per-category spent vs. budget with remaining/over indicators
- Month navigation
- **Export to CSV** button in the page header

**Category History (Deep-Dive):**
- Dropdown to select any category
- Shows spending for that category across up to 6 past months, **only including months where the category had actual transactions**
- Average monthly spend displayed (calculated only from months with data, avoiding misleading averages)
- Horizontal bar chart with month labels and amounts

---

## Settings

**Route:** `/settings`
**Files:** `Pages/Settings.razor`, `Pages/Settings.razor.cs`, `Pages/Settings.razor.css`
**Services:** `IAuthService`, `ISyncService`, `ISubscriptionService`, `ThemeService`

Centralized settings page organized into sections.

**Sections:**
- **Account:** display email, change password form, sign out (when authenticated)
- **Sync:** last synced timestamp, "Sync Now" button (when authenticated)
- **Subscription:** plan status (Free/Premium), link to Upgrade or Connected Banks
- **Appearance:** theme toggle (dark/light mode)
- **About:** app version, description

---

## Authentication

**Routes:** `/login`, `/register`, `/forgot-password`
**Files:** `Pages/Login.razor`, `Pages/Register.razor`, `Pages/ForgotPassword.razor` (+ `.cs`, `.css`)
**Services:** `IAuthService`
**Layout:** `AuthLayout` (no hamburger menu)

User authentication with JWT tokens.

**Login:** Email/password form, "Forgot your password?" link, "Create account" link.
**Register:** Email/password/confirm password form, "Already have an account?" link.
**Forgot Password:** Two-step flow: enter email to request reset code, then enter code + new password. In development mode, the reset token is returned directly for testing.

---

## Connect Bank / Upgrade

**Routes:** `/connect-bank`, `/upgrade`
**Files:** `Pages/ConnectBank.razor`, `Pages/Upgrade.razor` (+ `.cs`, `.css`)
**Services:** `IBankConnectionService`, `ISubscriptionService`

**Connect Bank:** Plaid Link integration for connecting bank accounts and importing transactions (Premium only).
**Upgrade:** Plan selection page for upgrading to Premium (unlocks automatic bank transaction import).

**Navigation:** The "Connected Banks" link is always visible in the hamburger menu when authenticated. If the user is not premium, the link appears greyed out with a "Premium" badge and routes to `/upgrade` instead of `/connect-bank`.

---

## Theme (Dark / Light Mode)

**Files:** `Services/ThemeService.cs`, `Layout/MainLayout.razor`, `wwwroot/app.css`

Supports dark and light themes with a toggle on the Settings page.

**Capabilities:**
- Toggle between dark and light mode
- Theme persisted to localStorage via JavaScript interop (`themeHelper.getTheme` / `themeHelper.setTheme`)
- All colours defined as CSS variables in `app.css` under `:root, .theme-dark` and `.theme-light` selectors

**How it works:**
- `ThemeService` manages state and exposes `CurrentTheme`, `IsDarkMode`, `ThemeClass`, `ToggleTheme()`
- `MainLayout` subscribes to `OnThemeChanged` and applies the theme class to the app container
- Settings page renders a toggle calling `ThemeService.ToggleTheme()`

---

## Navigation

**Files:** `Layout/HamburgerMenu.razor`, `Layout/HamburgerMenu.razor.css`

Slide-out hamburger menu accessible from all pages.

**Links:**
| Destination | Route | Notes |
|-------------|-------|-------|
| Home | `/` | |
| All Transactions | `/transactions` | |
| Recurring | `/recurring` | |
| Sinking Funds | `/sinking-funds` | |
| Reports | `/reports` | |
| Categories | `/categories` | |
| Connected Banks | `/connect-bank` or `/upgrade` | Greyed out with "Premium" badge if not subscribed |
| Settings | `/settings` | |

**Behavior:**
- Highlights the currently active route
- Closes automatically on navigation

**Route Map:**

| Route | Page |
|-------|------|
| `/` | Home |
| `/onboarding` | Onboarding Wizard |
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
| `/settings` | Settings |
| `/login` | Login |
| `/register` | Register |
| `/forgot-password` | Forgot Password |
| `/connect-bank` | Connect Bank (Premium) |
| `/upgrade` | Upgrade to Premium |

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
| `ConfirmDialog` | Reusable confirmation modal built on ModalBase -- title, message, confirm/cancel callbacks, danger mode |
| `IconPicker` | Grid of emoji icons with selected state and optional highlight colour |
| `ColorPicker` | Grid of colour swatches with selected state |

---

## Data Export (CSV)

**Files:** `Services/Interfaces/IExportService.cs`, `Services/ExportService.cs`, `wwwroot/file-download.js`
**Used by:** Transactions page, Reports page

Generates CSV files and triggers client-side downloads via JavaScript interop.

**Capabilities:**
- `GenerateTransactionsCsv`: exports a list of transactions with date, description, category, type, amount columns
- `GenerateBudgetReportCsv`: exports a budget report with category, budget, spent, remaining columns
- Downloads use `window.fileDownload.downloadCsv` JS function to create and trigger a Blob download

---

## Service Layer

All service interfaces live in `Services/Interfaces/` and implementations in `Services/Sqlite*.cs` or `Services/Api*.cs`.

| Interface | Implementation | Responsibility |
|-----------|---------------|----------------|
| `IDatabaseService` | `DatabaseService` | Pure SQLite CRUD operations (no business logic) |
| `ICategoryService` | `SqliteCategoryService` | Category management, deletion safety checks |
| `ITransactionService` | `SqliteTransactionService` | Transaction CRUD with sorting, cross-month search |
| `IBudgetService` | `SqliteBudgetService` | Budget queries, custom/default detection, budget reset |
| `IRecurringTransactionService` | `SqliteRecurringTransactionService` | Recurring transaction management, due-date processing |
| `ISettingsService` | `SqliteSettingsService` | User settings, monthly income, onboarding completion flag |
| `ISinkingFundService` | `SqliteSinkingFundService` | Sinking fund CRUD, contributions, withdrawals, balance management |
| `IExportService` | `ExportService` | CSV generation for transactions and budget reports |
| `IAuthService` | `ApiAuthService` | JWT authentication (login, register, forgot/reset password) |
| `ISyncService` | `SyncService` | Bidirectional data sync between client (SQLite) and server (PostgreSQL) |
| `IBankConnectionService` | `ApiBankConnectionService` | Plaid bank connection management |
| `ISubscriptionService` | `ApiSubscriptionService` | Premium subscription status and receipt validation |

**Shared helpers:**
- `Helpers/DateHelpers` -- `FormatDate()`, `GetOrdinal()` for consistent date display
- `Helpers/RecurrenceCalculator` -- `CalculateNextDueDate()`, `GetNextMonthlyDate()` for recurring transaction scheduling

---

## Removed / Deferred Features

**Budget Rollover (removed):**
Previously, the Home page offered a "Carry over unspent budget" banner that would add unspent amounts from the previous month to the current month's budgets. This feature was removed because:
- It falsely triggered for brand-new users (default budgets with no spending history created a phantom rollover)
- The total rollover amount was opaque -- users couldn't see which categories contributed
- The detection heuristic (`HasRolloverBeenApplied`) was unreliable (any custom budget triggered it)
- The feature will be redesigned and re-implemented in a future release

**6-Month Trends (removed):**
The Reports page previously included a "6-Month Trends" tab showing spending/income over 6 months. This was removed because it produced misleading averages when the user had fewer than 6 months of data (e.g., dividing 1 month of spending by 6). Replaced by the Category History deep-dive, which only averages over months with actual data.
