using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace MoneyMap.Services
{
    public class FirebaseBackupService
    {
        private static readonly HttpClient _http = new HttpClient();

        private readonly FirebaseAuthService _authService;
        private readonly UserRepository _users;
        private readonly CategoryRepository _categories;
        private readonly BudgetRepository _budgets;
        private readonly ExpenseRepository _expenses;
        private readonly InvestmentRepository _investments;
        private readonly IncomeRepository _incomes;

        public FirebaseBackupService(
            FirebaseAuthService authService,
            UserRepository users,
            CategoryRepository categories,
            BudgetRepository budgets,
            ExpenseRepository expenses,
            InvestmentRepository investments,
            IncomeRepository incomes)
        {
            _authService = authService;
            _users = users;
            _categories = categories;
            _budgets = budgets;
            _expenses = expenses;
            _investments = investments;
            _incomes = incomes;
        }

        public async Task SyncPendingAsync(int localUserId, BackupSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.HasAny)
                return;

            await SyncProfileAsync(localUserId);

            if (snapshot.CategoriesChanged)
                await SyncCategoriesAsync(localUserId);

            if (snapshot.BudgetsChanged)
                await SyncBudgetsAsync(localUserId);

            if (snapshot.ExpensesChanged)
                await SyncExpensesAsync(localUserId);

            if (snapshot.InvestmentsChanged)
                await SyncInvestmentsAsync(localUserId);

            if (snapshot.IncomesChanged)
                await SyncIncomesAsync(localUserId);
        }

        public async Task SyncProfileAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var user = await _users.GetUserById(localUserId);

            var payload = new
            {
                localUserId = localUserId,
                fullName = user?.FullName ?? Preferences.Get("LoggedInUserName", ""),
                email = user?.Email ?? Preferences.Get("FirebaseEmail", ""),
                firebaseUid = Preferences.Get("FirebaseUid", ""),
                lastBackupUtc = DateTime.UtcNow.ToString("o")
            };

            await PutAsync("profile", payload);
        }

        public async Task SyncCategoriesAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var rows = await _categories.GetCategoriesForUser(localUserId);

            var dict = rows.ToDictionary(
                x => x.CategoryID.ToString(),
                x => (object)new
                {
                    localId = x.CategoryID,
                    categoryName = x.CategoryName,
                    isSystem = x.IsSystem,
                    createdByUserId = x.CreatedByUserID
                });

            await PutAsync("categories", dict);
        }

        public async Task SyncBudgetsAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var rows = await _budgets.GetUserBudgets(localUserId, null);

            var dict = rows.ToDictionary(
                x => x.BudgetID.ToString(),
                x => (object)new
                {
                    localId = x.BudgetID,
                    userId = x.UserID,
                    categoryId = x.CategoryID,
                    monthlyLimit = x.MonthlyLimit,
                    budgetMonth = x.BudgetMonth.ToString("o"),
                    createdAt = x.CreatedAt.ToString("o")
                });

            await PutAsync("budgets", dict);
        }

        public async Task SyncExpensesAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var rows = await _expenses.GetAllByUser(localUserId);

            var dict = rows.ToDictionary(
                x => x.ExpenseID.ToString(),
                x => (object)new
                {
                    localId = x.ExpenseID,
                    userId = x.UserID,
                    categoryId = x.CategoryID,
                    amount = x.Amount,
                    date = x.Date.ToString("o"),
                    reason = x.Reason,
                    createdAt = x.CreatedAt.ToString("o"),
                    receiptPath = x.ReceiptPath
                });

            await PutAsync("expenses", dict);
        }

        public async Task SyncInvestmentsAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var rows = await _investments.GetAllByUser(localUserId);

            var dict = rows.ToDictionary(
                x => x.InvestmentID.ToString(),
                x => (object)new
                {
                    localId = x.InvestmentID,
                    userId = x.UserID,
                    stockSymbol = x.StockSymbol,
                    buyDate = x.BuyDate.ToString("o"),
                    buyPrice = x.BuyPrice,
                    quantity = x.Quantity,
                    createdAt = x.CreatedAt.ToString("o"),
                    originalCurrency = x.OriginalCurrency,
                    fxRateToIlsAtPurchase = x.FxRateToIlsAtPurchase,
                    totalInIls = x.TotalInIls,
                    receiptImagePath = x.ReceiptImagePath
                });

            await PutAsync("investments", dict);
        }

        public async Task SyncIncomesAsync(int localUserId)
        {
            if (!await CanSyncAsync()) return;

            var rows = await _incomes.GetAllByUser(localUserId);

            var dict = rows.ToDictionary(
                x => x.IncomeID.ToString(),
                x => (object)new
                {
                    localId = x.IncomeID,
                    userId = x.UserID,
                    amount = x.Amount,
                    date = x.Date.ToString("o"),
                    source = x.Source,
                    createdAt = x.CreatedAt.ToString("o")
                });

            await PutAsync("incomes", dict);
        }

        public async Task<bool> RestoreIfLocalDataMissingAsync(int localUserId)
        {
            if (localUserId <= 0)
                return false;

            if (!await CanSyncAsync())
                return false;

            bool hasLocalData = await HasAnyLocalUserDataAsync(localUserId);
            if (hasLocalData)
                return false;

            bool hasRemoteData = await HasAnyRemoteBackupAsync();
            if (!hasRemoteData)
                return false;

            await RestoreAllAsync(localUserId);
            return true;
        }

        private async Task<bool> HasAnyLocalUserDataAsync(int localUserId)
        {
            var db = App.Db;
            if (db == null)
                return true;

            var categories = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM CategoriesTable WHERE CreatedByUserID = ?",
                localUserId);

            var budgets = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM UserBudgetTable WHERE UserID = ?",
                localUserId);

            var expenses = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ExpensesTable WHERE UserID = ?",
                localUserId);

            var investments = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM UserInvestmentsTable WHERE UserID = ?",
                localUserId);

            var incomes = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM IncomesTable WHERE UserID = ?",
                localUserId);

            return categories > 0 ||
                   budgets > 0 ||
                   expenses > 0 ||
                   investments > 0 ||
                   incomes > 0;
        }

        private async Task<bool> HasAnyRemoteBackupAsync()
        {
            var categories = await GetAsync<Dictionary<string, FirebaseCategoryRow>>("categories");
            if (categories != null && categories.Count > 0) return true;

            var budgets = await GetAsync<Dictionary<string, FirebaseBudgetRow>>("budgets");
            if (budgets != null && budgets.Count > 0) return true;

            var expenses = await GetAsync<Dictionary<string, FirebaseExpenseRow>>("expenses");
            if (expenses != null && expenses.Count > 0) return true;

            var investments = await GetAsync<Dictionary<string, FirebaseInvestmentRow>>("investments");
            if (investments != null && investments.Count > 0) return true;

            var incomes = await GetAsync<Dictionary<string, FirebaseIncomeRow>>("incomes");
            if (incomes != null && incomes.Count > 0) return true;

            return false;
        }

        private async Task RestoreAllAsync(int localUserId)
        {
            var db = App.Db;
            if (db == null)
                return;

            await ClearLocalUserDataAsync(localUserId);

            var categories = await GetAsync<Dictionary<string, FirebaseCategoryRow>>("categories");
            if (categories != null)
            {
                foreach (var row in categories.Values.Where(x => x != null))
                {
                    if (row.localId <= 0 || string.IsNullOrWhiteSpace(row.categoryName))
                        continue;

                    int? createdBy = row.isSystem ? (int?)null : localUserId;

                    await db.ExecuteAsync(
                        @"INSERT OR REPLACE INTO CategoriesTable
                          (CategoryID, CategoryName, IsSystem, CreatedByUserID)
                          VALUES (?, ?, ?, ?)",
                        row.localId,
                        row.categoryName,
                        row.isSystem,
                        createdBy);
                }
            }

            var budgets = await GetAsync<Dictionary<string, FirebaseBudgetRow>>("budgets");
            if (budgets != null)
            {
                foreach (var row in budgets.Values.Where(x => x != null))
                {
                    if (row.localId <= 0 || row.categoryId <= 0)
                        continue;

                    await db.ExecuteAsync(
                        @"INSERT OR REPLACE INTO UserBudgetTable
                          (BudgetID, UserID, CategoryID, MonthlyLimit, BudgetMonth, CreatedAt)
                          VALUES (?, ?, ?, ?, ?, ?)",
                        row.localId,
                        localUserId,
                        row.categoryId,
                        row.monthlyLimit,
                        ParseDate(row.budgetMonth, DateTime.Now),
                        ParseDate(row.createdAt, DateTime.Now));
                }
            }

            var expenses = await GetAsync<Dictionary<string, FirebaseExpenseRow>>("expenses");
            if (expenses != null)
            {
                foreach (var row in expenses.Values.Where(x => x != null))
                {
                    if (row.localId <= 0 || row.categoryId <= 0)
                        continue;

                    await db.ExecuteAsync(
                        @"INSERT OR REPLACE INTO ExpensesTable
                          (ExpenseID, UserID, CategoryID, Amount, Date, Reason, CreatedAt, ReceiptPath)
                          VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
                        row.localId,
                        localUserId,
                        row.categoryId,
                        row.amount,
                        ParseDate(row.date, DateTime.Now),
                        row.reason ?? "",
                        ParseDate(row.createdAt, DateTime.Now),
                        row.receiptPath);
                }
            }

            var investments = await GetAsync<Dictionary<string, FirebaseInvestmentRow>>("investments");
            if (investments != null)
            {
                foreach (var row in investments.Values.Where(x => x != null))
                {
                    if (row.localId <= 0 || string.IsNullOrWhiteSpace(row.stockSymbol))
                        continue;

                    await db.ExecuteAsync(
                        @"INSERT OR REPLACE INTO UserInvestmentsTable
                          (InvestmentID, UserID, StockSymbol, BuyDate, BuyPrice, Quantity, CreatedAt,
                           OriginalCurrency, FxRateToIlsAtPurchase, TotalInIls, ReceiptImagePath)
                          VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                        row.localId,
                        localUserId,
                        row.stockSymbol.Trim().ToUpperInvariant(),
                        ParseDate(row.buyDate, DateTime.Now),
                        row.buyPrice,
                        row.quantity,
                        ParseDate(row.createdAt, DateTime.Now),
                        string.IsNullOrWhiteSpace(row.originalCurrency) ? "ILS" : row.originalCurrency.Trim().ToUpperInvariant(),
                        row.fxRateToIlsAtPurchase <= 0 ? 1m : row.fxRateToIlsAtPurchase,
                        row.totalInIls,
                        row.receiptImagePath);
                }
            }

            var incomes = await GetAsync<Dictionary<string, FirebaseIncomeRow>>("incomes");
            if (incomes != null)
            {
                foreach (var row in incomes.Values.Where(x => x != null))
                {
                    if (row.localId <= 0)
                        continue;

                    await db.ExecuteAsync(
                        @"INSERT OR REPLACE INTO IncomesTable
                          (IncomeID, UserID, Amount, Date, Source, CreatedAt)
                          VALUES (?, ?, ?, ?, ?, ?)",
                        row.localId,
                        localUserId,
                        row.amount,
                        ParseDate(row.date, DateTime.Now),
                        row.source ?? "",
                        ParseDate(row.createdAt, DateTime.Now));
                }
            }
        }

        private async Task ClearLocalUserDataAsync(int localUserId)
        {
            var db = App.Db;
            if (db == null)
                return;

            await db.ExecuteAsync("DELETE FROM IncomesTable WHERE UserID = ?", localUserId);
            await db.ExecuteAsync("DELETE FROM UserInvestmentsTable WHERE UserID = ?", localUserId);
            await db.ExecuteAsync("DELETE FROM ExpensesTable WHERE UserID = ?", localUserId);
            await db.ExecuteAsync("DELETE FROM UserBudgetTable WHERE UserID = ?", localUserId);
            await db.ExecuteAsync("DELETE FROM CategoriesTable WHERE CreatedByUserID = ?", localUserId);
        }

        private static DateTime ParseDate(string value, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
                return result;

            return fallback;
        }

        private async Task<bool> CanSyncAsync()
        {
            if (string.IsNullOrWhiteSpace(FirebaseConfig.DatabaseBaseUrl) ||
                FirebaseConfig.DatabaseBaseUrl.Contains("PUT-YOUR-DATABASE-URL-HERE"))
            {
                return false;
            }

            var firebaseUid = Preferences.Get("FirebaseUid", "");
            if (string.IsNullOrWhiteSpace(firebaseUid))
                return false;

            var token = await EnsureValidIdTokenAsync();
            return !string.IsNullOrWhiteSpace(token);
        }

        private async Task<string> EnsureValidIdTokenAsync()
        {
            var currentToken = Preferences.Get("FirebaseIdToken", "");
            var refreshToken = Preferences.Get("FirebaseRefreshToken", "");
            var expiresAtUnix = Preferences.Get("FirebaseIdTokenExpiresAtUnix", 0L);
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!string.IsNullOrWhiteSpace(currentToken) && nowUnix < (expiresAtUnix - 60))
                return currentToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            var refreshed = await _authService.RefreshIdTokenAsync(refreshToken);
            if (!refreshed.Success)
                return null;

            SaveSession(refreshed);
            return refreshed.IdToken;
        }

        private void SaveSession(FirebaseAuthResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.FirebaseUid))
                Preferences.Set("FirebaseUid", result.FirebaseUid);

            if (!string.IsNullOrWhiteSpace(result.IdToken))
                Preferences.Set("FirebaseIdToken", result.IdToken);

            if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                Preferences.Set("FirebaseRefreshToken", result.RefreshToken);

            var expiresAt = DateTimeOffset.UtcNow
                .AddSeconds(result.ExpiresInSeconds <= 0 ? 3600 : result.ExpiresInSeconds)
                .ToUnixTimeSeconds();

            Preferences.Set("FirebaseIdTokenExpiresAtUnix", expiresAt);
        }

        private async Task PutAsync(string relativePath, object payload)
        {
            var firebaseUid = Preferences.Get("FirebaseUid", "");
            var idToken = await EnsureValidIdTokenAsync();

            if (string.IsNullOrWhiteSpace(firebaseUid) || string.IsNullOrWhiteSpace(idToken))
                return;

            var url = $"{FirebaseConfig.DatabaseBaseUrl}/users/{firebaseUid}/{relativePath}.json?auth={Uri.EscapeDataString(idToken)}";

            var json = JsonConvert.SerializeObject(payload ?? new Dictionary<string, object>());
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PutAsync(url, content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<T> GetAsync<T>(string relativePath)
        {
            var firebaseUid = Preferences.Get("FirebaseUid", "");
            var idToken = await EnsureValidIdTokenAsync();

            if (string.IsNullOrWhiteSpace(firebaseUid) || string.IsNullOrWhiteSpace(idToken))
                return default;

            var url = $"{FirebaseConfig.DatabaseBaseUrl}/users/{firebaseUid}/{relativePath}.json?auth={Uri.EscapeDataString(idToken)}";

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json) || json == "null")
                return default;

            return JsonConvert.DeserializeObject<T>(json);
        }

        private class FirebaseCategoryRow
        {
            public int localId { get; set; }
            public string categoryName { get; set; }
            public bool isSystem { get; set; }
            public int? createdByUserId { get; set; }
        }

        private class FirebaseBudgetRow
        {
            public int localId { get; set; }
            public int userId { get; set; }
            public int categoryId { get; set; }
            public decimal monthlyLimit { get; set; }
            public string budgetMonth { get; set; }
            public string createdAt { get; set; }
        }

        private class FirebaseExpenseRow
        {
            public int localId { get; set; }
            public int userId { get; set; }
            public int categoryId { get; set; }
            public decimal amount { get; set; }
            public string date { get; set; }
            public string reason { get; set; }
            public string createdAt { get; set; }
            public string receiptPath { get; set; }
        }

        private class FirebaseInvestmentRow
        {
            public int localId { get; set; }
            public int userId { get; set; }
            public string stockSymbol { get; set; }
            public string buyDate { get; set; }
            public decimal buyPrice { get; set; }
            public decimal quantity { get; set; }
            public string createdAt { get; set; }
            public string originalCurrency { get; set; }
            public decimal fxRateToIlsAtPurchase { get; set; }
            public decimal totalInIls { get; set; }
            public string receiptImagePath { get; set; }
        }

        private class FirebaseIncomeRow
        {
            public int localId { get; set; }
            public int userId { get; set; }
            public decimal amount { get; set; }
            public string date { get; set; }
            public string source { get; set; }
            public string createdAt { get; set; }
        }
    }
}