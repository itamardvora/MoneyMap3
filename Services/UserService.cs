using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MoneyMap.DAL;
using MoneyMap.Models;
using Xamarin.Essentials;


namespace MoneyMap.Services
{
    public class UserService
    {
        private readonly UserRepository _users; //שומר הפניה לUserRepository

        public const string AdminEmail = "admin@moneymap.local";
        public const string AdminPassword = "Admin123!";
        public const string AdminDisplayName = "אדמין";

        public UserService(UserRepository userRepository) //מקבל UserRepository מוכן ושומר אותו בשדה _users
        {
            _users = userRepository;
        }

        public int? CurrentUserId => UserSession.LoggedInUserId; // מחזיר את מזהה המשתמש שמחובר כרגע.

        public bool IsAdminCredentials(string email, string password)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            password = password ?? string.Empty;

            return email == AdminEmail.ToLowerInvariant() &&
                   password == AdminPassword;
        }

        public void SignInAsAdmin() // מחברת את המערכת כאדמין ומנקה נתוני התחברות של משתמש רגיל מהשמירה המקומית
        {
            UserSession.SetAdmin(AdminDisplayName);

            Preferences.Remove("LoggedInUserId");
            Preferences.Remove("LoggedInUserName");
            Preferences.Remove("LoggedInUserEmail");
            Preferences.Remove("LoggedInUserRole");
            Preferences.Remove("FirebaseUid");
            Preferences.Remove("FirebaseEmail");
        }

        public async Task<bool> IsEmailTaken(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var existing = await _users.GetUserByEmail(email);
            return existing != null;
        }

        public Task<User> GetUserProfile(int userId)
        {
            return _users.GetUserById(userId);
        }

        public async Task<(bool Success, string Error)> UpdateFullNameAsync(int userId, string fullName)
        {
            if (userId <= 0)
                return (false, "משתמש לא תקין");

            fullName = (fullName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "יש להזין שם מלא");

            var user = await _users.GetUserById(userId);

            if (user == null)
                return (false, "המשתמש לא נמצא");

            user.FullName = fullName;

            await _users.UpdateUser(user);

            UserSession.SetUser(user.UserID, user.FullName, user.FirebaseUid);

            Preferences.Set("LoggedInUserId", user.UserID);
            Preferences.Set("LoggedInUserName", user.FullName ?? "");
            Preferences.Set("LoggedInUserEmail", user.Email ?? "");
            Preferences.Set("LoggedInUserRole", user.Role ?? "User");
            Preferences.Set("FirebaseUid", user.FirebaseUid ?? "");
            Preferences.Set("FirebaseEmail", user.Email ?? "");

            return (true, null);
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            return _users.GetAllUsers();
        }

        public async Task<(bool Success, string Error)> DeleteUserCompletelyAsync(int userId)
        {
            if (userId <= 0)
                return (false, "משתמש לא תקין");

            var user = await _users.GetUserById(userId);

            if (user == null)
                return (false, "המשתמש לא נמצא");

            try
            {
                await App.Db.ExecuteAsync("DELETE FROM UserBudgetTable WHERE UserID = ?", userId);
                await App.Db.ExecuteAsync("DELETE FROM ExpensesTable WHERE UserID = ?", userId);
                await App.Db.ExecuteAsync("DELETE FROM UserInvestmentsTable WHERE UserID = ?", userId);
                await App.Db.ExecuteAsync("DELETE FROM IncomesTable WHERE UserID = ?", userId);
                await App.Db.ExecuteAsync("DELETE FROM CategoriesTable WHERE CreatedByUserID = ?", userId);

                await _users.DeleteUserByIdAsync(userId);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "שגיאה במחיקת המשתמש: " + ex.Message);
            }
        }
    }
}