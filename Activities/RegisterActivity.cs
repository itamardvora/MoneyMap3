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

namespace MoneyMap.Activities
{
    [Activity(Label = "הרשמה")]
    public class RegisterActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_register);

            var fullNameInput = FindViewById<EditText>(Resource.Id.fullNameInput);
            var emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            var passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            var birthDateInput = FindViewById<EditText>(Resource.Id.birthDateInput);
            var registerButton = FindViewById<Button>(Resource.Id.registerButton);

            registerButton.Click += async (s, e) =>
            {
                try
                {
                    string fullName = fullNameInput.Text.Trim();
                    string email = emailInput.Text.Trim();
                    string password = passwordInput.Text;
                    DateTime birthDate;

                    if (!DateTime.TryParse(birthDateInput.Text, out birthDate))
                    {
                        Toast.MakeText(this, "תאריך לא תקין", ToastLength.Short).Show();
                        return;
                    }

                    var user = await App.UserService.RegisterUser(fullName, email, password, birthDate);
                    Toast.MakeText(this, "נרשמת בהצלחה!", ToastLength.Long).Show();

                    StartActivity(typeof(LoginActivity));
                    Finish();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }
    }
}