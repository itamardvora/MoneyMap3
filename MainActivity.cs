using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using MoneyMap.Activities;

namespace MoneyMap
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // קריאה יחידה שמאתחלת DB + DAL + Services
            await App.InitAsync();
            StartActivity(typeof(LoginActivity));
            Finish();
            // מכאן ה-UI משתמש בשירותים:
            // var user = await App.UserService.LoginUser(email, password);

        }
    }
}
