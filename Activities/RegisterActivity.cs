using System;
using Android.App;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;
using MoneyMap.Models;
using MoneyMap.Services;

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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_register);

            _fullNameInput = FindViewById<EditText>(Resource.Id.fullNameInput);
            _emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            _passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            _birthDateInput = FindViewById<EditText>(Resource.Id.birthDateInput);
            _registerButton = FindViewById<Button>(Resource.Id.registerButton);

            if (_fullNameInput == null ||
                _emailInput == null ||
                _passwordInput == null ||
                _birthDateInput == null ||
                _registerButton == null)
            {
                Toast.MakeText(this, "שגיאה במסך הרשמה: חסר רכיב XML", ToastLength.Long).Show();
                return;
            }

            _birthDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            _birthDateInput.Focusable = false;

            _birthDateInput.Click += (s, e) =>
            {
                var today = DateTime.Today;

                var datePicker = new DatePickerDialog(
                    this,
                    (sender, args) =>
                    {
                        _birthDateInput.Text = args.Date.ToString("yyyy-MM-dd");
                    },
                    today.Year,
                    today.Month - 1,
                    today.Day
                );

                datePicker.Show();
            };

            _registerButton.Click += async (s, e) =>
            {
                await OnRegisterAsync();
            };
        }

        private async System.Threading.Tasks.Task OnRegisterAsync()
        {
            try
            {
                _registerButton.Enabled = false;
                _registerButton.Text = "נרשם...";

                var fullName = (_fullNameInput?.Text ?? "").Trim();
                var email = (_emailInput?.Text ?? "").Trim().ToLowerInvariant();
                var password = _passwordInput?.Text ?? "";

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    Toast.MakeText(this, "אנא מלא שם מלא", ToastLength.Short).Show();
                    return;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    Toast.MakeText(this, "אנא מלא אימייל", ToastLength.Short).Show();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    Toast.MakeText(this, "אנא מלא סיסמה", ToastLength.Short).Show();
                    return;
                }

                if (password.Length < 6)
                {
                    Toast.MakeText(this, "הסיסמה חייבת להכיל לפחות 6 תווים", ToastLength.Short).Show();
                    return;
                }

                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    Toast.MakeText(this, "אין חיבור לאינטרנט", ToastLength.Long).Show();
                    return;
                }

                if (!DateTime.TryParse(_birthDateInput?.Text, out var birthDate))
                {
                    Toast.MakeText(this, "תאריך לידה לא תקין", ToastLength.Short).Show();
                    return;
                }

                if (!App.IsCoreReady())
                    await App.InitForAuthAsync();

                FireBaseHelper.InitializeFirebase(this);

                var user = new User
                {
                    FullName = fullName,
                    Email = email,
                    Password = password,
                    BirthDate = birthDate,
                    CreatedAt = DateTime.Now,
                    Role = "User"
                };

                await FireBaseHelper.RegisterUserAsync(user);

                Toast.MakeText(this, "נרשמת בהצלחה! אפשר להתחבר.", ToastLength.Short).Show();

                StartActivity(typeof(LoginActivity));
                Finish();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("REGISTER_ERROR", ex.ToString());

                Toast.MakeText(
                    this,
                    "שגיאה בהרשמה: " + ex.GetType().Name + " - " + ex.Message,
                    ToastLength.Long
                ).Show();
            }
            finally
            {
                if (!IsFinishing && _registerButton != null)
                {
                    _registerButton.Enabled = true;
                    _registerButton.Text = "הרשמה";
                }
            }
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}