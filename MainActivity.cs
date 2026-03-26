// MoneyMap/MainActivity.cs
using Android.App;
using Android.OS;
using MoneyMap.Activities;

namespace MoneyMap
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Exported = true, Name = "com.companyname.moneymap.MainActivity")]
    public class MainActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            if (!App.IsCoreReady())
                await App.InitForAuthAsync();

            StartActivity(typeof(LoginActivity));
            Finish();
        }
    }
}
