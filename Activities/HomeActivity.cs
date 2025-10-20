using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using MoneyMap.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyMap.Activities
{
    [Activity(Label = "מסך בית")]
    public class HomeActivity : Activity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_home);
            //

            var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            bottomNav.NavigationItemSelected += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        // כבר בדף הבית – לא עושים כלום
                        break;

                    case Resource.Id.menu_investments:
                        StartActivity(typeof(InvestmentActivity));
                        break;

                    case Resource.Id.menu_budget:
                        StartActivity(typeof(BudgetActivity));
                        break;
                }
            };

            //
            if (!UserSession.IsLoggedIn)
            {
                Toast.MakeText(this, "שגיאה: אין משתמש מחובר.", ToastLength.Long).Show();
                Finish();
                return;
            }

            int userId = UserSession.LoggedInUserId.Value;

            // 1. טקסט ברוך הבא
            var welcomeText = FindViewById<TextView>(Resource.Id.welcomeText);
            string userName = UserSession.LoggedInUserName ?? "משתמש";
            welcomeText.Text = $"שלום, {userName}";

            // 2. כפתור מעבר להשקעות
            //var investmentButton = FindViewById<Button>(Resource.Id.openInvestmentButton);
            //investmentButton.Click += (sender, e) =>
            //{
            //    StartActivity(typeof(MoneyMap.Activities.InvestmentActivity));
            //};

            // 3. קריאה לטעינת סיכומים
            try
            {
                await LoadBudgetSummary(userId);
                await LoadInvestmentSummary(userId);
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"שגיאה: {ex.Message}", ToastLength.Long).Show();
                Console.WriteLine(ex);
            }
        }

        private async Task LoadBudgetSummary(int userId)
        {
            var totalBudgetText = FindViewById<TextView>(Resource.Id.totalBudgetHomeText);
            var remainingBudgetText = FindViewById<TextView>(Resource.Id.remainingBudgetHomeText);
            var progressBar = FindViewById<ProgressBar>(Resource.Id.budgetProgressBar);

            try
            {
                var statuses = await App.BudgetService.GetUserBudgetStatus(userId, DateTime.Now);

                decimal total = 0, spent = 0;

                foreach (var status in statuses)
                {
                    total += status.MonthlyLimit;
                    spent += status.Spent;
                }

                decimal remaining = total - spent;
                int progress = total > 0 ? (int)(spent / total * 100) : 0;

                totalBudgetText.Text = $"תקציב כולל: {total:n0} ₪";
                remainingBudgetText.Text = $"יתרה: {remaining:n0} ₪";
                progressBar.Progress = progress;

                // שינוי צבע לפי מצב
                if (progress < 70)
                    progressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#4CAF50")); // ירוק
                else if (progress < 90)
                    progressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#FFC107")); // כתום
                else
                    progressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#F44336")); // אדום
            }
            catch (Exception ex)
            {
                totalBudgetText.Text = "שגיאה בטעינה";
                remainingBudgetText.Text = "";
                Android.Util.Log.Error("MoneyMap", "שגיאה ב-LoadBudgetSummary: " + ex.ToString());
            }
        }


        private async Task LoadInvestmentSummary(int userId)
        {
            try
            {
                string alphaVantageApiKey = "M97MHPDROY3XVBF1";
                var stockService = new StockPriceService(App.StockPrices, alphaVantageApiKey);
                var portfolioService = new PortfolioService(App.Investments, stockService);

                var totalValue = await portfolioService.GetTotalPortfolioValue(userId);
                var totalProfit = await portfolioService.GetTotalPortfolioReturn(userId);

                var textView = FindViewById<TextView>(Resource.Id.investmentSummaryText);
                textView.Text = $"שווי תיק: {totalValue:N0} ₪\nרווח/הפסד: {totalProfit:N0} ₪";
            }
            catch (Exception ex)
            {
                var textView = FindViewById<TextView>(Resource.Id.investmentSummaryText);
                textView.Text = "אין נתוני השקעות להצגה.";
                System.Diagnostics.Debug.WriteLine("שגיאה בהצגת השקעות: " + ex.Message);
            }
        }
    }
}
