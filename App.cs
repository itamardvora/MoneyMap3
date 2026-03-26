//using System;
//using System.Threading.Tasks;
//using SQLite;
//using Xamarin.Essentials;
//using Android.Util;

//using MoneyMap.DAL;
//using MoneyMap.Services;

//namespace MoneyMap
//{
//    public static class App
//    {
//        public static SQLiteAsyncConnection Db { get; private set; }

//        // --- DAL ---
//        public static UserRepository Users { get; private set; }
//        public static InvestmentRepository Investments { get; private set; }
//        public static BudgetRepository Budgets { get; private set; }
//        public static ExpenseRepository Expenses { get; private set; }
//        public static CategoryRepository Category { get; private set; }
//        public static StockPriceCacheRepository StockPrices { get; private set; }
//        public static UserSettingsRepository UserSettingsRepo { get; private set; }
//        public static CurrencyRateRepository CurrencyRateRepo { get; private set; }

//        // --- Services ---
//        public static UserService UserService { get; private set; }
//        public static CategoryService CategoryService { get; private set; }
//        public static ExpenseService ExpenseService { get; private set; }
//        public static BudgetService BudgetService { get; private set; }
//        public static StockPriceService StockPriceService { get; private set; }
//        public static InvestmentService InvestmentService { get; private set; }
//        public static PortfolioService PortfolioService { get; private set; }
//        public static CurrencyService CurrencyService { get; private set; }

//        private static bool _coreInited = false; // DB + UserService
//        private static bool _fullInited = false; // יתר השירותים

//        public static async Task InitAsync()
//        {
//            if (IsFullyReady()) return;

//            if (!_coreInited)
//                await InitForAuthAsync();

//            var savedUserId = Preferences.Get("LoggedInUserId", 0);
//            if (savedUserId > 0 && !_fullInited)
//                await InitAfterLoginAsync();
//        }

//        public static async Task InitForAuthAsync()
//        {
//            if (_coreInited) return;

//            await DatabaseContext.InitAsync();
//            Db = DatabaseContext.GetConnection();

//            Users = new UserRepository(Db);
//            UserService = new UserService(Users);

//            _coreInited = true;
//        }

//        public static async Task InitAfterLoginAsync()
//        {
//            if (_fullInited) return;

//            if (!_coreInited)
//                await InitForAuthAsync();

//            // DAL
//            Investments = new InvestmentRepository(Db);
//            var invMigrator = new InvestmentMigrationHelper(Db);
//            await invMigrator.EnsureInvestmentCurrencyColumnsAsync();

//            Budgets = new BudgetRepository(Db);
//            Expenses = new ExpenseRepository(Db);
//            Category = new CategoryRepository(Db);
//            StockPrices = new StockPriceCacheRepository(Db);
//            UserSettingsRepo = new UserSettingsRepository(Db);
//            CurrencyRateRepo = new CurrencyRateRepository(Db);

//            // Services ללא רשת
//            CategoryService = new CategoryService(Category);
//            ExpenseService = new ExpenseService(Expenses);
//            BudgetService = new BudgetService(Budgets, Expenses, Category);

//            // CurrencyService — לא מבטלים את השירות אם הרענון הראשון נכשל.
//            try
//            {
//                IExchangeRatesClient boiClient = new ExchangeRatesClient(); // ראה קובץ למטה

//                CurrencyService = new CurrencyService(
//                    UserSettingsRepo,
//                    CurrencyRateRepo,
//                    boiClient,
//                    TimeSpan.FromDays(1) // TTL 24 שעות
//                );

//                // ניסיון רענון ראשון — אם ייכשל נשארים עם השירות פעיל (מטמון/ILS)
//                try
//                {
//                    await CurrencyService.EnsureBaseRatesAsync();
//                }
//                catch (Exception exRefresh)
//                {
//                    Log.Warn("App.InitAfterLogin", "Currency first refresh failed: " + exRefresh.Message);
//                }
//            }
//            catch (Exception exCtor)
//            {
//                Log.Warn("App.InitAfterLogin", "Currency service ctor failed: " + exCtor.Message);
//                CurrencyService = null; // רק אם הבנאי עצמו נכשל
//            }

