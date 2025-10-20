using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SQLite;
using System;
using System.IO;
using Xamarin.Essentials;
using MoneyMap.Models; // כדי לגשת למחלקות שלך



namespace MoneyMap.DAL
{
    public class DatabaseContext
    {
        private static SQLiteAsyncConnection _database;

        public static async void Init()
        {
            if (_database != null)
                return;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "MoneyMap.db3");
            _database = new SQLiteAsyncConnection(databasePath);

            // יצירת הטבלאות במסד
            await _database.CreateTableAsync<User>();
            await _database.CreateTableAsync<Investment>();
            await _database.CreateTableAsync<Budget>();
            await _database.CreateTableAsync<Expense>();
            await _database.CreateTableAsync<Category>();
            await _database.CreateTableAsync<StockPrices>();
        }

        public static SQLiteAsyncConnection GetConnection()
        {
            return _database;
        }
    }
}