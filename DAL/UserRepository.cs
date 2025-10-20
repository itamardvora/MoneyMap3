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
using MoneyMap.Models;
using System.Threading.Tasks;
using MoneyMap.DAL;
using static Android.Provider.ContactsContract.CommonDataKinds;

namespace MoneyMap.DAL
{
    public class UserRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public UserRepository(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public Task AddUser(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
                user.Email = user.Email.Trim().ToLowerInvariant();

            return _db.InsertAsync(user);
        }

        public Task<User> GetUserByEmail(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
                email = email.Trim().ToLowerInvariant();

            return _db.Table<User>()
                      .Where(u => u.Email == email)
                      .FirstOrDefaultAsync();
        }

        public Task<User> GetUserById(int userId)
        {
            return _db.Table<User>()
                      .Where(u => u.UserID == userId)
                      .FirstOrDefaultAsync();
        }

        public async Task<User> ValidateLogin(string email, string password)
        {
            if (!string.IsNullOrWhiteSpace(email))
                email = email.Trim().ToLowerInvariant();

            var user = await _db.Table<User>()
                                .Where(u => u.Email == email)
                                .FirstOrDefaultAsync();

            if (user == null) return null;
            return user.Password == password ? user : null;
        }
    }
}

