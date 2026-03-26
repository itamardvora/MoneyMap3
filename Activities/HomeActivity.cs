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
        private TextView _helloUser;
        private TextView _budgetTotal, _budgetRemaining;
        private TextView _portfolioValue, _portfolioPnL;
        private Spinner _currencySpinner;
        private Button _addExpenseButton;

        private readonly string[] _currencyOptions = new[] { "ILS", "USD", "EUR" };
        private bool _suppressSpinnerEvent = false;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState); // ✅ חובה ל-MediaPicker
            SetContentView(Resource.Layout.activity_home);

            _helloUser = FindViewById<TextView>(Resource.Id.helloUserText);
            _budgetTotal = FindViewById<TextView>(Resource.Id.homeBudgetTotalText);
            _budgetRemaining = FindViewById<TextView>(Resource.Id.homeBudgetRemainingText);
            _portfolioValue = FindViewById<TextView>(Resource.Id.homePortfolioValueText);
            _portfolioPnL = FindViewById<TextView>(Resource.Id.homePortfolioPnlText);
            _currencySpinner = FindViewById<Spinner>(Resource.Id.currencySpinner);

            // ✅ הכפתור החדש
            _addExpenseButton = FindViewById<Button>(Resource.Id.homeAddExpenseButton);

            WireBottomNavigation();

            if (_addExpenseButton != null)
                _addExpenseButton.Click += (s, e) => ShowAddExpenseDialog();

            if (!App.IsFullyReady())
                await App.InitAfterLoginAsync();

            SetupCurrencySpinner();
            await LoadAll();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            await SyncSpinnerSelectionFromSettingsAsync();
            await LoadAll();
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
                    case Resource.Id.menu_home: break;
                    case Resource.Id.menu_investments:
                        if (!(this is InvestmentActivity)) StartActivity(typeof(InvestmentActivity));
                        break;
                    case Resource.Id.menu_budget:
                        if (!(this is BudgetActivity)) StartActivity(typeof(BudgetActivity));
                        break;
                }
            };
        }

        private void SetupCurrencySpinner()
        {
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, _currencyOptions);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _currencySpinner.Adapter = adapter;

            _suppressSpinnerEvent = true;
            var code = Preferences.Get("PreferredCurrencyCode", "ILS");
            var idx = Array.IndexOf(_currencyOptions, code);
            if (idx < 0) idx = 0;
            _currencySpinner.SetSelection(idx);
            _suppressSpinnerEvent = false;

            _currencySpinner.ItemSelected += async (s, e) =>
            {
                if (_suppressSpinnerEvent) return;

                var selectedCode = _currencyOptions[e.Position];

                if (App.CurrencyService == null)
                {
                    Preferences.Set("PreferredCurrencyCode", selectedCode);
                    await LoadAll();
                    return;
                }

                try
                {
                    await EnsureRatesNowAsync();
                    await App.CurrencyService.SetPreferredCurrencyAsync(selectedCode);
                    Preferences.Set("PreferredCurrencyCode", selectedCode);
                    Toast.MakeText(this, $"מטבע תצוגה: {selectedCode}", ToastLength.Short).Show();
                    await LoadAll();
                }
                catch
                {
                    Preferences.Set("PreferredCurrencyCode", selectedCode);
                    await LoadAll();
                }
            };
        }

        private async Task EnsureRatesNowAsync()
        {
            try
            {
                if (App.CurrencyService != null)
                    await App.CurrencyService.EnsureBaseRatesAsync();
            }
            catch { }
        }

        private async Task SyncSpinnerSelectionFromSettingsAsync()
        {
            var code = await SafePreferredCodeAsync();
            var idx = Array.IndexOf(_currencyOptions, code);
            if (idx < 0) idx = 0;

            _suppressSpinnerEvent = true;
            _currencySpinner.SetSelection(idx);
            _suppressSpinnerEvent = false;
        }

        private async Task LoadAll()
        {
            _helloUser.Text = $"שלום, {UserSession.LoggedInUserName ?? "משתמש"}";
            await LoadBudgetSummary();
            await LoadInvestmentSummary();
        }

        private async Task<string> SafePreferredCodeAsync()
        {
            var code = Preferences.Get("PreferredCurrencyCode", "ILS");
            try
            {
                if (App.CurrencyService != null)
                {
                    var srv = await App.CurrencyService.GetPreferredCurrencyCodeAsync();
                    if (!string.IsNullOrWhiteSpace(srv) && srv != code)
                    {
                        code = srv;
                        Preferences.Set("PreferredCurrencyCode", code);
                    }
                }
            }
            catch { }
            return code;
        }

        private async Task<string> SafeFormatAsync(decimal amountIls, int decimals = 2)
        {
            var code = Preferences.Get("PreferredCurrencyCode", "ILS");
            try
            {
                if (App.CurrencyService != null)
                    return await App.CurrencyService.FormatAsync(amountIls, "ILS", code, decimals);
            }
            catch { }

            return amountIls.ToString("N" + decimals) + " ₪";
        }

        private async Task LoadBudgetSummary()
        {
            int userId = UserSession.LoggedInUserId ?? 0;
            var month = DateTime.Now;

            var summaries = await App.BudgetService.GetUserBudgetStatus(userId, month);
            decimal total = summaries.Sum(s => s.MonthlyLimit);
            decimal spent = summaries.Sum(s => s.Spent);
            decimal remaining = total - spent;

            _budgetTotal.Text = "סה״כ: " + await SafeFormatAsync(total);
            _budgetRemaining.Text = "יתרה: " + await SafeFormatAsync(remaining);
        }

        // ✅ הוספת הוצאה ישירות מהבית
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

                        // המשתמש מקליד במטבע תצוגה — במסד שומרים תמיד ILS
                        decimal amountIls = amountInDisplayCurrency;
                        var preferred = await SafePreferredCodeAsync();

                        if (!string.Equals(preferred, "ILS", StringComparison.OrdinalIgnoreCase) &&
                            App.CurrencyService != null)
                        {
                            try
                            {
                                var rate = await App.CurrencyService.GetRateAsync(preferred, "ILS");
                                if (rate.HasValue && rate.Value > 0m)
                                    amountIls = amountInDisplayCurrency * rate.Value;
                            }
                            catch { }
                        }

                        await App.ExpenseService.AddExpense(
                            userId,
                            selectedCategory.CategoryID,
                            amountIls,
                            reasonInput.Text,
                            date,
                            receiptPath
                        );

                        await LoadBudgetSummary(); // ✅ רענון סיכום התקציב בבית
                        dialog.Dismiss();
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "שגיאה בהוספה: " + ex.Message, ToastLength.Long).Show();
                    }
                };
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בפתיחת הדיאלוג: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async Task LoadInvestmentSummary()
        {
            int userId = UserSession.LoggedInUserId ?? 0;

            var totalValueIls = await App.PortfolioService.GetPortfolioTotalValueAsync(userId);
            _portfolioValue.Text = "שווי תיק: " + await SafeFormatAsync(totalValueIls);

            decimal totalPnlIls = 0m;
            int failedSymbols = 0;

            try
            {
                var list = await App.InvestmentService.GetInvestmentsForUserAsync(userId);
                foreach (var inv in list)
                {
                    try
                    {
                        var current = await App.StockPriceService.GetPriceOrFetch(inv.StockSymbol);
                        var curVal = (decimal)current * inv.Quantity;
                        var cost = inv.BuyPrice * inv.Quantity;
                        totalPnlIls += (curVal - cost);
                    }
                    catch { failedSymbols++; }
                }
            }
            catch { }

            _portfolioPnL.Text = "רווח/הפסד: " + await SafeFormatAsync(totalPnlIls);

            if (failedSymbols > 0)
                Toast.MakeText(this, $"הערה: {failedSymbols} סמלים לא חושבו (API/רשת).", ToastLength.Short).Show();
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
