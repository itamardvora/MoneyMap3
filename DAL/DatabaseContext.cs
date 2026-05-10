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
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private static bool _initialized = false;

        public static SQLiteAsyncConnection GetConnection()
        {
            if (_database == null)
                throw new InvalidOperationException("DatabaseContext was not initialized. Call InitAsync() first.");

            return _database;
        }

        public static async Task InitAsync()
        {
            if (_initialized) return;

            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;

                var databasePath = Path.Combine(FileSystem.AppDataDirectory, "MoneyMap.db3");
                _database = new SQLiteAsyncConnection(databasePath);

                await _database.CreateTableAsync<User>();
                await _database.CreateTableAsync<Investment>();
                await _database.CreateTableAsync<Budget>();
                await _database.CreateTableAsync<Expense>();
                await _database.CreateTableAsync<Category>();
                await _database.CreateTableAsync<StockPrices>();
                await _database.CreateTableAsync<CurrencyRate>();

                // חדש: טבלת הכנסות
                await _database.CreateTableAsync<Income>();

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }
    }
}