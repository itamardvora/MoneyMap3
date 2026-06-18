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
        private static SQLiteAsyncConnection _database;
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1); // מנעול שמוודא שרק פעולה אחת יכולה לאתחל את מסד הנתונים בכל רגע, כדי למנוע אתחול כפול במקביל
        private static bool _initialized = false; // מונע אתחול מיותר שוש ושוב

        public static SQLiteAsyncConnection GetConnection() // פעולה שמחזירה את הקשר לטבלה 
        {
            if (_database == null)
                throw new InvalidOperationException("DatabaseContext was not initialized. Call InitAsync() first.");

            return _database;
        }

        public static async Task InitAsync() // מאתחל את ממסד הנתונים במידת הצורך 
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                    return;

                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "MoneyMap.db3"); // יצירת הנתיב לקובץ של הדאטה בייס
                _database = new SQLiteAsyncConnection(databasePath); // יצירת החיבור

                await _database.CreateTableAsync<User>(); // תוודא שהטבלה קיימת אם לא תיצור
                await _database.CreateTableAsync<Investment>();
                await _database.CreateTableAsync<Budget>();
                await _database.CreateTableAsync<Expense>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<StockPrices>();
                await _database.CreateTableAsync<CurrencyRate>();
                await _database.CreateTableAsync<Income>();

                _initialized = true;
            }
            finally
            {
                _initLock.Release();// לא משנה אם האתחול הצליח או נכשל תשחרר את הנעילה
            }
        }
    }
}