//            // Stock prices
//            try
//            {
//                const string alphaKey = "K64STGMLKTAXGBC7";
//                StockPriceService = new StockPriceService(StockPrices, alphaKey, TimeSpan.FromDays(1));
//            }
//            catch (Exception ex)
//            {
//                Log.Warn("App.InitAfterLogin", "StockPrice init failed: " + ex.Message);
//                StockPriceService = null;
//            }

//            InvestmentService = new InvestmentService(Investments, StockPriceService, CurrencyService);
//            PortfolioService = new PortfolioService(Investments, StockPriceService);

//            _fullInited = true;
//        }

//        // --- Status helpers ---
//        public static bool IsCoreReady() =>
//            _coreInited && UserService != null && Db != null;

//        public static bool IsFullyReady() =>
//            _fullInited &&
//            UserService != null &&
//            CategoryService != null &&
//            ExpenseService != null &&
//            BudgetService != null &&
//            InvestmentService != null &&
//            PortfolioService != null &&
//            Db != null;
//        // שים לב: בכוונה לא מכריחים CurrencyService != null כדי שכשל רשת לא יעכב מסכים.
//    }
//}
using System;
using System.Threading.Tasks;
using SQLite;
using Xamarin.Essentials;
using Android.Util;

using MoneyMap.DAL;
using MoneyMap.Services;

namespace MoneyMap
{
    /// <summary>
    /// מחלקת-על סטטית שמחזיקה "Singletonים" (אובייקט אחד לכל האפליקציה) של:
    /// 1) חיבור ל-DB
    /// 2) Repositories (DAL)
    /// 3) Services (לוגיקה)
    ///
    /// המטרה: שכל מסך (Activity) יוכל להשתמש ב-App.XYZ במקום ליצור לבד תלותים.
    /// </summary>
    public static class App
    {
        /// <summary>
        /// חיבור אסינכרוני ל-SQLite.
        /// WHY: זה הלב של האפליקציה — כל הקריאות לנתונים עוברות דרך Db (בעקיפין דרך Repositories).
        /// </summary>
        public static SQLiteAsyncConnection Db { get; private set; }

        // --- DAL ---
        // DAL = Data Access Layer: שכבה שמדברת ישירות עם ה-DB (טבלאות/שאילתות/CRUD).
        public static UserRepository Users { get; private set; }
        public static InvestmentRepository Investments { get; private set; }
        public static BudgetRepository Budgets { get; private set; }
        public static ExpenseRepository Expenses { get; private set; }
        public static CategoryRepository Category { get; private set; }
        public static StockPriceCacheRepository StockPrices { get; private set; }
        public static UserSettingsRepository UserSettingsRepo { get; private set; }
        public static CurrencyRateRepository CurrencyRateRepo { get; private set; }

        // --- Services ---
        // Services = לוגיקה עסקית: משתמשים ב-Repositories כדי להפעיל חוקים/חישובים/תיאומים בין טבלאות.
        public static UserService UserService { get; private set; }
        public static CategoryService CategoryService { get; private set; }
        public static ExpenseService ExpenseService { get; private set; }
        public static BudgetService BudgetService { get; private set; }
        public static StockPriceService StockPriceService { get; private set; }
        public static InvestmentService InvestmentService { get; private set; }
        public static PortfolioService PortfolioService { get; private set; }
        public static CurrencyService CurrencyService { get; private set; }

        /// <summary>
        /// _coreInited = מאתחלים את המינימום הדרוש למסכי Authentication (Login/Register):
        /// - DB
        /// - Users Repository
        /// - UserService
        /// </summary>
        private static bool _coreInited = false; // DB + UserService

        /// <summary>
        /// _fullInited = מאתחלים את כל שאר השירותים שאחרי התחברות:
        /// - Budgets/Expenses/Investments וכו'
        /// - Services נוספים
        /// - שירותים שעשויים לכלול רשת/מטמון (מניות/מטבע)
        /// </summary>
        private static bool _fullInited = false; // יתר השירותים

