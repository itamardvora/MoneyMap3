using System.Threading.Tasks;
using SQLite;
using MoneyMap.DAL;
using MoneyMap.Services;
using System;
using System.Text.Json;
using System.Globalization;

namespace MoneyMap
{
    public static class App
    {
        public static SQLiteAsyncConnection Db { get; private set; }

        // DAL
        public static UserRepository Users { get; private set; }
        public static InvestmentRepository Investments { get; private set; }
        public static BudgetRepository Budgets { get; private set; }
        public static ExpenseRepository Expenses { get; private set; }
        public static StockPriceCacheRepository StockPrices { get; private set; }
        
        public static CategoryRepository Category { get; private set; }

        // Services
        public static UserService UserService { get; private set; }
        
        public static CategoryService CategoryService { get; private set; }

        public static ExpenseService ExpenseService { get; private set; }

        public static BudgetService BudgetService { get; private set; }

        public static StockPriceService StockPriceService { get; private set; }

        public static InvestmentService InvestmentService { get; private set; }

        public static PortfolioService PortfolioService { get; private set; }
        public static object UserSession { get; internal set; }



        // תוסיף כאן Services נוספים בהמשך

        public static async Task InitAsync()
        {
            // 1) אתחול המסד (יוצר קובץ וטבלאות אם צריך)
            DatabaseContext.Init();
            Db = DatabaseContext.GetConnection();

            // 2) בניית DAL על אותו חיבור
            Users = new UserRepository(Db);
            Investments = new InvestmentRepository(Db);
            Budgets = new BudgetRepository(Db);
            Expenses = new ExpenseRepository(Db);
            StockPrices = new StockPriceCacheRepository(Db);
            Category = new CategoryRepository(Db);

            // 3) בניית Services מעל ה-DAL
            CategoryService = new CategoryService(Category);
            UserService = new UserService(Users);
            ExpenseService = new ExpenseService(Expenses);
            BudgetService = new BudgetService(Budgets, Expenses, Category);
            string alphaKey = "M97MHPDROY3XVBF1"; // לפיתוח. בהמשך שים ב-SecureStorage
            StockPriceService = new StockPriceService(StockPrices, "M97MHPDROY3XVBF1");

            StockPriceService = new StockPriceService(StockPrices, alphaKey, TimeSpan.FromDays(1));
            InvestmentService = new InvestmentService(Investments, StockPriceService);
            PortfolioService = new PortfolioService(Investments, StockPriceService);




        await Task.CompletedTask; // להשאיר חתימה async
        }
    }
}
