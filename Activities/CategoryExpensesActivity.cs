// Activities/CategoryExpensesActivity.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Models;
using MoneyMap.Services;
using MoneyMap.UI;

namespace MoneyMap.Activities
{
    [Activity(Label = "הוצאות בקטגוריה", Theme = "@style/AppTheme", Exported = false)]
    public class CategoryExpensesActivity : AppCompatActivity
    {
        public const string ExtraCategoryId = "extra_category_id";
        public const string ExtraMonthIso = "extra_month_iso"; // yyyy-MM-01

        private TextView _titleText;
        private TextView _monthText;
        private TextView _emptyText;
        private RecyclerView _recycler;
        private ExpenseListAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_category_expenses);

            _titleText = FindViewById<TextView>(Resource.Id.categoryExpensesTitleText);
            _monthText = FindViewById<TextView>(Resource.Id.categoryExpensesMonthText);
            _emptyText = FindViewById<TextView>(Resource.Id.emptyExpensesText);
            _recycler = FindViewById<RecyclerView>(Resource.Id.rvCategoryExpenses);

            _recycler.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new ExpenseListAdapter(new List<Expense>());
            _recycler.SetAdapter(_adapter);

            int categoryId = Intent.GetIntExtra(ExtraCategoryId, -1);
            var monthIso = Intent.GetStringExtra(ExtraMonthIso) ?? DateTime.Today.ToString("yyyy-MM-01");

            if (!DateTime.TryParse(monthIso, out var month))
                month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            month = new DateTime(month.Year, month.Month, 1);

            int userId = UserSession.LoggedInUserId ?? 0;
            if (userId <= 0 || categoryId <= 0)
            {
                Toast.MakeText(this, "שגיאה בטעינת הנתונים", ToastLength.Short).Show();
                Finish();
                return;
            }

            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var category = categories.FirstOrDefault(c => c.CategoryID == categoryId);
            var categoryName = category?.CategoryName ?? "קטגוריה";

            _titleText.Text = $"הוצאות - {categoryName}";
            _monthText.Text = GetHebrewMonthTitle(month);

            var list = await App.ExpenseService.GetMonthlyExpenses(userId, month);
            list = list
                .Where(x => x.CategoryID == categoryId)
                .OrderByDescending(x => x.Date)
                .ToList();

            _adapter.Update(list);

            bool hasItems = list.Any();
            _emptyText.Visibility = hasItems ? Android.Views.ViewStates.Gone : Android.Views.ViewStates.Visible;
            _recycler.Visibility = hasItems ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
        }

        private string GetHebrewMonthTitle(DateTime date)
        {
            var monthNames = new[]
            {
                "",
                "ינואר",
                "פברואר",
                "מרץ",
                "אפריל",
                "מאי",
                "יוני",
                "יולי",
                "אוגוסט",
                "ספטמבר",
                "אוקטובר",
                "נובמבר",
                "דצמבר"
            };

            return $"{monthNames[date.Month]} {date.Year}";
        }
    }
}