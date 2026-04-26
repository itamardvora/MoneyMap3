using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;

namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.RegisterActivity",
        Label = "הרשמה",
        Theme = "@style/AppTheme",
        Exported = true)]
    public class RegisterActivity : Activity
    {
        private EditText _fullNameInput;
        private EditText _emailInput;
        private EditText _passwordInput;
        private EditText _birthDateInput;
        private Button _registerButton;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_register);

            if (!App.IsCoreReady())
                await App.InitForAuthAsync();

            _fullNameInput = FindViewById<EditText>(Resource.Id.fullNameInput);
            _emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            _passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            _birthDateInput = FindViewById<EditText>(Resource.Id.birthDateInput);
            _registerButton = FindViewById<Button>(Resource.Id.registerButton);

            _birthDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            _birthDateInput.Focusable = false;
            _birthDateInput.Click += (s, e) =>
            {
                var t = DateTime.Today;
                var dp = new DatePickerDialog(this, (snd, ev) =>
                {
                    _birthDateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                }, t.Year, t.Month - 1, t.Day);
                dp.Show();
            };

            _registerButton.Click += async (s, e) => await OnRegisterAsync();
        }

        private async System.Threading.Tasks.Task OnRegisterAsync()
        {
            try
            {
                var fullName = (_fullNameInput?.Text ?? "").Trim();
                var email = (_emailInput?.Text ?? "").Trim().ToLowerInvariant();
                var password = _passwordInput?.Text ?? "";

                if (!DateTime.TryParse(_birthDateInput?.Text, out var birthDate))
                {
                    Toast.MakeText(this, "תאריך לידה לא תקין", ToastLength.Short).Show();
                    return;
                }

                var (ok, err, user) = await App.UserService.TryRegisterWithFirebaseAsync(fullName, email, password, birthDate);
                if (!ok)
                {
                    Toast.MakeText(this, err, ToastLength.Long).Show();
                    return;
                }

                Toast.MakeText(this, "נרשמת בהצלחה! אפשר להתחבר.", ToastLength.Short).Show();
                StartActivity(typeof(LoginActivity));
                Finish();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה: " + ex.Message, ToastLength.Long).Show();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}