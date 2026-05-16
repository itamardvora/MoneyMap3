using System;
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
        public static CurrencyRateRepository CurrencyRateRepo { get; private set; }
        public static IncomeRepository Incomes { get; private set; }

       
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

        public static async Task InitAsync()
        {
            if (IsFullyReady())
                return;

            if (!_coreInited)
                await InitForAuthAsync();

            var savedUserId = Preferences.Get("LoggedInUserId", 0);

            if (savedUserId > 0 && !_fullInited)
                await InitAfterLoginAsync();
        }

        public static async Task InitForAuthAsync()
        {
            if (_coreInited)
                return;

            await DatabaseContext.InitAsync();
            Db = DatabaseContext.GetConnection();

            // עדיין נשאר כדי שה-DAL לא יישבר.
            // בהמשך אפשר להחליף את כל App.BackupState לשיטה חדשה עם Firestore.
            BackupState = new FirebaseBackupState();

            Users = new UserRepository(Db);

            // כבר לא משתמשים ב-FirebaseAuthService הישן.
            UserService = new UserService(Users);

            _coreInited = true;
        }

        public static async Task InitAfterLoginAsync()
        {
            if (_fullInited)
                return;

            if (!_coreInited)
                await InitForAuthAsync();

            Investments = new InvestmentRepository(Db);

            var invMigrator = new InvestmentMigrationHelper(Db);
            await invMigrator.EnsureInvestmentCurrencyColumnsAsync();

            Budgets = new BudgetRepository(Db);
            Expenses = new ExpenseRepository(Db);
            Category = new CategoryRepository(Db);
            StockPrices = new StockPriceCacheRepository(Db);
            CurrencyRateRepo = new CurrencyRateRepository(Db);
            Incomes = new IncomeRepository(Db);

            CategoryService = new CategoryService(Category);
            await CategoryService.EnsureDefaultCategoriesAsync();

            ExpenseService = new ExpenseService(Expenses);
            BudgetService = new BudgetService(Budgets, Expenses, Category);
            IncomeService = new IncomeService(Incomes);

            try
            {
                IExchangeRatesClient boiClient = new ExchangeRatesClient();

                CurrencyService = new CurrencyService(
                    CurrencyRateRepo,
                    boiClient,
                    TimeSpan.FromDays(1)
                );

                // חשוב:
                // לא מחכים פה לשערי מטבע.
                // אחרת התחברות אוטומטית יכולה להיתקע הרבה זמן.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await CurrencyService.EnsureBaseRatesAsync();
                    }
                    catch (Exception exRefresh)
                    {
                        Log.Warn("App.InitAfterLogin", "Currency background refresh failed: " + exRefresh.Message);
                    }
                });
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

            _fullInited = true;
        }

        // משאירים את הפונקציה כדי שאם יש מקום באפליקציה שקורא לה,
        // הפרויקט לא יישבר. כרגע היא לא עושה כלום.
        public static async Task TryBackupPendingChangesAsync()
        {
            if (!IsFullyReady())
                return;

            var userId = UserSession.LoggedInUserId ?? Preferences.Get("LoggedInUserId", 0);
            var firebaseUid = Preferences.Get("FirebaseUid", "");

            if (userId <= 0)
                return;

            if (string.IsNullOrWhiteSpace(firebaseUid))
                return;

            if (BackupState == null || !BackupState.HasPendingChanges)
                return;

            var snapshot = BackupState.CreateSnapshot();

            if (snapshot == null || !snapshot.HasAny)
                return;

            try
            {
                await FireBaseHelper.BackupAllUserDataAsync();

                BackupState.Clear(snapshot);

                Log.Info("App.Backup", "Full Firestore backup completed.");
            }
            catch (Exception ex)
            {
                Log.Warn("App.Backup", "Full Firestore backup failed: " + ex.Message);
            }
        }
        public static bool IsCoreReady() =>
            _coreInited &&
            UserService != null &&
            Db != null;

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