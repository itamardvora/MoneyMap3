using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Adapters;
using MoneyMap.Models;
using MoneyMap.Services;
using Xamarin.Essentials;

namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.AdminActivity",
        Label = "אדמין",
        Theme = "@style/AppTheme",
        Exported = false)]
    public class AdminActivity : AppCompatActivity
    {
        private RecyclerView _recyclerView;
        private TextView _emptyText;
        private AdminUserAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_admin);

            _recyclerView = FindViewById<RecyclerView>(Resource.Id.adminUsersRecyclerView);
            _emptyText = FindViewById<TextView>(Resource.Id.adminEmptyText);

            if (!UserSession.IsAdmin)
            {
                Toast.MakeText(this, "אין גישה למסך אדמין", ToastLength.Long).Show();
                Finish();
                return;
            }

            if (!App.IsCoreReady())
                await App.InitForAuthAsync();

            _recyclerView.SetLayoutManager(new LinearLayoutManager(this));

            await LoadUsersAsync();
        }

        protected override async void OnResume()
        {
            base.OnResume();

            if (UserSession.IsAdmin)
                await LoadUsersAsync();
        }

        private async System.Threading.Tasks.Task LoadUsersAsync()
        {
            try
            {
                List<User> users = await App.UserService.GetAllUsersAsync();
                users ??= new List<User>();

                if (_adapter == null)
                {
                    _adapter = new AdminUserAdapter(users, async user => await ConfirmDeleteAsync(user));
                    _recyclerView.SetAdapter(_adapter);
                }
                else
                {
                    _adapter.UpdateData(users);
                }

                _emptyText.Visibility = users.Count == 0 ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
                _recyclerView.Visibility = users.Count == 0 ? Android.Views.ViewStates.Gone : Android.Views.ViewStates.Visible;
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בטעינת משתמשים: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async System.Threading.Tasks.Task ConfirmDeleteAsync(User user)
        {
            if (user == null)
                return;

            new Android.App.AlertDialog.Builder(this)
                .SetTitle("מחיקת משתמש")
                .SetMessage($"האם למחוק את המשתמש {user.FullName}?\nהפעולה תמחק גם את כל הנתונים שלו.")
                .SetNegativeButton("ביטול", (s, e) => { })
                .SetPositiveButton("מחק", async (s, e) =>
                {
                    var result = await App.UserService.DeleteUserCompletelyAsync(user.UserID);
                    if (!result.Success)
                    {
                        Toast.MakeText(this, result.Error, ToastLength.Long).Show();
                        return;
                    }

                    Toast.MakeText(this, "המשתמש נמחק", ToastLength.Short).Show();
                    await LoadUsersAsync();
                })
                .Show();
        }
    }
}