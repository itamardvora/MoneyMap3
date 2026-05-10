using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using MoneyMap.Services;
using Xamarin.Essentials;

namespace MoneyMap.Activities
{
    [Activity(Label = "בית", Theme = "@style/AppTheme", Exported = false)]
    public class HomeActivity : AppCompatActivity
    {
        private bool _isLoadingHome = false;

        private TextView _helloUser;
        private TextView _budgetTotal, _budgetRemaining;
        private TextView _portfolioValue, _portfolioPnL;
        private TextView _budgetTableEmptyText;
        private LinearLayout _budgetRowsContainer;
        private Button _addExpenseButton;
        private Button _profileButton;


        protected override void OnStop()
        {
            base.OnStop();
            _ = App.TryBackupPendingChangesAsync();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_home);

            _helloUser = FindViewById<TextView>(Resource.Id.helloUserText);
            _budgetTotal = FindViewById<TextView>(Resource.Id.homeBudgetTotalText);
            _budgetRemaining = FindViewById<TextView>(Resource.Id.homeBudgetRemainingText);

            _portfolioValue = FindViewById<TextView>(Resource.Id.homePortfolioValueText);
            _portfolioPnL = FindViewById<TextView>(Resource.Id.homePortfolioPnlText);

            _budgetTableEmptyText = FindViewById<TextView>(Resource.Id.homeBudgetTableEmptyText);
            _budgetRowsContainer = FindViewById<LinearLayout>(Resource.Id.homeBudgetRowsContainer);

            _addExpenseButton = FindViewById<Button>(Resource.Id.homeAddExpenseButton);
            _profileButton = FindViewById<Button>(Resource.Id.profileButton);

            WireBottomNavigation();

            if (_addExpenseButton != null)
                _addExpenseButton.Click += (s, e) => ShowAddExpenseDialog();

            if (_profileButton != null)
                _profileButton.Click += (s, e) => StartActivity(typeof(ProfileActivity));

        }

        protected override async void OnResume()
        {
            base.OnResume();

            if (_isLoadingHome)
                return;

            try
            {
                _isLoadingHome = true;

                if (!App.IsFullyReady())
                    await App.InitAfterLoginAsync();

                await LoadAll();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בטעינת מסך הבית: " + ex.Message, ToastLength.Long).Show();
            }
            finally
            {
                _isLoadingHome = false;
            }
        }

