using Android.App;
using Android.OS;
using Android.Widget;
using MoneyMap.Services;
using System;
using System.Threading.Tasks;

namespace MoneyMap.Activities
{
    [Activity(Label = "התחברות")]
    public class LoginActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_login);

            // מציאת רכיבים מה-XML
            var emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            var passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            var loginButton = FindViewById<Button>(Resource.Id.loginButton);

            loginButton.Click += async (s, e) =>
            {
                string email = emailInput.Text.Trim();
                string password = passwordInput.Text;

                try
                {
                    // משתמש בשירות מה-BLL
                    var user = await App.UserService.LoginUser(email, password);

                    if (user != null)
                    {
                        UserSession.LoggedInUserId = user.UserID;
                        UserSession.LoggedInUserName = user.FullName;

                        Toast.MakeText(this, $"ברוך הבא {user.FullName}", ToastLength.Short).Show();
                        await App.InitAsync();
                        StartActivity(typeof(HomeActivity));
                        Finish();
                    }
                    else
                    {
                        Toast.MakeText(this, "מייל או סיסמה לא נכונים", ToastLength.Short).Show();
                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה: " + ex.Message, ToastLength.Long).Show();
                }
            };


            var goToRegisterButton = FindViewById<Button>(Resource.Id.goToRegisterButton);
            goToRegisterButton.Click += (s, e) =>
            {

                StartActivity(typeof(RegisterActivity));
            };

        }

    }
}
