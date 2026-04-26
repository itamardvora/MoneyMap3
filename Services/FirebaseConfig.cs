namespace MoneyMap.Services
{
    public static class FirebaseConfig
    {
        public const string ApiKey = "AIzaSyAlJOsg7ykbrx61SAWBhhnyKARM1GgG0sI";

        public const string RealtimeDatabaseUrl = "https://moneymap-a96f8-default-rtdb.firebaseio.com";

        public static string DatabaseBaseUrl =>
            (RealtimeDatabaseUrl ?? string.Empty).Trim().TrimEnd('/');
    }
}