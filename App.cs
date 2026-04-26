using System;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using Xamarin.Essentials;
using Android.Util;

using MoneyMap.DAL;
using MoneyMap.Services;

namespace MoneyMap
{
    public static class App
    {
        public static SQLiteAsyncConnection Db { get; private set; }

        public static UserRepository Users { get; private set; }
        public static InvestmentRepository Investments { get; private set; }
        public static BudgetRepository Budgets { get; private set; }
        public static ExpenseRepository Expenses { get; private set; }
        public static CategoryRepository Category { get; private set; }
        public static StockPriceCacheRepository StockPrices { get; private set; }
        public static UserSettingsRepository UserSettingsRepo { get; private set; }
        public static CurrencyRateRepository CurrencyRateRepo { get; private set; }
        public static IncomeRepository Incomes { get; private set; }

        public static FirebaseAuthService FirebaseAuthService { get; private set; }
        public static FirebaseBackupService FirebaseBackupService { get; private set; }
        public static FirebaseBackupState BackupState { get; private set; }

        public static UserService UserService { get; private set; }
        public static CategoryService CategoryService { get; private set; }
        public static ExpenseService ExpenseService { get; private set; }
        public static BudgetService BudgetService { get; private set; }
        public static StockPriceService StockPriceService { get; private set; }
        public static InvestmentService InvestmentService { get; private set; }
        public static PortfolioService PortfolioService { get; private set; }
        public static CurrencyService CurrencyService { get; private set; }
        public static IncomeService IncomeService { get; private set; }

        private static bool _coreInited = false;
        private static bool _fullInited = false;
        private static readonly SemaphoreSlim _backupLock = new SemaphoreSlim(1, 1);

        public static async Task InitAsync()
        {
            if (IsFullyReady()) return;

            if (!_coreInited)
                await InitForAuthAsync();

            var savedUserId = Preferences.Get("LoggedInUserId", 0);
            if (savedUserId > 0 && !_fullInited)
                await InitAfterLoginAsync();
        }

        public static async Task InitForAuthAsync()
        {
            if (_coreInited) return;

            await DatabaseContext.InitAsync();
            Db = DatabaseContext.GetConnection();

            BackupState = new FirebaseBackupState();
            FirebaseAuthService = new FirebaseAuthService();

            Users = new UserRepository(Db);
            UserService = new UserService(Users, FirebaseAuthService);

            _coreInited = true;
        }

        public static async Task InitAfterLoginAsync()
        {
            if (_fullInited) return;

            if (!_coreInited)
                await InitForAuthAsync();

            Investments = new InvestmentRepository(Db);

            var invMigrator = new InvestmentMigrationHelper(Db);
            await invMigrator.EnsureInvestmentCurrencyColumnsAsync();

            Budgets = new BudgetRepository(Db);
            Expenses = new ExpenseRepository(Db);
            Category = new CategoryRepository(Db);
            StockPrices = new StockPriceCacheRepository(Db);
            UserSettingsRepo = new UserSettingsRepository(Db);
            CurrencyRateRepo = new CurrencyRateRepository(Db);
            Incomes = new IncomeRepository(Db);

            FirebaseBackupService = new FirebaseBackupService(
                FirebaseAuthService,
                Users,
                Category,
                Budgets,
                Expenses,
                Investments,
                Incomes);

            CategoryService = new CategoryService(Category);
            await CategoryService.EnsureDefaultCategoriesAsync();
            ExpenseService = new ExpenseService(Expenses);
            BudgetService = new BudgetService(Budgets, Expenses, Category);
            IncomeService = new IncomeService(Incomes);

            try
            {
                IExchangeRatesClient boiClient = new ExchangeRatesClient();

                CurrencyService = new CurrencyService(
                    UserSettingsRepo,
                    CurrencyRateRepo,
                    boiClient,
                    TimeSpan.FromDays(1)
                );

                try
                {
                    await CurrencyService.EnsureBaseRatesAsync();
                }
                catch (Exception exRefresh)
                {
                    Log.Warn("App.InitAfterLogin", "Currency first refresh failed: " + exRefresh.Message);
                }
            }
            catch (Exception exCtor)
            {
                Log.Warn("App.InitAfterLogin", "Currency service ctor failed: " + exCtor.Message);
                CurrencyService = null;
            }

            try
            {
                const string alphaKey = "K64STGMLKTAXGBC7";
                StockPriceService = new StockPriceService(StockPrices, alphaKey, TimeSpan.FromDays(1));
            }
            catch (Exception ex)
            {
                Log.Warn("App.InitAfterLogin", "StockPrice init failed: " + ex.Message);
                StockPriceService = null;
            }

            InvestmentService = new InvestmentService(Investments, StockPriceService, CurrencyService);
            PortfolioService = new PortfolioService(Investments, StockPriceService, CurrencyService);

            await TryRestoreBackupIfNeededAsync();

            _fullInited = true;
        }

        private static async Task TryRestoreBackupIfNeededAsync()
        {
            try
            {
                var userId = UserSession.LoggedInUserId ?? Preferences.Get("LoggedInUserId", 0);
                if (userId <= 0)
                    return;

                if (FirebaseBackupService == null)
                    return;

                bool restored = await FirebaseBackupService.RestoreIfLocalDataMissingAsync(userId);

                if (restored)
                {
                    Log.Info("App.Restore", "Firebase backup restored into local SQLite.");
                }
            }
            catch (Exception ex)
            {
                Log.Warn("App.Restore", "Restore failed: " + ex.Message);
            }
        }

        public static async Task TryBackupPendingChangesAsync()
        {
            if (!IsFullyReady())
                return;

            var userId = UserSession.LoggedInUserId ?? 0;
            if (userId <= 0)
                return;

            if (BackupState == null || !BackupState.HasPendingChanges)
                return;

            if (!await _backupLock.WaitAsync(0))
                return;

            try
            {
                var snapshot = BackupState.CreateSnapshot();
                if (!snapshot.HasAny)
                    return;

                await FirebaseBackupService.SyncPendingAsync(userId, snapshot);
                BackupState.Clear(snapshot);
            }
            catch (Exception ex)
            {
                Log.Warn("App.Backup", "Backup failed: " + ex.Message);
            }
            finally
            {
                _backupLock.Release();
            }
        }

        public static bool IsCoreReady() =>
            _coreInited && UserService != null && Db != null;

        public static bool IsFullyReady() =>
            _fullInited &&
            UserService != null &&
            CategoryService != null &&
            ExpenseService != null &&
            BudgetService != null &&
            IncomeService != null &&
            InvestmentService != null &&
            PortfolioService != null &&
            Db != null;
    }
}