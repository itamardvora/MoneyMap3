using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomNavigation;
using MoneyMap.Adapters;
using MoneyMap.Models;
using MoneyMap.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MoneyMap.Activities
{
    [Activity(Label = "ניהול תקציב")]
    public class BudgetActivity : AppCompatActivity
    {
        private TextView _totalBudgetText, _totalRemainingText;
        private ProgressBar _totalProgressBar;
        private Button _addExpenseButton, _addBudgetButton;
        private RecyclerView _categoryRecyclerView;
        private BudgetAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_budget);

            var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            bottomNav.NavigationItemSelected += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        StartActivity(typeof(HomeActivity));
                        break;
                    case Resource.Id.menu_investments:
                        StartActivity(typeof(InvestmentActivity));
                        break;
                    case Resource.Id.menu_budget:
                        break;
                }
            };

            _totalBudgetText = FindViewById<TextView>(Resource.Id.totalBudgetText);
            _totalRemainingText = FindViewById<TextView>(Resource.Id.totalRemainingText);
            _totalProgressBar = FindViewById<ProgressBar>(Resource.Id.totalProgressBar);
            _addExpenseButton = FindViewById<Button>(Resource.Id.addExpenseButton);
            _addBudgetButton = FindViewById<Button>(Resource.Id.addBudgetButton);
            _categoryRecyclerView = FindViewById<RecyclerView>(Resource.Id.categoryRecyclerView);

            _categoryRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new BudgetAdapter(new List<BudgetStatus>());
            _categoryRecyclerView.SetAdapter(_adapter);

            _addExpenseButton.Click += (s, e) => ShowAddExpenseDialog();
            _addBudgetButton.Click += (s, e) => ShowAddBudgetDialog();

            await LoadBudgetSummary();
        }

        private async Task LoadBudgetSummary()
        {
            int userId = UserSession.LoggedInUserId ?? 0;
            var summaries = await App.BudgetService.GetUserBudgetStatus(userId, DateTime.Now);
            _adapter.UpdateData(summaries);

            decimal totalBudget = 0, totalUsed = 0;
            foreach (var item in summaries)
            {
                totalBudget += item.MonthlyLimit;
                totalUsed += item.Spent;
            }

            decimal remaining = totalBudget - totalUsed;
            int progress = totalBudget > 0 ? (int)(totalUsed / totalBudget * 100) : 0;

            _totalBudgetText.Text = $"תקציב כולל: {totalBudget:n0} ₪";
            _totalRemainingText.Text = $"יתרה כוללת: {remaining:n0} ₪";
            _totalProgressBar.Progress = progress;
        }

        private async void ShowAddBudgetDialog()
        {
            var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_add_budget, null);
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle("הוסף תקציב")
                   .SetView(dialogView)
                   .SetNegativeButton("ביטול", (s2, e2) => { });

            var monthInput = dialogView.FindViewById<EditText>(Resource.Id.budgetMonthInput);
            var amountInput = dialogView.FindViewById<EditText>(Resource.Id.budgetAmountInput);
            var categorySpinner = dialogView.FindViewById<Spinner>(Resource.Id.budgetCategorySpinner);

            var userId = UserSession.LoggedInUserId ?? 0;
            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var categoryNames = categories.Select(c => c.CategoryName).ToList();
            categoryNames.Add("➕ קטגוריה חדשה");

            categorySpinner.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, categoryNames);

            var dialog = builder.Create();
            dialog.Show();

            categorySpinner.ItemSelected += (s, e) =>
            {
                if (e.Position == categories.Count)
                {
                    var input = new EditText(this) { Hint = "הכנס שם קטגוריה חדשה" };
                    new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                        .SetTitle("קטגוריה חדשה")
                        .SetView(input)
                        .SetPositiveButton("צור", async (dlg, evt) =>
                        {
                            var newName = input.Text?.Trim();
                            if (!string.IsNullOrEmpty(newName))
                            {
                                await App.CategoryService.AddCategory(newName, userId);
                                dialog.Dismiss();
                                ShowAddBudgetDialog();
                            }
                            else
                            {
                                Toast.MakeText(this, "יש להזין שם תקין", ToastLength.Short).Show();
                            }
                        })
                        .SetNegativeButton("ביטול", (dlg, evt) => { })
                        .Show();
                }
            };

            var confirmBtn = dialogView.FindViewById<Button>(Resource.Id.confirmAddBudgetButton);
            confirmBtn.Click += async (s, e) =>
            {
                try
                {
                    var selectedIndex = categorySpinner.SelectedItemPosition;
                    if (selectedIndex >= categories.Count) return;

                    var selectedCategory = categories[selectedIndex];
                    var budget = new Budget
                    {
                        UserID = userId,
                        CategoryID = selectedCategory.CategoryID,
                        BudgetMonth = DateTime.Parse(monthInput.Text),
                        MonthlyLimit = decimal.Parse(amountInput.Text)
                    };

                    await App.BudgetService.AddUserBudget(
                        budget.UserID,
                        budget.CategoryID,
                        budget.MonthlyLimit,
                        budget.BudgetMonth
                    );

                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }

        private async void ShowAddExpenseDialog()
        {
            var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_add_expense, null);
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle("הוסף הוצאה")
                   .SetView(dialogView)
                   .SetNegativeButton("ביטול", (s2, e2) => { });

            var dateInput = dialogView.FindViewById<EditText>(Resource.Id.expenseDateInput);
            dateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            dateInput.Focusable = false;
            dateInput.Click += (s2, e2) =>
            {
                var today = DateTime.Today;
                var dialog = new DatePickerDialog(this, (sender, e3) =>
                {
                    dateInput.Text = e3.Date.ToString("yyyy-MM-dd");
                }, today.Year, today.Month - 1, today.Day);
                dialog.Show();
            };

            var reasonInput = dialogView.FindViewById<EditText>(Resource.Id.expenseReasonInput);
            var amountInput = dialogView.FindViewById<EditText>(Resource.Id.expenseAmountInput);
            var categorySpinner = dialogView.FindViewById<Spinner>(Resource.Id.expenseCategorySpinner);

            var userId = UserSession.LoggedInUserId ?? 0;
            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var categoryNames = categories.Select(c => c.CategoryName).ToList();
            categorySpinner.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, categoryNames);

            var dialog = builder.Create();
            dialog.Show();

            var confirmBtn = dialogView.FindViewById<Button>(Resource.Id.confirmAddExpenseButton);
            confirmBtn.Click += async (s2, e2) =>
            {
                try
                {
                    var selectedIndex = categorySpinner.SelectedItemPosition;
                    var selectedCategory = categories[selectedIndex];

                    var expense = new Expense
                    {
                        UserID = userId,
                        Date = DateTime.Parse(dateInput.Text),
                        Reason = reasonInput.Text,
                        Amount = decimal.Parse(amountInput.Text),
                        CategoryID = selectedCategory.CategoryID
                    };

                    await App.ExpenseService.AddExpense(
                        expense.UserID,
                        expense.CategoryID,
                        expense.Amount,
                        expense.Reason,
                        expense.Date
                    );

                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }
    }
}
