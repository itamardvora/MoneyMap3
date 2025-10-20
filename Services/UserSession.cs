using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoneyMap.Services
{
    public class UserSession
    {
        public static int? LoggedInUserId { get; set; }
        public static string LoggedInUserName { get; set; }

        public static void SetUser(int userId, string fullName)
        {
            LoggedInUserId = userId;
            LoggedInUserName = fullName;
        }

        public static void Clear()
        {
            LoggedInUserId = null;
            LoggedInUserName = null;
        }

        public static bool IsLoggedIn => LoggedInUserId != null;
    }
}