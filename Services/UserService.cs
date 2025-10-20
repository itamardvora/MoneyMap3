using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MoneyMap.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using MoneyMap.DAL;


namespace MoneyMap.Services
{
    public class UserService

    {

        private readonly UserRepository _users;

        public UserService(UserRepository userRepository)
        {
            _users = userRepository;
        }
        public int? CurrentUserId => UserSession.LoggedInUserId;

        // --- מתודות בסיסיות (כבר היו לך) ---
        public async Task<bool> IsEmailTaken(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var existing = await _users.GetUserByEmail(email);
            return existing != null;
        }

        public async Task<User> RegisterUser(string fullName, string email, string password, DateTime birthDate)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.");
            if (birthDate == default)
                throw new ArgumentException("Birth date is invalid.");

            var exists = await _users.GetUserByEmail(email);
            if (exists != null)
                throw new InvalidOperationException("Email is already registered.");

            var user = new User
            {
                FullName = fullName,
                Email = email.Trim().ToLower(),
                Password = password,  // כרגע PlainText, בעתיד מומלץ להצפין
                BirthDate = birthDate,
                CreatedAt = DateTime.Now,
                Role = "User"
            };

            await _users.AddUser(user);
            return user;
        }

        public Task<User> LoginUser(string email, string password)
        {
            return _users.ValidateLogin(email.Trim().ToLower(), password);
        }

        public Task<User> GetUserProfile(int userId)
        {
            return _users.GetUserById(userId);
        }

        // --- ✅ פונקציות חדשות לשימוש מה-UI ---
        // החזרה "עדינה" בלי לזרוק חריגות
        public async Task<(bool Success, string Error, User User)> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "נא להזין אימייל", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "נא להזין סיסמה", null);

            var user = await _users.ValidateLogin(email.Trim().ToLower(), password);

            if (user == null)
                return (false, "אימייל או סיסמה שגויים", null);

            return (true, null, user);
        }

        public async Task<(bool Success, string Error, User User)> TryRegisterAsync(string fullName, string email, string password, DateTime birthDate)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "יש להזין שם מלא", null);
            if (string.IsNullOrWhiteSpace(email))
                return (false, "יש להזין אימייל", null);
            if (string.IsNullOrWhiteSpace(password))
                return (false, "יש להזין סיסמה", null);
            if (birthDate == default)
                return (false, "תאריך לידה לא תקין", null);

            var exists = await _users.GetUserByEmail(email.Trim().ToLower());
            if (exists != null)
                return (false, "האימייל כבר רשום", null);

            var user = new User
            {
                FullName = fullName,
                Email = email.Trim().ToLower(),
                Password = password,
                BirthDate = birthDate,
                CreatedAt = DateTime.Now,
                Role = "User"
            };

            await _users.AddUser(user);
            return (true, null, user);
        }
    }
}