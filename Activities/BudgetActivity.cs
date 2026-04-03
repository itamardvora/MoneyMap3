using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.BottomNavigation;
using Xamarin.Essentials;
using MoneyMap.Services;
using MoneyMap.Adapters;
using MoneyMap.Models;

namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.BudgetActivity",
        Label = "ניהול תקציב",
        Theme = "@style/AppTheme",
        Exported = false)]
    public class BudgetActivity : AppCompatActivity, IBudgetItemListener
    {
        private TextView _currentMonthText;
        private TextView _totalIncomeText;
        private TextView _totalBudgetText;
        private TextView _totalSpentText;
        private TextView _totalRemainingText;

        private ProgressBar _totalProgressBar;

        private ImageButton _previousMonthButton;
        private ImageButton _nextMonthButton;

        private Button _addIncomeButton;
        private Button _addExpenseButton;
        private Button _addBudgetBottomButton;

        private RecyclerView _categoryRecyclerView;
        private FormattedBudgetAdapter _adapter;

        private RecyclerView _incomeRecyclerView;
        private IncomeAdapter _incomeAdapter;

        private DateTime _selectedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_budget);

            WireBottomNavigation();
            await EnsureUserSessionAsync();

            BindViews();
            WireEvents();

            _categoryRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new FormattedBudgetAdapter(new List<FormattedBudgetRow>(), this);
            _categoryRecyclerView.SetAdapter(_adapter);

            _incomeRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _incomeAdapter = new IncomeAdapter(new List<Income>());
            _incomeRecyclerView.SetAdapter(_incomeAdapter);

            await LoadBudgetSummary();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await LoadBudgetSummary();
        }

        private void BindViews()
        {
            _currentMonthText = FindViewById<TextView>(Resource.Id.currentMonthText);

            _totalIncomeText = FindViewById<TextView>(Resource.Id.totalIncomeText);
            _totalBudgetText = FindViewById<TextView>(Resource.Id.totalBudgetText);
            _totalSpentText = FindViewById<TextView>(Resource.Id.totalSpentText);
            _totalRemainingText = FindViewById<TextView>(Resource.Id.totalRemainingText);

            _totalProgressBar = FindViewById<ProgressBar>(Resource.Id.totalProgressBar);

            _previousMonthButton = FindViewById<ImageButton>(Resource.Id.previousMonthButton);
            _nextMonthButton = FindViewById<ImageButton>(Resource.Id.nextMonthButton);

            _addIncomeButton = FindViewById<Button>(Resource.Id.addIncomeButton);
            _addExpenseButton = FindViewById<Button>(Resource.Id.addExpenseButton);
            _addBudgetBottomButton = FindViewById<Button>(Resource.Id.addBudgetBottomButton);

            _categoryRecyclerView = FindViewById<RecyclerView>(Resource.Id.categoryRecyclerView);
            _incomeRecyclerView = FindViewById<RecyclerView>(Resource.Id.incomeRecyclerView);
        }

        private void WireEvents()
        {
            _previousMonthButton.Click += async (s, e) =>
            {
                _selectedMonth = _selectedMonth.AddMonths(-1);
                await LoadBudgetSummary();
            };

            _nextMonthButton.Click += async (s, e) =>
            {
                _selectedMonth = _selectedMonth.AddMonths(1);
                await LoadBudgetSummary();
            };

            _addIncomeButton.Click += (s, e) => ShowAddIncomeDialog();
            _addExpenseButton.Click += (s, e) => ShowAddExpenseDialog();
            _addBudgetBottomButton.Click += (s, e) => ShowAddBudgetDialog();
        }

        private void WireBottomNavigation()
        {
            var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            if (bottomNav == null) return;

            bottomNav.SelectedItemId = Resource.Id.menu_budget;
            bottomNav.NavigationItemSelected += (s, e) =>
            {
                e.Handled = true;
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        if (!(this is HomeActivity)) StartActivity(typeof(HomeActivity));
                        break;
                    case Resource.Id.menu_investments:
                        if (!(this is InvestmentActivity)) StartActivity(typeof(InvestmentActivity));
                        break;
                    case Resource.Id.menu_budget:
                        break;
                }
            };
        }

        private async Task EnsureUserSessionAsync()
        {
            if (!(UserSession.LoggedInUserId.HasValue && UserSession.LoggedInUserId.Value > 0))
            {
                var savedId = Preferences.Get("LoggedInUserId", 0);
                var savedName = Preferences.Get("LoggedInUserName", "");
                if (savedId > 0)
                {
                    UserSession.LoggedInUserId = savedId;
                    UserSession.LoggedInUserName = savedName;
                }
                else
                {
                    StartActivity(typeof(LoginActivity));
                    Finish();
                    return;
                }
            }

            if (!App.IsFullyReady())
                await App.InitAfterLoginAsync();
        }

        private async Task LoadBudgetSummary()
        {
            int userId = UserSession.LoggedInUserId ?? 0;

            _currentMonthText.Text = GetHebrewMonthTitle(_selectedMonth);

            var summaries = await App.BudgetService.GetUserBudgetStatus(userId, _selectedMonth);
            var expenses = await App.ExpenseService.GetMonthlyExpenses(userId, _selectedMonth);
            var incomes = await App.IncomeService.GetMonthlyIncomes(userId, _selectedMonth);

            decimal totalIncome = incomes.Sum(i => i.Amount);
            decimal totalBudget = summaries.Sum(s => s.MonthlyLimit);
            decimal totalSpent = expenses.Sum(e => e.Amount);
            decimal remaining = totalIncome - totalSpent;

            _totalIncomeText.Text = await SafeFormatAsync(totalIncome, 2);
            _totalBudgetText.Text = await SafeFormatAsync(totalBudget, 2);
            _totalSpentText.Text = await SafeFormatAsync(totalSpent, 2);
            _totalRemainingText.Text = await SafeFormatAsync(remaining, 2);

            _totalProgressBar.Progress = totalBudget > 0m
                ? (int)Math.Min(100, (double)(totalSpent / totalBudget * 100m))
                : 0;

            var preferred = await SafePreferredCodeAsync();

            var rows = new List<FormattedBudgetRow>();
            foreach (var s in summaries.OrderBy(x => x.CategoryName))
            {
                rows.Add(new FormattedBudgetRow
                {
                    CategoryID = s.CategoryID,
                    CategoryName = s.CategoryName,
                    PlannedText = await SafeFormatFromIlsAsync(s.MonthlyLimit, preferred, 0),
                    SpentText = await SafeFormatFromIlsAsync(s.Spent, preferred, 0),
                    RemainingText = await SafeFormatFromIlsAsync(s.Remaining, preferred, 0),
                    Progress = s.MonthlyLimit > 0
                        ? (int)Math.Round((double)(s.Spent / s.MonthlyLimit * 100m))
                        : 0
                });
            }

            _adapter.UpdateData(rows);
            _incomeAdapter.Update(incomes.OrderByDescending(i => i.Date).ToList());
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

        private async void ShowAddIncomeDialog()
        {
            var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_add_income, null);

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("הוסף הכנסה")
                .SetView(dialogView)
                .SetNegativeButton("ביטול", (s, e) => { });

            var dateInput = dialogView.FindViewById<EditText>(Resource.Id.incomeDateInput);
            var sourceInput = dialogView.FindViewById<EditText>(Resource.Id.incomeSourceInput);
            var amountInput = dialogView.FindViewById<EditText>(Resource.Id.incomeAmountInput);
            var confirmBtn = dialogView.FindViewById<Button>(Resource.Id.confirmAddIncomeButton);

            dateInput.Text = _selectedMonth.ToString("yyyy-MM-01");
            dateInput.Focusable = false;

            dateInput.Click += (s, e) =>
            {
                var t = _selectedMonth;
                var dp = new DatePickerDialog(this, (sender, ev) =>
                {
                    dateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                }, t.Year, t.Month - 1, 1);
                dp.Show();
            };

            var dialog = builder.Create();
            dialog.Show();

            confirmBtn.Click += async (s, e) =>
            {
                try
                {
                    int userId = UserSession.LoggedInUserId ?? 0;

                    if (!DateTime.TryParse(dateInput.Text, out var date))
                    {
                        Toast.MakeText(this, "תאריך לא תקין", ToastLength.Short).Show();
                        return;
                    }

                    if (!decimal.TryParse(amountInput.Text, out var amountInDisplayCurrency) || amountInDisplayCurrency <= 0)
                    {
                        Toast.MakeText(this, "סכום לא תקין", ToastLength.Short).Show();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(sourceInput.Text))
                    {
                        Toast.MakeText(this, "יש להזין מקור הכנסה", ToastLength.Short).Show();
                        return;
                    }

                    decimal amountIls = amountInDisplayCurrency;
                    var preferred = await SafePreferredCodeAsync();

                    if (!string.Equals(preferred, "ILS", StringComparison.OrdinalIgnoreCase) &&
                        App.CurrencyService != null)
                    {
                        try
                        {
                            var rate = await App.CurrencyService.GetRateAsync(preferred, "ILS");
                            if (rate.HasValue && rate.Value > 0m)
                            {
                                amountIls = amountInDisplayCurrency * rate.Value;
                            }
                        }
                        catch
                        {
                        }
                    }

                    await App.IncomeService.AddIncome(
                        userId,
                        amountIls,
                        sourceInput.Text,
                        date
                    );

                    _selectedMonth = new DateTime(date.Year, date.Month, 1);
                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }

        private async void ShowAddBudgetDialog()
        {
            var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_add_budget, null);

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("הוסף תקציב")
                .SetView(dialogView)
                .SetNegativeButton("ביטול", (s, e) => { });

            var monthInput = dialogView.FindViewById<EditText>(Resource.Id.budgetMonthInput);
            var amountInput = dialogView.FindViewById<EditText>(Resource.Id.budgetAmountInput);
            var categorySpinner = dialogView.FindViewById<Spinner>(Resource.Id.budgetCategorySpinner);
            var confirmBtn = dialogView.FindViewById<Button>(Resource.Id.confirmAddBudgetButton);

            monthInput.Text = new DateTime(_selectedMonth.Year, _selectedMonth.Month, 1).ToString("yyyy-MM-01");
            monthInput.Focusable = false;

            monthInput.Click += (s, e) =>
            {
                var t = _selectedMonth;
                var dp = new DatePickerDialog(this, (sender, ev) =>
                {
                    var d = new DateTime(ev.Date.Year, ev.Date.Month, 1);
                    monthInput.Text = d.ToString("yyyy-MM-01");
                }, t.Year, t.Month - 1, 1);
                dp.Show();
            };

            int userId = UserSession.LoggedInUserId ?? 0;
            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var names = categories.Select(c => c.CategoryName).ToList();
            names.Add("➕ קטגוריה חדשה");

            var spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, names);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            categorySpinner.Adapter = spinnerAdapter;

            var dialog = builder.Create();
            dialog.Show();

            categorySpinner.ItemSelected += (s, e) =>
            {
                if (e.Position == categories.Count)
                {
                    var input = new EditText(this) { Hint = "שם קטגוריה חדשה" };
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

            confirmBtn.Click += async (s, e) =>
            {
                try
                {
                    var idx = categorySpinner.SelectedItemPosition;
                    if (idx < 0 || idx >= categories.Count)
                    {
                        Toast.MakeText(this, "בחר קטגוריה", ToastLength.Short).Show();
                        return;
                    }

                    var monthOk = DateTime.TryParse(monthInput.Text, out var month);
                    var limitOk = decimal.TryParse(amountInput.Text, out var monthlyLimit);

                    if (!monthOk || !limitOk || monthlyLimit <= 0)
                    {
                        Toast.MakeText(this, "תאריך או סכום לא תקינים", ToastLength.Short).Show();
                        return;
                    }

                    await App.BudgetService.AddUserBudget(userId, categories[idx].CategoryID, monthlyLimit, month);
                    _selectedMonth = new DateTime(month.Year, month.Month, 1);
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

            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("הוסף הוצאה")
                .SetView(dialogView)
                .SetNegativeButton("ביטול", (s, e) => { });

            var dateInput = dialogView.FindViewById<EditText>(Resource.Id.expenseDateInput);
            var reasonInput = dialogView.FindViewById<EditText>(Resource.Id.expenseReasonInput);
            var amountInput = dialogView.FindViewById<EditText>(Resource.Id.expenseAmountInput);
            var categorySpinner = dialogView.FindViewById<Spinner>(Resource.Id.expenseCategorySpinner);
            var captureBtn = dialogView.FindViewById<Button>(Resource.Id.captureReceiptButton);
            var previewImg = dialogView.FindViewById<ImageView>(Resource.Id.receiptPreviewImage);
            var confirmBtn = dialogView.FindViewById<Button>(Resource.Id.confirmAddExpenseButton);

            dateInput.Text = _selectedMonth.ToString("yyyy-MM-01");
            dateInput.Focusable = false;

            dateInput.Click += (s, e) =>
            {
                var t = _selectedMonth;
                var dp = new DatePickerDialog(this, (sender, ev) =>
                {
                    dateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                }, t.Year, t.Month - 1, 1);
                dp.Show();
            };

            int userId = UserSession.LoggedInUserId ?? 0;
            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var names = categories.Select(c => c.CategoryName).ToList();

            var spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, names);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            categorySpinner.Adapter = spinnerAdapter;

            string receiptPath = null;

            captureBtn.Click += async (s, e) =>
            {
                try
                {
                    var options = new[] { "צלם עכשיו", "בחר מהגלריה" };
                    new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                        .SetTitle("צרף קבלה")
                        .SetItems(options, async (sender, args) =>
                        {
                            var useCamera = args.Which == 0;
                            FileResult result = null;

                            if (useCamera)
                            {
                                result = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                                {
                                    Title = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg"
                                });
                            }
                            else
                            {
                                result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                                {
                                    Title = "בחר קבלה"
                                });
                            }

                            if (result != null)
                            {
                                var dir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "receipts");
                                System.IO.Directory.CreateDirectory(dir);
                                var fileName = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg";
                                var full = System.IO.Path.Combine(dir, fileName);

                                using (var src = await result.OpenReadAsync())
                                using (var dst = System.IO.File.OpenWrite(full))
                                    await src.CopyToAsync(dst);

                                receiptPath = full;
                                previewImg.Visibility = ViewStates.Visible;
                                previewImg.SetImageURI(Android.Net.Uri.Parse(full));
                            }
                        })
                        .Show();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בצילום או בחירה: " + ex.Message, ToastLength.Long).Show();
                }
            };

            var dialog = builder.Create();
            dialog.Show();

            confirmBtn.Click += async (s, e) =>
            {
                try
                {
                    if (categorySpinner.SelectedItemPosition < 0)
                    {
                        Toast.MakeText(this, "בחר קטגוריה", ToastLength.Short).Show();
                        return;
                    }

                    if (!decimal.TryParse(amountInput.Text, out var amountInDisplayCurrency) || amountInDisplayCurrency <= 0)
                    {
                        Toast.MakeText(this, "סכום לא תקין", ToastLength.Short).Show();
                        return;
                    }

                    if (!DateTime.TryParse(dateInput.Text, out var date))
                    {
                        Toast.MakeText(this, "תאריך לא תקין", ToastLength.Short).Show();
                        return;
                    }

                    var selectedCategory = categories[categorySpinner.SelectedItemPosition];

                    decimal amountIls = amountInDisplayCurrency;
                    var preferred = await SafePreferredCodeAsync();

                    if (!string.Equals(preferred, "ILS", StringComparison.OrdinalIgnoreCase) && App.CurrencyService != null)
                    {
                        try
                        {
                            var rate = await App.CurrencyService.GetRateAsync(preferred, "ILS");
                            if (rate.HasValue && rate.Value > 0m)
                                amountIls = amountInDisplayCurrency * rate.Value;
                        }
                        catch
                        {
                        }
                    }

                    await App.ExpenseService.AddExpense(
                        userId,
                        selectedCategory.CategoryID,
                        amountIls,
                        reasonInput.Text,
                        date,
                        receiptPath
                    );

                    _selectedMonth = new DateTime(date.Year, date.Month, 1);
                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }

        private async Task<string> SafePreferredCodeAsync()
        {
            try
            {
                if (App.CurrencyService != null)
                {
                    var code = await App.CurrencyService.GetPreferredCurrencyCodeAsync();
                    if (!string.IsNullOrWhiteSpace(code)) return code;
                }
            }
            catch { }

            return "ILS";
        }

        private async Task<string> SafeFormatAsync(decimal amountIls, int decimals = 2)
        {
            try
            {
                var code = await SafePreferredCodeAsync();
                if (App.CurrencyService != null)
                    return await App.CurrencyService.FormatAsync(amountIls, "ILS", code, decimals);
            }
            catch { }

            return amountIls.ToString("N" + decimals) + " ₪";
        }

        private async Task<string> SafeFormatFromIlsAsync(decimal amountIls, string target, int decimals = 2)
        {
            try
            {
                if (App.CurrencyService != null && !string.IsNullOrWhiteSpace(target))
                    return await App.CurrencyService.FormatAsync(amountIls, "ILS", target, decimals);
            }
            catch { }

            return amountIls.ToString("N" + decimals) + " ₪";
        }

        public void OnViewExpenses(int categoryId, string categoryName)
        {
            var intent = new Android.Content.Intent(this, typeof(CategoryExpensesActivity));
            intent.PutExtra(CategoryExpensesActivity.ExtraCategoryId, categoryId);
            intent.PutExtra(CategoryExpensesActivity.ExtraMonthIso, _selectedMonth.ToString("yyyy-MM-01"));
            StartActivity(intent);
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