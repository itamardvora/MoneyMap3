using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Extensions;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Java.Util;
using MoneyMap.Models;
using Newtonsoft.Json.Linq;
using Xamarin.Essentials;

namespace MoneyMap.Services
{
    public static class FireBaseHelper
    {
        private static FirebaseAuth _auth;
        private static FirebaseFirestore _db;
        private static bool _initialized = false;

        private const string UsersCollection = "users";

        public static void InitializeFirebase(Context context)
        {
            if (_initialized)
                return;

            try
            {
                var existingApp = FirebaseApp.Instance;
                _auth = FirebaseAuth.Instance;
                _db = FirebaseFirestore.Instance;
                _initialized = true;
                return;
            }
            catch
            {
                // Firebase עדיין לא מאותחל, אז נאתחל אותו ידנית מהקובץ.
            }

            using (var stream = context.Assets.Open("google-services.json"))
            using (var reader = new StreamReader(stream))
            {
                var jsonText = reader.ReadToEnd();
                var json = JObject.Parse(jsonText);

                var projectId = json["project_info"]?["project_id"]?.ToString();
                var storageBucket = json["project_info"]?["storage_bucket"]?.ToString();
                var appId = json["client"]?[0]?["client_info"]?["mobilesdk_app_id"]?.ToString();
                var apiKey = json["client"]?[0]?["api_key"]?[0]?["current_key"]?.ToString();

                if (string.IsNullOrWhiteSpace(projectId) ||
                    string.IsNullOrWhiteSpace(appId) ||
                    string.IsNullOrWhiteSpace(apiKey))
                {
                    throw new Exception("google-services.json לא תקין או חסרים בו נתונים");
                }

                var optionsBuilder = new FirebaseOptions.Builder()
                    .SetApplicationId(appId)
                    .SetApiKey(apiKey)
                    .SetProjectId(projectId);

                if (!string.IsNullOrWhiteSpace(storageBucket))
                    optionsBuilder.SetStorageBucket(storageBucket);

                var options = optionsBuilder.Build();

                FirebaseApp.InitializeApp(context, options);
            }

            _auth = FirebaseAuth.Instance;
            _db = FirebaseFirestore.Instance;
            _initialized = true;
        }

        public static async Task<string> RegisterUserAsync(User user)
        {
            EnsureInitialized();

            if (user == null)
                throw new Exception("User is null");

            NormalizeUser(user);
            ValidateRegisterUser(user);

            var authResult = await _auth
                .CreateUserWithEmailAndPassword(user.Email, user.Password)
                .AsAsync<IAuthResult>();

            var firebaseUser = authResult.User;

            if (firebaseUser == null || string.IsNullOrWhiteSpace(firebaseUser.Uid))
                throw new Exception("Firebase לא החזיר UID");

            user.FirebaseUid = firebaseUser.Uid;

            if (string.IsNullOrWhiteSpace(user.Role))
                user.Role = "User";

            if (user.CreatedAt == default)
                user.CreatedAt = DateTime.Now;

            await SaveUserLocallyAsync(user);
            await SaveUserToFirestoreAsync(user);

            SaveLocalSession(user);

            return user.FirebaseUid;
        }

        public static async Task<string> SignInUserAsync(string email, string password)
        {
            EnsureInitialized();

            email = (email ?? "").Trim().ToLowerInvariant();
            password = password ?? "";

            if (string.IsNullOrWhiteSpace(email))
                throw new Exception("אנא מלא אימייל");

            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("אנא מלא סיסמה");

            var authResult = await _auth
                .SignInWithEmailAndPassword(email, password)
                .AsAsync<IAuthResult>();

            var firebaseUser = authResult.User;

            if (firebaseUser == null || string.IsNullOrWhiteSpace(firebaseUser.Uid))
                throw new Exception("התחברות נכשלה");

            Preferences.Set("FirebaseUid", firebaseUser.Uid);
            Preferences.Set("FirebaseEmail", email);

            return firebaseUser.Uid;
        }

        public static async Task<User> GetUserByIdAsync(string firebaseUid)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(firebaseUid))
                return null;

