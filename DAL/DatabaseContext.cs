using SQLite;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using MoneyMap.Models;

namespace MoneyMap.DAL
{
    public static class DatabaseContext
    {
        private static SQLiteAsyncConnection _database;// משתנה שיש בוא את החיבור לדאטה בייס
        
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);// דואג שיהיה כל פעם רק THEARD אחד ולא כמה מאתחלים את הDB 
        private static bool _initialized = false;// בודק האם הטבלאות מאותחלות 

        public static SQLiteAsyncConnection GetConnection()
        {
            if (_database == null)
                throw new InvalidOperationException("DatabaseContext was not initialized(מאותחל). Call InitAsync() first.");
            return _database;
        }
        /// אתחול הבסיס נתונים. יוצר קובץ DB אם צריך וטבלאות. 
        

        public static async Task InitAsync()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;// אם דאטה בייס מאותחל לא לעשות כלום

                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "MoneyMap.db3"); //יצרית הדאטה בייס במידה ולא קיים
                _database = new SQLiteAsyncConnection(databasePath);

                // יצירת טבלאות שלא קיימות 
                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Investment>();
                await _database.CreateTableAsync<Budget>();
                await _database.CreateTableAsync<Expense>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<StockPrices>();
                await _database.CreateTableAsync<UserSettings>();
                await _database.CreateTableAsync<CurrencyRate>();

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }
    }
}
