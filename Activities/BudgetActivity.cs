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
        private TextView _totalBudgetText, _totalRemainingText;
        private ProgressBar _totalProgressBar;
        private Button _addExpenseButton, _addBudgetButton;
        private RecyclerView _categoryRecyclerView;
        private FormattedBudgetAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_budget);

            WireBottomNavigation();
            await EnsureUserSessionAsync();

            _totalBudgetText = FindViewById<TextView>(Resource.Id.totalBudgetText);
            _totalRemainingText = FindViewById<TextView>(Resource.Id.totalRemainingText);
            _totalProgressBar = FindViewById<ProgressBar>(Resource.Id.totalProgressBar);
            _addExpenseButton = FindViewById<Button>(Resource.Id.addExpenseButton);
            _addBudgetButton = FindViewById<Button>(Resource.Id.addBudgetButton);
            _categoryRecyclerView = FindViewById<RecyclerView>(Resource.Id.categoryRecyclerView);

            _categoryRecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new FormattedBudgetAdapter(new List<FormattedBudgetRow>(), this);
            _categoryRecyclerView.SetAdapter(_adapter);

            _addExpenseButton.Click += (s, e) => ShowAddExpenseDialog();
            _addBudgetButton.Click += (s, e) => ShowAddBudgetDialog();

            await LoadBudgetSummary();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await LoadBudgetSummary();
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
                        // כבר כאן
                        break;
                }
            };
        }

        private async Task EnsureUserSessionAsync()
        {
            // שחזור משתמש אם צריך
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

            // אתחול מלא אחרי התחברות (במקום InitAsync הישן)
            if (!App.IsFullyReady())
                await App.InitAfterLoginAsync();
        }

        private async Task LoadBudgetSummary()
        {
            int userId = UserSession.LoggedInUserId ?? 0;
            var summaries = await App.BudgetService.GetUserBudgetStatus(userId, DateTime.Now);

            decimal totalBudget = summaries.Sum(s => s.MonthlyLimit);
            decimal totalUsed = summaries.Sum(s => s.Spent);
            decimal remaining = totalBudget - totalUsed;

            _totalBudgetText.Text = "תקציב כולל: " + await SafeFormatAsync(totalBudget, 2);
            _totalRemainingText.Text = "יתרה כוללת: " + await SafeFormatAsync(remaining, 2);
            _totalProgressBar.Progress = totalBudget > 0 ? (int)Math.Min(100, (double)(totalUsed / totalBudget * 100m)) : 0;

            var preferred = await SafePreferredCodeAsync();

            var rows = new List<FormattedBudgetRow>();
            foreach (var s in summaries)
            {
                rows.Add(new FormattedBudgetRow
                {
                    CategoryID = s.CategoryID,
                    CategoryName = s.CategoryName,
                    PlannedText = $"מתוכנן: {await SafeFormatFromIlsAsync(s.MonthlyLimit, preferred, 2)}",
                    SpentText = $"הוצאה: {await SafeFormatFromIlsAsync(s.Spent, preferred, 2)}",
                    RemainingText = $"נותר: {await SafeFormatFromIlsAsync(s.Remaining, preferred, 2)}",
                    Progress = s.MonthlyLimit > 0 ? (int)Math.Min(100, (double)(s.Spent / s.MonthlyLimit * 100m)) : 0
                });
            }
            _adapter.UpdateData(rows);
        }

        // ===== דיאלוג: הוספת תקציב =====
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

            // ברירת מחדל: היום הראשון של החודש הנוכחי
            monthInput.Text = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-01");
            monthInput.Focusable = false;
            monthInput.Click += (s, e) =>
            {
                var t = DateTime.Today;
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

            categorySpinner.Adapter = new ArrayAdapter<string>(
                this, Android.Resource.Layout.SimpleSpinnerItem, names);

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
                                ShowAddBudgetDialog(); // פותח מחדש עם הרשימה המעודכנת
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
                        Toast.MakeText(this, "תאריך/סכום לא תקינים", ToastLength.Short).Show();
                        return;
                    }

                    await App.BudgetService.AddUserBudget(userId, categories[idx].CategoryID, monthlyLimit, month);
                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }

        // ===== דיאלוג: הוספת הוצאה =====
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

            dateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            dateInput.Focusable = false;
            dateInput.Click += (s, e) =>
            {
                var t = DateTime.Today;
                var dp = new DatePickerDialog(this, (sender, ev) =>
                {
                    dateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                }, t.Year, t.Month - 1, t.Day);
                dp.Show();
            };

            int userId = UserSession.LoggedInUserId ?? 0;
            var categories = await App.CategoryService.GetCategoriesForUser(userId);
            var names = categories.Select(c => c.CategoryName).ToList();
            categorySpinner.Adapter = new ArrayAdapter<string>(
                this, Android.Resource.Layout.SimpleSpinnerItem, names);

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
                    Toast.MakeText(this, "שגיאה בצילום/בחירה: " + ex.Message, ToastLength.Long).Show();
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

                    // כאן ההמרה: המשתמש הקליד במטבע שמופיע לו באפליקציה (ILS / USD / EUR),
                    // ואנחנו שומרים למסד תמיד ב-ILS.
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
                            // במקרה כישלון רשת – נשאר עם הערך כמו שהוא (לא נתקע את המשתמש)
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

                    await LoadBudgetSummary();
                    dialog.Dismiss();
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                }
            };
        }

        // ========= עוזרי פורמט בטוחים =========
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

        // ===== ניווט להוצאות של קטגוריה =====
        public void OnViewExpenses(int categoryId, string categoryName)
        {
            var intent = new Android.Content.Intent(this, typeof(CategoryExpensesActivity));
            intent.PutExtra(CategoryExpensesActivity.ExtraCategoryId, categoryId);

            var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            intent.PutExtra(CategoryExpensesActivity.ExtraMonthIso, month.ToString("yyyy-MM-01"));

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
