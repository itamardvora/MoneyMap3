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
        private readonly UserRepository _users;
        private readonly FirebaseAuthService _firebaseAuth;

        public const string AdminEmail = "admin@moneymap.local";
        public const string AdminPassword = "Admin123!";
        public const string AdminDisplayName = "אדמין";

        public UserService(UserRepository userRepository, FirebaseAuthService firebaseAuth)
        {
            _users = userRepository;
            _firebaseAuth = firebaseAuth;
        }

        public int? CurrentUserId => UserSession.LoggedInUserId;

        public bool IsAdminCredentials(string email, string password)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            password ??= string.Empty;

            return email == AdminEmail.ToLowerInvariant() && password == AdminPassword;
        }

        public void SignInAsAdmin()
        {
            UserSession.SetAdmin(AdminDisplayName);
            Preferences.Remove("LoggedInUserId");
            Preferences.Remove("LoggedInUserName");
        }

        public async Task<bool> IsEmailTaken(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
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

                App.BackupState?.MarkBudgetsChanged();
                App.BackupState?.MarkExpensesChanged();
                App.BackupState?.MarkInvestmentsChanged();
                App.BackupState?.MarkIncomesChanged();
                App.BackupState?.MarkCategoriesChanged();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "שגיאה במחיקת המשתמש: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Error, User User)> TryRegisterWithFirebaseAsync(
            string fullName,
            string email,
            string password,
            DateTime birthDate)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "יש להזין שם מלא", null);

            if (string.IsNullOrWhiteSpace(email))
                return (false, "יש להזין אימייל", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "יש להזין סיסמה", null);

            if (birthDate == default)
                return (false, "תאריך לידה לא תקין", null);

            email = email.Trim().ToLowerInvariant();

            var localExisting = await _users.GetUserByEmail(email);
            if (localExisting != null)
                return (false, "האימייל כבר קיים במסד המקומי", null);

            var firebase = await _firebaseAuth.RegisterWithEmailPasswordAsync(email, password);
            if (!firebase.Success)
                return (false, firebase.ErrorMessage, null);

            var user = new User
            {
                FullName = fullName.Trim(),
                Email = email,
                Password = null,
                FirebaseUid = firebase.FirebaseUid,
                BirthDate = birthDate,
                CreatedAt = DateTime.Now,
                Role = "User"
            };

            await _users.AddUser(user);
            return (true, null, user);
        }

        public async Task<(bool Success, string Error, User User)> AuthenticateWithFirebaseAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, "נא להזין אימייל", null);

            if (string.IsNullOrWhiteSpace(password))
                return (false, "נא להזין סיסמה", null);

            email = email.Trim().ToLowerInvariant();

            var firebase = await _firebaseAuth.SignInWithEmailPasswordAsync(email, password);
            if (!firebase.Success)
                return (false, firebase.ErrorMessage, null);

            var user = await _users.GetUserByFirebaseUid(firebase.FirebaseUid);

            if (user == null)
            {
                user = await _users.GetUserByEmail(firebase.Email);

                if (user != null && string.IsNullOrWhiteSpace(user.FirebaseUid))
                {
                    await _users.LinkFirebaseUidAsync(user.UserID, firebase.FirebaseUid);
                    user.FirebaseUid = firebase.FirebaseUid;
                }
            }

            if (user == null)
                return (false, "המשתמש קיים ב-Firebase אבל לא קיים במסד המקומי", null);

            SaveFirebaseSession(firebase);

            UserSession.SetUser(user.UserID, user.FullName, firebase.FirebaseUid);
            Preferences.Set("LoggedInUserId", user.UserID);
            Preferences.Set("LoggedInUserName", user.FullName ?? "");

            return (true, null, user);
        }

        private void SaveFirebaseSession(FirebaseAuthResult auth)
        {
            Preferences.Set("FirebaseUid", auth.FirebaseUid ?? "");
            Preferences.Set("FirebaseEmail", auth.Email ?? "");
            Preferences.Set("FirebaseIdToken", auth.IdToken ?? "");
            Preferences.Set("FirebaseRefreshToken", auth.RefreshToken ?? "");

            var expiresAt = DateTimeOffset.UtcNow
                .AddSeconds(auth.ExpiresInSeconds <= 0 ? 3600 : auth.ExpiresInSeconds)
                .ToUnixTimeSeconds();

            Preferences.Set("FirebaseIdTokenExpiresAtUnix", expiresAt);
        }
    }
}