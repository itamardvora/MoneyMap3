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
    // מחלקה סטטית מרכזית שאחראית לאתחול בסיס הנתונים, המחסנים והשירותים של האפליקציהצ
    //משמשת כנקודת גישה מרכזית לכל שכבות המידע והלוגיקה, כדי ששאר האפליקציה תוכל להשתמש בהן ממקום אחד

    {
       
        public static SQLiteAsyncConnection Db { get; private set; } // שומר את החיבור לSQLLITE 

        // שמירה של אוביקטים שאחראים על השינויים בממד נתונםי ועל הפעולות הלוגיסטיות
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

        private static bool _coreInited = false; // מסמן האם האתחול הבסיסי של האפליקציה כבר בוצע דברים בסיסיים
        private static bool _fullInited = false; //   מסמן האם האתחול המלא אחרי התחברות המשתמש כבר בוצע הכל


        // בודקת איזה אתחול צצרצךי וקרואת לפעולה הצמתאימה
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

        // אתחול בסיסי דף הרשמה והתחברות
        public static async Task InitForAuthAsync()
        {
            if (_coreInited)
                return;

            await DatabaseContext.InitAsync();
            Db = DatabaseContext.GetConnection();

          
            BackupState = new FirebaseBackupState();

            Users = new UserRepository(Db);

           
            UserService = new UserService(Users);

            _coreInited = true;
        }

        // אתחול מלא של הכל
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

       // מנסה לעשות גיבוי אבל רק אם היה שינוי של גובה
        public static async Task TryBackupPendingChangesAsync()  
        {
            if (!IsFullyReady()) // מצחכה שהאפליקצציה תציהיה מוכנה 
                return;

            var userId = UserSession.LoggedInUserId ?? Preferences.Get("LoggedInUserId", 0);
            var firebaseUid = Preferences.Get("FirebaseUid", "");

            if (userId <= 0)
                return;

            if (string.IsNullOrWhiteSpace(firebaseUid)) // אם אין מזהה של הפייר בייס מפסיקה
                return;

            if (BackupState == null || !BackupState.HasPendingChanges)
                return;

            var snapshot = BackupState.CreateSnapshot();

            if (snapshot == null || !snapshot.HasAny)
                return;

            try // מנסה לעשות את הגיבוי
            {
                await FireBaseHelper.BackupAllUserDataAsync();

                BackupState.Clear(snapshot);

                Log.Info("App.Backup", "Full Firestore backup completed.");
            }
            catch (Exception ex) // כותב אם לא הצצליחה שהגיבוי נכשל
            {
                Log.Warn("App.Backup", "Full Firestore backup failed: " + ex.Message);
            }
        }


        // בודקת אם האתחול המלא מוכן לא מאתחלת שום דבר
        public static bool IsCoreReady() =>
            _coreInited &&
            UserService != null &&
            Db != null;


        // בודקת אם האתחול המלא מוכן לא מאתחלת שום דבר
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