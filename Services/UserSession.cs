namespace MoneyMap.Services
{
    public static class UserSession
    {
        public static int? LoggedInUserId { get; set; }
        public static string LoggedInUserName { get; set; }
        public static string FirebaseUid { get; set; }
        public static bool IsAdmin { get; set; }

        public static void SetUser(int userId, string fullName, string firebaseUid = null)
        {
            LoggedInUserId = userId;
            LoggedInUserName = fullName;
            FirebaseUid = firebaseUid;
            IsAdmin = false;
        }

        public static void SetAdmin(string adminName) // מגדירה התחברות של מנהל מערכת
        {
            LoggedInUserId = null;
            LoggedInUserName = adminName;
            FirebaseUid = null;
            IsAdmin = true;
        }

        public static void Clear()
        {
            LoggedInUserId = null;
            LoggedInUserName = null;
            FirebaseUid = null;
            IsAdmin = false;
        }

        public static bool IsLoggedIn => IsAdmin || LoggedInUserId != null;
    }
}