            var localUser = await App.Users.GetUserByFirebaseUid(firebaseUid);
            if (localUser != null)
            {
                SaveLocalSession(localUser);
                return localUser;
            }

            var snapshot = await _db
                .Collection(UsersCollection)
                .Document(firebaseUid)
                .Get()
                .AsAsync<DocumentSnapshot>();

            if (snapshot == null || !snapshot.Exists())
                return null;

            var user = new User
            {
                FirebaseUid = firebaseUid,
                FullName = snapshot.GetString("fullName"),
                Email = snapshot.GetString("email"),
                Password = snapshot.GetString("password"),
                BirthDate = ParseDate(snapshot.GetString("birthDate"), DateTime.Today),
                CreatedAt = ParseDate(snapshot.GetString("createdAt"), DateTime.Now),
                Role = snapshot.GetString("role") ?? "User"
            };

            if (string.IsNullOrWhiteSpace(user.Email))
                return null;

            await SaveUserLocallyAsync(user);
            SaveLocalSession(user);

            return user;
        }

        public static async Task SaveUserToFirestoreAsync(User user)
        {
            EnsureInitialized();

            if (user == null)
                return;

            if (string.IsNullOrWhiteSpace(user.FirebaseUid))
                throw new Exception("חסר FirebaseUid למשתמש");

            var map = new HashMap();

            map.Put("localUserId", user.UserID);
            map.Put("firebaseUid", user.FirebaseUid ?? "");
            map.Put("fullName", user.FullName ?? "");
            map.Put("email", user.Email ?? "");

            // לגרסת הבגרות בלבד, לפי הדרישה שלך כרגע.
            map.Put("password", user.Password ?? "");

            map.Put("birthDate", user.BirthDate.ToString("o"));
            map.Put("createdAt", user.CreatedAt.ToString("o"));
            map.Put("role", string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role);

            await _db
                .Collection(UsersCollection)
                .Document(user.FirebaseUid)
                .Set(map)
                .AsAsync();
        }

        public static async Task UpdateUserAsync(User user)
        {
            EnsureInitialized();

            if (user == null)
                return;

            await App.Users.UpdateUser(user);
            await SaveUserToFirestoreAsync(user);
            SaveLocalSession(user);
        }

        public static void SignOut()
        {
            if (_auth != null)
                _auth.SignOut();

            Preferences.Remove("RememberMe");
            Preferences.Remove("IsLoggedIn");

            Preferences.Remove("LoggedInUserId");
            Preferences.Remove("LoggedInUserName");
            Preferences.Remove("LoggedInUserEmail");
            Preferences.Remove("LoggedInUserRole");

            Preferences.Remove("FirebaseUid");
            Preferences.Remove("FirebaseEmail");

            UserSession.Clear();
        }

        private static async Task SaveUserLocallyAsync(User user)
        {
            var existingByUid = string.IsNullOrWhiteSpace(user.FirebaseUid)
                ? null
                : await App.Users.GetUserByFirebaseUid(user.FirebaseUid);

            if (existingByUid != null)
            {
                existingByUid.FullName = user.FullName;
                existingByUid.Email = user.Email;
                existingByUid.Password = user.Password;
                existingByUid.BirthDate = user.BirthDate;
                existingByUid.CreatedAt = user.CreatedAt;
                existingByUid.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;

                await App.Users.UpdateUser(existingByUid);
                user.UserID = existingByUid.UserID;
                return;
            }

            var existingByEmail = await App.Users.GetUserByEmail(user.Email);

            if (existingByEmail != null)
            {
                existingByEmail.FullName = user.FullName;
                existingByEmail.Password = user.Password;
                existingByEmail.BirthDate = user.BirthDate;
                existingByEmail.FirebaseUid = user.FirebaseUid;
                existingByEmail.CreatedAt = user.CreatedAt;
                existingByEmail.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;

                await App.Users.UpdateUser(existingByEmail);
                user.UserID = existingByEmail.UserID;
                return;
            }

            await App.Users.AddUser(user);
        }

        private static void SaveLocalSession(User user)
        {
            if (user == null)
                return;

            Preferences.Set("LoggedInUserId", user.UserID);
            Preferences.Set("LoggedInUserName", user.FullName ?? "");
            Preferences.Set("LoggedInUserEmail", user.Email ?? "");
            Preferences.Set("LoggedInUserRole", user.Role ?? "User");
            Preferences.Set("FirebaseUid", user.FirebaseUid ?? "");
            Preferences.Set("FirebaseEmail", user.Email ?? "");

            UserSession.LoggedInUserId = user.UserID;
            UserSession.LoggedInUserName = user.FullName;
        }

        private static void NormalizeUser(User user)
        {
            user.FullName = (user.FullName ?? "").Trim();
            user.Email = (user.Email ?? "").Trim().ToLowerInvariant();
            user.Password = user.Password ?? "";
            user.Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
        }

        private static void ValidateRegisterUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.FullName))
                throw new Exception("שם מלא חסר");

            if (string.IsNullOrWhiteSpace(user.Email))
                throw new Exception("אימייל חסר");

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new Exception("סיסמה חסרה");

            if (user.Password.Length < 6)
                throw new Exception("הסיסמה חייבת להיות לפחות 6 תווים");

            if (user.BirthDate == default)
                throw new Exception("תאריך לידה לא תקין");
        }

        private static void EnsureInitialized()
        {
            if (!_initialized || _auth == null || _db == null)
                throw new Exception("Firebase לא מאותחל. קרא קודם ל-InitializeFirebase");
        }

        private static DateTime ParseDate(string value, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            if (DateTime.TryParse(
                    value,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var result))
            {
                return result;
            }

            return fallback;
        }



        
        public static async Task BackupAllUserDataAsync()
        {
            EnsureInitialized();

            var firebaseUid = Preferences.Get("FirebaseUid", "");
            var localUserId = UserSession.LoggedInUserId ?? Preferences.Get("LoggedInUserId", 0);

            if (string.IsNullOrWhiteSpace(firebaseUid))
                throw new Exception("אין FirebaseUid. המשתמש לא מחובר ל-Firebase.");

            if (localUserId <= 0)
                throw new Exception("אין משתמש מקומי מחובר.");

            await BackupInvestmentsAsync(localUserId, firebaseUid);
            await BackupBudgetsAsync(localUserId, firebaseUid);
            await BackupExpensesAsync(localUserId, firebaseUid);
            await BackupCategoriesAsync(localUserId, firebaseUid);
            await BackupIncomesAsync(localUserId, firebaseUid);
        }

        
        public static async Task BackupInvestmentsAsync(int localUserId, string firebaseUid)
        {
            EnsureInitialized();

            if (App.Investments == null)
                return;

            var rows = await App.Investments.GetAllByUser(localUserId);

            await ClearCollectionAsync(firebaseUid, "investments");

            foreach (var x in rows)
            {
                var map = new HashMap();

                map.Put("localId", x.InvestmentID);
                map.Put("userId", x.UserID);
                map.Put("stockSymbol", x.StockSymbol ?? "");
                map.Put("buyDate", x.BuyDate.ToString("o"));
                map.Put("buyPrice", Convert.ToDouble(x.BuyPrice));
                map.Put("quantity", Convert.ToDouble(x.Quantity));
                map.Put("createdAt", x.CreatedAt.ToString("o"));
                map.Put("originalCurrency", string.IsNullOrWhiteSpace(x.OriginalCurrency) ? "ILS" : x.OriginalCurrency);
                map.Put("fxRateToIlsAtPurchase", Convert.ToDouble(x.FxRateToIlsAtPurchase));
                map.Put("totalInIls", Convert.ToDouble(x.TotalInIls));
                map.Put("receiptImagePath", x.ReceiptImagePath ?? "");

                await _db
                    .Collection(UsersCollection)
                    .Document(firebaseUid)
                    .Collection("investments")
                    .Document(x.InvestmentID.ToString())
                    .Set(map)
                    .AsAsync();
            }
        }

        
        public static async Task BackupBudgetsAsync(int localUserId, string firebaseUid)
        {
            EnsureInitialized();

            if (App.Budgets == null)
                return;

            var rows = await App.Budgets.GetUserBudgets(localUserId, null);

            await ClearCollectionAsync(firebaseUid, "budgets");

            foreach (var x in rows)
            {
                var map = new HashMap();

                map.Put("localId", x.BudgetID);
                map.Put("userId", x.UserID);
                map.Put("categoryId", x.CategoryID);
                map.Put("monthlyLimit", Convert.ToDouble(x.MonthlyLimit));
                map.Put("budgetMonth", x.BudgetMonth.ToString("o"));
                map.Put("createdAt", x.CreatedAt.ToString("o"));

                await _db
                    .Collection(UsersCollection)
                    .Document(firebaseUid)
                    .Collection("budgets")
                    .Document(x.BudgetID.ToString())
                    .Set(map)
                    .AsAsync();
            }
        }

       
        public static async Task BackupExpensesAsync(int localUserId, string firebaseUid)
        {
            EnsureInitialized();

            if (App.Expenses == null)
                return;

            var rows = await App.Expenses.GetAllByUser(localUserId);

            await ClearCollectionAsync(firebaseUid, "expenses");

            foreach (var x in rows)
            {
                var map = new HashMap();

                map.Put("localId", x.ExpenseID);
                map.Put("userId", x.UserID);
                map.Put("categoryId", x.CategoryID);
                map.Put("amount", Convert.ToDouble(x.Amount));
                map.Put("date", x.Date.ToString("o"));
                map.Put("reason", x.Reason ?? "");
                map.Put("createdAt", x.CreatedAt.ToString("o"));
                map.Put("receiptPath", x.ReceiptPath ?? "");

                await _db
                    .Collection(UsersCollection)
                    .Document(firebaseUid)
                    .Collection("expenses")
                    .Document(x.ExpenseID.ToString())
                    .Set(map)
                    .AsAsync();
            }
        }

        
        public static async Task BackupCategoriesAsync(int localUserId, string firebaseUid)
        {
            EnsureInitialized();

            if (App.Category == null)
                return;

            var rows = await App.Category.GetCategoriesForUser(localUserId);

            await ClearCollectionAsync(firebaseUid, "categories");

            foreach (var x in rows)
            {
                var map = new HashMap();

                map.Put("localId", x.CategoryID);
                map.Put("categoryName", x.CategoryName ?? "");
                map.Put("isSystem", x.IsSystem);
                map.Put("createdByUserId", x.CreatedByUserID.HasValue ? x.CreatedByUserID.Value : 0);

                await _db
                    .Collection(UsersCollection)
                    .Document(firebaseUid)
                    .Collection("categories")
                    .Document(x.CategoryID.ToString())
                    .Set(map)
                    .AsAsync();
            }
        }

        
        public static async Task BackupIncomesAsync(int localUserId, string firebaseUid)
        {
            EnsureInitialized();

            if (App.Incomes == null)
                return;

            var rows = await App.Incomes.GetAllByUser(localUserId);

            await ClearCollectionAsync(firebaseUid, "incomes");

            foreach (var x in rows)
            {
                var map = new HashMap();

                map.Put("localId", x.IncomeID);
                map.Put("userId", x.UserID);
                map.Put("amount", Convert.ToDouble(x.Amount));
                map.Put("date", x.Date.ToString("o"));
                map.Put("source", x.Source ?? "");
                map.Put("createdAt", x.CreatedAt.ToString("o"));

                await _db
                    .Collection(UsersCollection)
                    .Document(firebaseUid)
                    .Collection("incomes")
                    .Document(x.IncomeID.ToString())
                    .Set(map)
                    .AsAsync();
            }
        }

        
        private static async Task ClearCollectionAsync(string firebaseUid, string collectionName)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(firebaseUid))
                return;

            if (string.IsNullOrWhiteSpace(collectionName))
                return;

            var snapshot = await _db
                .Collection(UsersCollection)
                .Document(firebaseUid)
                .Collection(collectionName)
                .Get()
                .AsAsync<QuerySnapshot>();

            if (snapshot == null)
                return;

            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.Delete().AsAsync();
            }
        }
    }

}