        /// <summary>
        /// נקודת כניסה כללית לאתחול.
        /// WHAT: תדאג שהאפליקציה תהיה "מוכנה" בהתאם למצב המשתמש:
        /// - תמיד תוודא Core מוכן
        /// - ואם יש משתמש שמור (LoggedInUserId) -> תאתחל גם את יתר השירותים
        /// HOW: בודקים Preferences, ואם יש userId -> InitAfterLoginAsync
        /// </summary>
        public static async Task InitAsync()
        {
            // אם כבר אתחלנו הכול בעבר — אין מה לעשות.
            if (IsFullyReady()) return;

            // אם עדיין לא אתחלנו Core (DB + UserService) — עושים את זה עכשיו.
            if (!_coreInited)
                await InitForAuthAsync();

            // Preferences = אחסון מקומי קטן (key/value).
            // כאן אתה שומר userId כשהמשתמש מתחבר, כדי לזכור אותו בפעמים הבאות.
            var savedUserId = Preferences.Get("LoggedInUserId", 0);

            // אם יש userId שמור, וה-Full עדיין לא אותחל — עוברים לאתחול של "אחרי התחברות".
            if (savedUserId > 0 && !_fullInited)
                await InitAfterLoginAsync();
        }

        /// <summary>
        /// אתחול מינימלי למסכי Authentication (Login/Register).
        /// WHY: מסכי Login לא צריכים את כל המערכת (השקעות/תקציב/מניות),
        /// רק DB + Users כדי לבדוק/ליצור משתמש.
        /// </summary>
        public static async Task InitForAuthAsync()
        {
            if (_coreInited) return;

            // DatabaseContext.InitAsync() בדרך כלל:
            // - יוצר/בודק את קובץ ה-DB
            // - יוצר טבלאות (CreateTable) אם צריך
            await DatabaseContext.InitAsync();

            // קבלת החיבור ל-SQLiteAsyncConnection ושמירה אותו ב-Db לשימוש גלובלי.
            Db = DatabaseContext.GetConnection();

            // יצירת Repository למשתמשים + שירות מעליו.
            Users = new UserRepository(Db);
            UserService = new UserService(Users);

            // סימון ש-Core מוכן.
            _coreInited = true;
        }