        private void WireBottomNavigation()
        {
            var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            if (bottomNav == null) return;

            bottomNav.SelectedItemId = Resource.Id.menu_home;

            bottomNav.NavigationItemSelected += (s, e) =>
            {
                e.Handled = true;

                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        break;

                    case Resource.Id.menu_investments:
                        StartActivity(typeof(InvestmentActivity));
                        OverridePendingTransition(Resource.Animation.slide_in_right, Resource.Animation.slide_out_left);
                        break;

                    case Resource.Id.menu_budget:
                        StartActivity(typeof(BudgetActivity));
                        OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
                        break;
                }
            };
        }

       


        private async Task LoadAll()
        {
            if (_helloUser != null)
                _helloUser.Text = $"שלום, {UserSession.LoggedInUserName ?? "משתמש"}";

            await LoadBudgetSummary();
            await LoadInvestmentSummary();
        }




        private async Task<string> SafeFormatAsync(decimal amountIls, int decimals = 2)
        {
            try
            {
                if (App.CurrencyService != null)
                    return await App.CurrencyService.FormatAsync(amountIls, "ILS", "ILS", decimals);
            }
            catch
            {
            }

            return amountIls.ToString("N" + decimals) + " ₪";
        }

        private bool TryParsePositiveDecimal(string text, out decimal value)
        {
            text = (text ?? "").Trim().Replace(",", ".");

            return decimal.TryParse(
                text,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out value
            ) && value > 0m;
        }


        private async Task LoadBudgetSummary()
        {
            try
            {
                if (_budgetTotal == null || _budgetRemaining == null)
                    return;

                if (App.BudgetService == null)
                {
                    _budgetTotal.Text = "סה״כ: 0 ₪";
                    _budgetRemaining.Text = "יתרה: 0 ₪";
                    ClearBudgetRows("אין עדיין תקציבים לחודש הזה");
                    return;
                }

                int userId = UserSession.LoggedInUserId ?? 0;
                var month = DateTime.Now;

                var summaries = await App.BudgetService.GetUserBudgetStatus(userId, month);

                if (summaries == null || !summaries.Any())
                {
                    _budgetTotal.Text = "סה״כ: 0 ₪";
                    _budgetRemaining.Text = "יתרה: 0 ₪";
                    ClearBudgetRows("אין עדיין תקציבים לחודש הזה");
                    return;
                }

                decimal total = summaries.Sum(s => s.MonthlyLimit);
                decimal spent = summaries.Sum(s => s.Spent);
                decimal remaining = total - spent;

                _budgetTotal.Text = "סה״כ: " + await SafeFormatAsync(total);
                _budgetRemaining.Text = "יתרה: " + await SafeFormatAsync(remaining);

                if (_budgetRowsContainer != null)
                    _budgetRowsContainer.RemoveAllViews();

                if (_budgetTableEmptyText != null)
                    _budgetTableEmptyText.Visibility = ViewStates.Gone;

                foreach (var item in summaries)
                {
                    string categoryName = item.CategoryName ?? "קטגוריה";
                    decimal monthlyLimit = item.MonthlyLimit;
                    decimal categorySpent = item.Spent;
                    decimal categoryRemaining = monthlyLimit - categorySpent;

                    var row = new LinearLayout(this)
                    {
                        Orientation = Orientation.Horizontal
                    };

                    row.SetPadding(0, 10, 0, 10);

                    var categoryText = new TextView(this)
                    {
                        Text = categoryName,
                        TextSize = 13f
                    };

                    categoryText.SetTextColor(Android.Graphics.Color.ParseColor("#0F172A"));
                    categoryText.LayoutParameters = new LinearLayout.LayoutParams(
                        0,
                        LinearLayout.LayoutParams.WrapContent,
                        1.3f
                    );

                    var budgetText = new TextView(this)
                    {
                        Text = await SafeFormatAsync(monthlyLimit, 0),
                        TextSize = 13f,
                        Gravity = GravityFlags.Center
                    };

                    budgetText.SetTextColor(Android.Graphics.Color.ParseColor("#334155"));
                    budgetText.LayoutParameters = new LinearLayout.LayoutParams(
                        0,
                        LinearLayout.LayoutParams.WrapContent,
                        1f
                    );

                    var spentText = new TextView(this)
                    {
                        Text = await SafeFormatAsync(categorySpent, 0),
                        TextSize = 13f,
                        Gravity = GravityFlags.Center
                    };

                    spentText.SetTextColor(Android.Graphics.Color.ParseColor("#334155"));
                    spentText.LayoutParameters = new LinearLayout.LayoutParams(
                        0,
                        LinearLayout.LayoutParams.WrapContent,
                        1f
                    );

                    var remainingText = new TextView(this)
                    {
                        Text = await SafeFormatAsync(categoryRemaining, 0),
                        TextSize = 13f,
                        Gravity = GravityFlags.Right
                    };

                    if (categoryRemaining < 0)
                        remainingText.SetTextColor(Android.Graphics.Color.ParseColor("#DC2626"));
                    else
                        remainingText.SetTextColor(Android.Graphics.Color.ParseColor("#16A34A"));

                    remainingText.LayoutParameters = new LinearLayout.LayoutParams(
                        0,
                        LinearLayout.LayoutParams.WrapContent,
                        1f
                    );

                    row.AddView(categoryText);
                    row.AddView(budgetText);
                    row.AddView(spentText);
                    row.AddView(remainingText);

                    if (_budgetRowsContainer != null)
                        _budgetRowsContainer.AddView(row);

                    var divider = new View(this);
                    divider.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E2E8F0"));
                    divider.LayoutParameters = new LinearLayout.LayoutParams(
                        LinearLayout.LayoutParams.MatchParent,
                        1
                    );

                    if (_budgetRowsContainer != null)
                        _budgetRowsContainer.AddView(divider);
                }
            }
            catch (Exception ex)
            {
                if (_budgetTotal != null)
                    _budgetTotal.Text = "סה״כ: 0 ₪";

                if (_budgetRemaining != null)
                    _budgetRemaining.Text = "יתרה: 0 ₪";

                ClearBudgetRows("שגיאה בטעינת פירוט התקציב");

                Toast.MakeText(this, "שגיאה בטעינת התקציב: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async Task LoadInvestmentSummary()
        {
            try
            {
                if (_portfolioValue == null || _portfolioPnL == null)
                    return;

                if (App.PortfolioService == null)
                {
                    _portfolioValue.Text = "שווי תיק: 0 ₪";
                    _portfolioPnL.Text = "רווח/הפסד: 0 ₪";
                    return;
                }

                int userId = UserSession.LoggedInUserId ?? 0;

                var summary = await App.PortfolioService.GetPortfolioSummaryAsync(userId);

                _portfolioValue.Text = "שווי תיק: " + await SafeFormatAsync(summary.TotalValueIls);
                _portfolioPnL.Text = "רווח/הפסד: " + await SafeFormatAsync(summary.TotalProfitIls);
            }
            catch (Exception ex)
            {
                if (_portfolioValue != null)
                    _portfolioValue.Text = "שווי תיק: 0 ₪";

                if (_portfolioPnL != null)
                    _portfolioPnL.Text = "רווח/הפסד: 0 ₪";

                Toast.MakeText(this, "שגיאה בטעינת ההשקעות: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private void ClearBudgetRows(string message)
        {
            if (_budgetRowsContainer != null)
                _budgetRowsContainer.RemoveAllViews();

            if (_budgetTableEmptyText != null)
            {
                _budgetTableEmptyText.Text = message;
                _budgetTableEmptyText.Visibility = ViewStates.Visible;
            }
        }

        private async void ShowAddExpenseDialog()
        {
            try
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

                if (dateInput == null || reasonInput == null || amountInput == null ||
                    categorySpinner == null || captureBtn == null || previewImg == null || confirmBtn == null)
                {
                    Toast.MakeText(this, "שגיאה: dialog_add_expense לא תואם לקוד", ToastLength.Long).Show();
                    return;
                }

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

                if (userId <= 0)
                {
                    Toast.MakeText(this, "משתמש לא מחובר", ToastLength.Long).Show();
                    return;
                }

                if (App.BudgetService == null)
                {
                    Toast.MakeText(this, "BudgetService לא מאותחל", ToastLength.Long).Show();
                    return;
                }

                var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var budgetStatuses = await App.BudgetService.GetUserBudgetStatus(userId, currentMonth);

                if (budgetStatuses == null || budgetStatuses.Count == 0)
                {
                    Toast.MakeText(this, "אין קטגוריות עם תקציב בחודש הזה", ToastLength.Long).Show();
                    return;
                }

                var categories = budgetStatuses
                    .Select(s => new MoneyMap.Models.Category
                    {
                        CategoryID = s.CategoryID,
                        CategoryName = s.CategoryName
                    })
                    .ToList();

                var names = categories.Select(c => c.CategoryName).ToList();

                var spinnerAdapter = new ArrayAdapter<string>(
                    this,
                    Android.Resource.Layout.SimpleSpinnerItem,
                    names
                );

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
                                        Title = "receipt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".jpg"
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

                                    var fileName = "receipt_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".jpg";
                                    var full = System.IO.Path.Combine(dir, fileName);

                                    using (var src = await result.OpenReadAsync())
                                    using (var dst = System.IO.File.OpenWrite(full))
                                    {
                                        await src.CopyToAsync(dst);
                                    }

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
                        confirmBtn.Enabled = false;

                        if (categorySpinner.SelectedItemPosition < 0 ||
                            categorySpinner.SelectedItemPosition >= categories.Count)
                        {
                            Toast.MakeText(this, "בחר קטגוריה", ToastLength.Short).Show();
                            return;
                        }

                        if (!TryParsePositiveDecimal(amountInput.Text, out var amountInDisplayCurrency))
                        {
                            Toast.MakeText(this, "סכום לא תקין", ToastLength.Short).Show();
                            return;
                        }

                        if (!DateTime.TryParse(dateInput.Text, out var date))
                        {
                            Toast.MakeText(this, "תאריך לא תקין", ToastLength.Short).Show();
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(reasonInput.Text))
                        {
                            Toast.MakeText(this, "יש להזין סיבה להוצאה", ToastLength.Short).Show();
                            return;
                        }

                        if (App.ExpenseService == null)
                        {
                            Toast.MakeText(this, "ExpenseService לא מאותחל", ToastLength.Long).Show();
                            return;
                        }

                        var selectedCategory = categories[categorySpinner.SelectedItemPosition];
                        var expenseMonth = new DateTime(date.Year, date.Month, 1);
                        var budgetForExpenseMonth = await App.BudgetService.GetUserBudgetStatus(userId, expenseMonth);

                        if (budgetForExpenseMonth == null)
                        {
                            Toast.MakeText(this, "לא נמצאו תקציבים לחודש הזה", ToastLength.Long).Show();
                            return;
                        }

                        bool hasBudgetForSelectedCategory = budgetForExpenseMonth
                            .Any(b => b.CategoryID == selectedCategory.CategoryID);

                        if (!hasBudgetForSelectedCategory)
                        {
                            Toast.MakeText(this, "אי אפשר להוסיף הוצאה לקטגוריה שאין לה תקציב בחודש הזה", ToastLength.Long).Show();
                            return;
                        }

                        decimal amountIls = amountInDisplayCurrency;

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
                    finally
                    {
                        confirmBtn.Enabled = true;
                    }
                };
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בפתיחת הדיאלוג: " + ex.Message, ToastLength.Long).Show();
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