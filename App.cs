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

        // --- DAL ---
        public static UserRepository Users { get; private set; }
        public static InvestmentRepository Investments { get; private set; }
        public static BudgetRepository Budgets { get; private set; }
        public static ExpenseRepository Expenses { get; private set; }
        public static CategoryRepository Category { get; private set; }
        public static StockPriceCacheRepository StockPrices { get; private set; }
        public static UserSettingsRepository UserSettingsRepo { get; private set; }
        public static CurrencyRateRepository CurrencyRateRepo { get; private set; }

        
        public static IncomeRepository Incomes { get; private set; }

        // --- Services ---
        public static UserService UserService { get; private set; }
        public static CategoryService CategoryService { get; private set; }
        public static ExpenseService ExpenseService { get; private set; }
        public static BudgetService BudgetService { get; private set; }
        public static StockPriceService StockPriceService { get; private set; }
        public static InvestmentService InvestmentService { get; private set; }
        public static PortfolioService PortfolioService { get; private set; }
        public static CurrencyService CurrencyService { get; private set; }

        // חדש
        public static IncomeService IncomeService { get; private set; }

        private static bool _coreInited = false;
        private static bool _fullInited = false;

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

            Users = new UserRepository(Db);
            UserService = new UserService(Users);

            _coreInited = true;
        }

        public static async Task InitAfterLoginAsync()
        {
            if (_fullInited) return;

            if (!_coreInited)
                await InitForAuthAsync();

            // --------------------
            // 1) DAL
            // --------------------
            Investments = new InvestmentRepository(Db);

            var invMigrator = new InvestmentMigrationHelper(Db);
            await invMigrator.EnsureInvestmentCurrencyColumnsAsync();

            Budgets = new BudgetRepository(Db);
            Expenses = new ExpenseRepository(Db);
            Category = new CategoryRepository(Db);
            StockPrices = new StockPriceCacheRepository(Db);
            UserSettingsRepo = new UserSettingsRepository(Db);
            CurrencyRateRepo = new CurrencyRateRepository(Db);

            // חדש
            Incomes = new IncomeRepository(Db);

            // --------------------
            // 2) Services ללא רשת
            // --------------------
            CategoryService = new CategoryService(Category);
            ExpenseService = new ExpenseService(Expenses);
            BudgetService = new BudgetService(Budgets, Expenses, Category);

            // חדש
            IncomeService = new IncomeService(Incomes);

            // --------------------
            // 3) CurrencyService
            // --------------------
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

            // --------------------
            // 4) StockPriceService
            // --------------------
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

            // --------------------
            // 5) Services שתלויים במניות/מטבע
            // --------------------
            InvestmentService = new InvestmentService(Investments, StockPriceService, CurrencyService);
            PortfolioService = new PortfolioService(Investments, StockPriceService);

            _fullInited = true;
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