        /// <summary>
        /// אתחול מלא אחרי שהמשתמש כבר מחובר.
        /// WHAT: בונה את כל ה-Repositories וה-Services של התקציב/השקעות, ובנוסף שירותים שקשורים לרשת (מטבע/מניות).
        /// </summary>
        public static async Task InitAfterLoginAsync()
        {
            if (_fullInited) return;

            // ביטחון: אם Core לא מוכן, נאתחל אותו קודם.
            if (!_coreInited)
                await InitForAuthAsync();

            // --------------------
            // 1) DAL (Repositories)
            // --------------------
            Investments = new InvestmentRepository(Db);

            // Migration helper:
            // WHY: אם בעבר לטבלת השקעות לא היו עמודות מטבע (Currency) ואתה מוסיף אותן עכשיו,
            // צריך לוודא שהעמודות קיימות כדי שהאפליקציה לא תיפול ב-SQL errors.
            var invMigrator = new InvestmentMigrationHelper(Db);
            await invMigrator.EnsureInvestmentCurrencyColumnsAsync();

            Budgets = new BudgetRepository(Db);
            Expenses = new ExpenseRepository(Db);
            Category = new CategoryRepository(Db);

            // StockPrices = טבלת מטמון למחירים כדי לא לפנות ל-API בכל פעם.
            StockPrices = new StockPriceCacheRepository(Db);

            // הגדרות משתמש (למשל מטבע תצוגה נבחר)
            UserSettingsRepo = new UserSettingsRepository(Db);

            // מטמון שערי מטבע ב-DB (כדי לעבוד גם בלי אינטרנט)
            CurrencyRateRepo = new CurrencyRateRepository(Db);

            // --------------------
            // 2) Services "ללא רשת"
            // --------------------
            // אלה שירותים שמסתמכים רק על DB ולא צריכים אינטרנט.
            CategoryService = new CategoryService(Category);
            ExpenseService = new ExpenseService(Expenses);

            // BudgetService תלוי ב-3 Repositories כדי לחשב סיכומים:
            // - תקציבים (planned)
            // - הוצאות (spent)
            // - קטגוריות (names)
            BudgetService = new BudgetService(Budgets, Expenses, Category);

            // --------------------
            // 3) CurrencyService (כולל רשת + מטמון)
            // --------------------
            // WHY: לא רוצים שהאפליקציה תיפול או תיתקע אם אין אינטרנט.
            // לכן בנית כאן try/catch:
            // - אם הרענון הראשון נכשל: ממשיכים לעבוד עם מטמון קיים / ILS.
            // - אם הבנאי עצמו נכשל: CurrencyService = null (ומסכים לא חייבים אותו).
            try
            {
                // IExchangeRatesClient = שכבה שמביאה שערי מטבע מהאינטרנט.
                // בפועל המימוש הוא ExchangeRatesClient.
                IExchangeRatesClient boiClient = new ExchangeRatesClient(); // ראה קובץ למטה

                CurrencyService = new CurrencyService(
                    UserSettingsRepo,   // מאיפה קוראים/שומרים הגדרות משתמש (למשל מטבע תצוגה)
                    CurrencyRateRepo,   // איפה שומרים/קוראים מטמון של שערי מטבע
                    boiClient,          // הלקוח שמביא שערים מהאינטרנט
                    TimeSpan.FromDays(1) // TTL 24 שעות: כמה זמן שערים "טריים"
                );

                // ניסיון רענון ראשון:
                // WHAT: לנסות להביא שערי מטבע עכשיו (בכניסה אחרי login),
                // כדי שהאפליקציה תציג המרות מדויקות כבר עכשיו.
                // HOW: אם נכשל (אין אינטרנט / API נפל) -> רק לוג, לא מפילים.
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
                // אם משהו ממש השתבש בבנייה של CurrencyService (לא רק רשת),
                // אנחנו מוותרים עליו וממשיכים: מסכים לא צריכים להיתקע.
                Log.Warn("App.InitAfterLogin", "Currency service ctor failed: " + exCtor.Message);
                CurrencyService = null; // רק אם הבנאי עצמו נכשל
            }

            // --------------------
            // 4) StockPriceService (כולל רשת + מטמון)
            // --------------------
            // WHY: מחירי מניות מגיעים מ-Alpha Vantage, אבל שומרים אותם ב-DB כדי לא להיתקע על rate limits.
            try
            {
                // מפתח API ל-Alpha Vantage.
                // הערה: עדיף לא לשים מפתח קשיח בקוד (Security), אבל לפרויקט לימודי זה מקובל.
                const string alphaKey = "K64STGMLKTAXGBC7";

                // TTL יום: מחיר מניה נשמר כתקף ל-24 שעות לפני שמנסים לרענן.
                StockPriceService = new StockPriceService(StockPrices, alphaKey, TimeSpan.FromDays(1));
            }
            catch (Exception ex)
            {
                // אם נכשל בבנייה (למשל בעיה פנימית/מפתח/פורמט),
                // לא מפילים את האפליקציה — רק מבטלים את השירות.
                Log.Warn("App.InitAfterLogin", "StockPrice init failed: " + ex.Message);
                StockPriceService = null;
            }

            // --------------------
            // 5) Services שתלויים במניות/מטבע
            // --------------------
            // InvestmentService משתמש גם במחירי מניות וגם בשערי מטבע כדי "להעשיר" השקעות
            // (רווח/הפסד, המרות וכו').
            // שים לב: StockPriceService/CurrencyService יכולים להיות null -> השירות צריך להתמודד עם זה.
            InvestmentService = new InvestmentService(Investments, StockPriceService, CurrencyService);

            // PortfolioService מחשב שווי תיק וסיכומים.
            // כאן אתה נותן לו Repos + StockPriceService (למחירים).
            PortfolioService = new PortfolioService(Investments, StockPriceService);

            // סימון ש-Full מוכן.
            _fullInited = true;
        }

        // --- Status helpers ---
        /// <summary>
        /// בדיקה אם "Core" מוכן (DB + UserService).
        /// שימוש: מסכי Login/Register יכולים לעבוד רק עם זה.
        /// </summary>
        public static bool IsCoreReady() =>
            _coreInited && UserService != null && Db != null;

        /// <summary>
        /// בדיקה אם כל המערכת מוכנה למסכים אחרי התחברות.
        /// שים לב: בכוונה לא דורשים CurrencyService != null כדי שכשל רשת לא יעכב מסכים.
        /// </summary>
        public static bool IsFullyReady() =>
            _fullInited &&
            UserService != null &&
            CategoryService != null &&
            ExpenseService != null &&
            BudgetService != null &&
            InvestmentService != null &&
            PortfolioService != null &&
            Db != null;
        // שים לב: בכוונה לא מכריחים CurrencyService != null כדי שכשל רשת לא יעכב מסכים.
    }
}
