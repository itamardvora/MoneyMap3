// MoneyMap/Activities/LoginActivity.cs
using System;
using Android.App;
using Android.OS;
using Android.Widget;
using MoneyMap.Services;
using Xamarin.Essentials;
using Android.Content;   // ← זה חסר


namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.LoginActivity",
        Label = "התחברות",
        Theme = "@style/AppTheme",
        Exported = true)]
    public class LoginActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_login);

            var emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            var passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            var loginButton = FindViewById<Button>(Resource.Id.loginButton);
            var goToRegisterButton = FindViewById<Button>(Resource.Id.goToRegisterButton);

            goToRegisterButton.Click += (s, e) => StartActivity(typeof(RegisterActivity));

            await App.InitForAuthAsync(); // ליבה בלבד

            loginButton.Click += async (s, e) =>
            {
                var email = (emailInput?.Text ?? "").Trim();
                var password = passwordInput?.Text ?? "";

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Toast.MakeText(this, "אנא מלא אימייל וסיסמה", ToastLength.Short).Show();
                    return;
                }

                try
                {
                    loginButton.Enabled = false;

                    var user = await App.UserService.LoginUser(email, password);
                    if (user == null)
                    {
                        Toast.MakeText(this, "מייל או סיסמה לא נכונים", ToastLength.Short).Show();
                        return;
                    }

                    UserSession.LoggedInUserId = user.UserID;
                    UserSession.LoggedInUserName = user.FullName;
                    Preferences.Set("LoggedInUserId", user.UserID);
                    Preferences.Set("LoggedInUserName", user.FullName);

                    await App.InitAfterLoginAsync(); // שירותים מלאים

                    // תזמון יומי ב-06:00 והפעלה ראשונית
                    AlarmScheduler.ScheduleDaily(this, 6, 0);

                    var svc = new Intent(this, typeof(RatesSyncService));
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                        StartForegroundService(svc);
                    else
                        StartService(svc);

                    StartActivity(typeof(HomeActivity));
                    Finish();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה: " + ex.Message, ToastLength.Long).Show();
                }
                finally
                {
                    loginButton.Enabled = true;
                }
            };
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
