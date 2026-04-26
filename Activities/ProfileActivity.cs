using System;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using MoneyMap.Models;
using MoneyMap.Services;
using Xamarin.Essentials;

namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.ProfileActivity",
        Label = "פרטים אישיים",
        Theme = "@style/AppTheme",
        Exported = false)]
    public class ProfileActivity : AppCompatActivity
    {
        private EditText _fullNameInput;
        private TextView _emailText;
        private TextView _birthDateText;
        private TextView _createdAtText;
        private Button _saveButton;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_profile);

            _fullNameInput = FindViewById<EditText>(Resource.Id.profileFullNameInput);
            _emailText = FindViewById<TextView>(Resource.Id.profileEmailText);
            _birthDateText = FindViewById<TextView>(Resource.Id.profileBirthDateText);
            _createdAtText = FindViewById<TextView>(Resource.Id.profileCreatedAtText);
            _saveButton = FindViewById<Button>(Resource.Id.profileSaveButton);

            if (!App.IsCoreReady())
                await App.InitForAuthAsync();

            _saveButton.Click += async (s, e) => await SaveAsync();

            await LoadProfileAsync();
        }

        private async System.Threading.Tasks.Task LoadProfileAsync()
        {
            try
            {
                var userId = UserSession.LoggedInUserId ?? 0;
                if (userId <= 0)
                {
                    Toast.MakeText(this, "לא נמצא משתמש מחובר", ToastLength.Long).Show();
                    Finish();
                    return;
                }

                User user = await App.UserService.GetUserProfile(userId);
                if (user == null)
                {
                    Toast.MakeText(this, "המשתמש לא נמצא", ToastLength.Long).Show();
                    Finish();
                    return;
                }

                _fullNameInput.Text = user.FullName ?? string.Empty;
                _emailText.Text = user.Email ?? string.Empty;
                _birthDateText.Text = user.BirthDate.ToString("dd/MM/yyyy");
                _createdAtText.Text = user.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בטעינת פרטים: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            try
            {
                _saveButton.Enabled = false;

                var userId = UserSession.LoggedInUserId ?? 0;
                var fullName = (_fullNameInput?.Text ?? string.Empty).Trim();

                var result = await App.UserService.UpdateFullNameAsync(userId, fullName);
                if (!result.Success)
                {
                    Toast.MakeText(this, result.Error, ToastLength.Long).Show();
                    return;
                }

                Toast.MakeText(this, "השם עודכן בהצלחה", ToastLength.Short).Show();
                Finish();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בשמירה: " + ex.Message, ToastLength.Long).Show();
            }
            finally
            {
                _saveButton.Enabled = true;
            }
        }
    }
}