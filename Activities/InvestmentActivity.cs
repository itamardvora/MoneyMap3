using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MoneyMap.Adapters;
using MoneyMap.Models;
using MoneyMap.Services;
using Xamarin.Essentials;
using Android.Views;
using Google.Android.Material.BottomNavigation;
using Android.Views.InputMethods;

namespace MoneyMap.Activities
{
    [Activity(Label = "השקעות", Theme = "@style/AppTheme", Exported = false)]
    public class InvestmentActivity : AppCompatActivity
    {
        protected override void OnStop()
        {
            base.OnStop();
            _ = App.TryBackupPendingChangesAsync();
        }

        private RecyclerView _recycler;
        private InvestmentAdapter _adapter;
        private TextView _portfolioValueText;
        private LinearLayout _addFormLayout;
        private Button _addInvestmentButton;
        private Button _saveInvestmentButton;

        private EditText _symbolInput;
        private EditText _quantityInput;
        private EditText _buyPriceInput;
        private EditText _buyDateInput;
        private Spinner _currencySpinner;

        private BottomNavigationView _bottomNavigation;

        private int _userId;
        private List<Investment> _investments = new List<Investment>();
        private List<InvestmentDisplayRow> _rows = new List<InvestmentDisplayRow>();

        private bool _isSaving = false;
        private bool _isLoadingData = false;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_investment);

            if (!App.IsFullyReady())
                await App.InitAfterLoginAsync();

            _userId = UserSession.LoggedInUserId ?? Preferences.Get("LoggedInUserId", 0);

            if (_userId <= 0)
            {
                Toast.MakeText(this, "לא נמצא משתמש מחובר", ToastLength.Long).Show();
                Finish();
                return;
            }

            BindViews();

            if (!ValidateViews())
                return;

            _recycler.SetLayoutManager(new LinearLayoutManager(this));

            SetupBottomNavigation();
            SetupBuyDatePicker();
            SetupCurrencySpinnerForPurchase();
            SetupAddFormButtons();

            await LoadDataAsync();
        }

        protected override async void OnResume()
        {
            base.OnResume();

            if (_userId > 0)
                await LoadDataAsync();
        }

        private void BindViews()
        {
            _portfolioValueText = FindViewById<TextView>(Resource.Id.portfolioValueText);
            _recycler = FindViewById<RecyclerView>(Resource.Id.investmentRecyclerView);
            _addFormLayout = FindViewById<LinearLayout>(Resource.Id.addFormLayout);
            _addInvestmentButton = FindViewById<Button>(Resource.Id.addInvestmentButton);
            _saveInvestmentButton = FindViewById<Button>(Resource.Id.saveInvestmentButton);

            _symbolInput = FindViewById<EditText>(Resource.Id.symbolInput);
            _quantityInput = FindViewById<EditText>(Resource.Id.quantityInput);
            _buyPriceInput = FindViewById<EditText>(Resource.Id.buyPriceInput);
            _buyDateInput = FindViewById<EditText>(Resource.Id.buyDateInput);
            _currencySpinner = FindViewById<Spinner>(Resource.Id.investmentCurrencySpinner);

            _bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
        }

        private bool ValidateViews()
        {
            if (_recycler == null ||
                _portfolioValueText == null ||
                _addFormLayout == null ||
                _addInvestmentButton == null ||
                _symbolInput == null ||
                _quantityInput == null ||
                _buyPriceInput == null ||
                _buyDateInput == null ||
                _currencySpinner == null)
            {
                Toast.MakeText(this, "שגיאה: activity_investment לא תואם לקוד", ToastLength.Long).Show();
                return false;
            }

            return true;
        }

        private void SetupBottomNavigation()
        {
            if (_bottomNavigation == null)
                return;

            _bottomNavigation.SelectedItemId = Resource.Id.menu_investments;

            _bottomNavigation.NavigationItemSelected += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        StartActivity(typeof(HomeActivity));
                        OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
                        Finish();
                        break;

                    case Resource.Id.menu_investments:
                        break;

                    case Resource.Id.menu_budget:
                        StartActivity(typeof(BudgetActivity));
                        OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
                        Finish();
                        break;
                }
            };
        }

        private void SetupBuyDatePicker()
        {
            _buyDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            _buyDateInput.Focusable = false;

            _buyDateInput.Click += (s, e) =>
            {
                var today = DateTime.Today;

                var dp = new DatePickerDialog(
                    this,
                    (sender, ev) =>
                    {
                        _buyDateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                    },
                    today.Year,
                    today.Month - 1,
                    today.Day
                );

                dp.Show();
            };
        }

        private void SetupCurrencySpinnerForPurchase()
        {
            var options = new[] { "ILS", "USD", "EUR" };

            var adapter = new ArrayAdapter<string>(
                this,
                Android.Resource.Layout.SimpleSpinnerItem,
                options
            );

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            _currencySpinner.Adapter = adapter;
            _currencySpinner.SetSelection(0);
        }

        private void SetupAddFormButtons()
        {
            _addInvestmentButton.Click += async (s, e) =>
            {
                if (_addFormLayout.Visibility != ViewStates.Visible)
                {
                    _addFormLayout.Visibility = ViewStates.Visible;
                    _addInvestmentButton.Text = "שמור השקעה";
                    return;
                }

                await SaveInvestmentAsync();
            };

            if (_saveInvestmentButton != null)
            {
                _saveInvestmentButton.Click += async (s, e) =>
                {
                    await SaveInvestmentAsync();
                };
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                if (App.InvestmentService == null)
                {
                    Toast.MakeText(this, "שירות השקעות לא זמין כרגע", ToastLength.Short).Show();
                    return;
                }

                int userId = _userId;

                // 1. קודם שולפים את ההשקעות מה-DB כדי שהרשימה תמיד תופיע
                var investmentsFromDb = await App.InvestmentService.GetInvestmentsForUserAsync(userId)
                                        ?? new List<Investment>();

                _investments = investmentsFromDb;

                // 2. שווי תיק למעלה - בדיוק בסגנון של HomeActivity
                if (App.PortfolioService != null)
                {
                    try
                    {
                        var summary = await App.PortfolioService.GetPortfolioSummaryAsync(userId);

                        if (summary != null && summary.TotalValueIls > 0m)
                        {
                            _portfolioValueText.Text =
                                "שווי תיק: " + summary.TotalValueIls.ToString("N2") + " ₪";
                        }
                        else
                        {
                            decimal fallbackCost = 0m;

                            foreach (var inv in investmentsFromDb)
                            {
                                decimal fx = inv.FxRateToIlsAtPurchase > 0m
                                    ? inv.FxRateToIlsAtPurchase
                                    : 1m;

                                decimal costIls = inv.TotalInIls > 0m
                                    ? inv.TotalInIls
                                    : inv.Quantity * inv.BuyPrice * fx;

                                fallbackCost += costIls;
                            }

                            _portfolioValueText.Text =
                                "עלות תיק: " + fallbackCost.ToString("N2") + " ₪";
                        }
                    }
                    catch
                    {
                        decimal fallbackCost = 0m;

                        foreach (var inv in investmentsFromDb)
                        {
                            decimal fx = inv.FxRateToIlsAtPurchase > 0m
                                ? inv.FxRateToIlsAtPurchase
                                : 1m;

                            decimal costIls = inv.TotalInIls > 0m
                                ? inv.TotalInIls
                                : inv.Quantity * inv.BuyPrice * fx;

                            fallbackCost += costIls;
                        }

                        _portfolioValueText.Text =
                            "עלות תיק: " + fallbackCost.ToString("N2") + " ₪";
                    }
                }
                else
                {
                    decimal fallbackCost = 0m;

                    foreach (var inv in investmentsFromDb)
                    {
                        decimal fx = inv.FxRateToIlsAtPurchase > 0m
                            ? inv.FxRateToIlsAtPurchase
                            : 1m;

                        decimal costIls = inv.TotalInIls > 0m
                            ? inv.TotalInIls
                            : inv.Quantity * inv.BuyPrice * fx;

                        fallbackCost += costIls;
                    }

                    _portfolioValueText.Text =
                        "עלות תיק: " + fallbackCost.ToString("N2") + " ₪";
                }

                // 3. מנסים להעשיר את ההשקעות במחיר נוכחי.
                // אם זה נכשל, עדיין מציגים את הרשימה מה-DB.
                var displayInvestments = investmentsFromDb;

                if (App.PortfolioService != null)
                {
                    try
                    {
                        displayInvestments =
                            await App.PortfolioService.EnrichInvestmentsWithPrices(investmentsFromDb)
                            ?? investmentsFromDb;
                    }
                    catch
                    {
                        displayInvestments = investmentsFromDb;
                    }
                }

                // 4. בניית השורות לרשימה
                var rows = new List<InvestmentDisplayRow>();

                foreach (var inv in displayInvestments)
                {
                    decimal qty = inv.Quantity;
                    decimal buyPrice = inv.BuyPrice;

                    decimal fx = inv.FxRateToIlsAtPurchase > 0m
                        ? inv.FxRateToIlsAtPurchase
                        : 1m;

                    decimal costIls = inv.TotalInIls > 0m
                        ? inv.TotalInIls
                        : qty * buyPrice * fx;

                    decimal currentPriceUsd = 0m;

                    try
                    {
                        currentPriceUsd = (decimal)inv.CurrentPrice;
                    }
                    catch
                    {
                        currentPriceUsd = 0m;
                    }

                    decimal currentValueIls = 0m;
                    bool hasCurrentValue = false;

                    if (currentPriceUsd > 0m)
                    {
                        decimal currentValueUsd = qty * currentPriceUsd;

                        if (App.CurrencyService != null)
                        {
                            try
                            {
                                currentValueIls = await App.CurrencyService.ConvertAsync(
                                    currentValueUsd,
                                    "USD",
                                    "ILS"
                                );

                                if (currentValueIls > 0m)
                                    hasCurrentValue = true;
                            }
                            catch
                            {
                                hasCurrentValue = false;
                            }
                        }
                    }

                    decimal profitIls = hasCurrentValue
                        ? currentValueIls - costIls
                        : 0m;

                    rows.Add(new InvestmentDisplayRow
                    {
                        Model = inv,
                        Symbol = inv.StockSymbol,
                        BuyDateText = inv.BuyDate.ToString("yyyy-MM-dd"),
                        QuantityText = "כמות: " + qty.ToString("N2"),
                        BuyPriceText = "מחיר למניה: " + buyPrice.ToString("N2") + " " + inv.OriginalCurrency,
                        CostText = "עלות: " + costIls.ToString("N2") + " ₪",

                        CurrentValueText = hasCurrentValue
                            ? "שווי נוכחי: " + currentValueIls.ToString("N2") + " ₪"
                            : currentPriceUsd > 0m
                                ? "מחיר נוכחי למניה: " + currentPriceUsd.ToString("N2") + " USD"
                                : "שווי נוכחי: לא זמין",

                        PnlText = hasCurrentValue
                            ? $"{(profitIls >= 0m ? "רווח" : "הפסד")}: {profitIls:N2} ₪"
                            : currentPriceUsd > 0m
                                ? "מחיר מניה נטען מה-API"
                                : "רווח/הפסד: לא זמין",

                        IsProfit = profitIls >= 0m
                    });
                }

                _rows = rows;

                if (_adapter == null)
                {
                    _adapter = new InvestmentAdapter(_rows, OnDeleteClicked);
                    _recycler.SetAdapter(_adapter);
                }
                else
                {
                    _adapter.UpdateData(_rows);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בטעינת השקעות: " + ex.Message, ToastLength.Long).Show();
            }
        }

        private async Task SaveInvestmentAsync()
        {
            if (_isSaving)
                return;

            try
            {
                _isSaving = true;

                if (_saveInvestmentButton != null)
                    _saveInvestmentButton.Enabled = false;

                if (_addInvestmentButton != null)
                {
                    _addInvestmentButton.Enabled = false;
                    _addInvestmentButton.Text = "שומר...";
                }

                if (_userId <= 0)
                {
                    Toast.MakeText(this, "לא נמצא משתמש מחובר", ToastLength.Long).Show();
                    return;
                }

                var symbol = (_symbolInput.Text ?? "").Trim().ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(symbol))
                {
                    Toast.MakeText(this, "סימבול חובה", ToastLength.Short).Show();
                    return;
                }

                if (!TryParseDecimalInput(_quantityInput.Text, out var qty) || qty <= 0)
                {
                    Toast.MakeText(this, "כמות לא תקינה", ToastLength.Short).Show();
                    return;
                }

                if (!TryParseDecimalInput(_buyPriceInput.Text, out var buyPrice) || buyPrice <= 0)
                {
                    Toast.MakeText(this, "מחיר קנייה לא תקין", ToastLength.Short).Show();
                    return;
                }

                DateTime buyDate;

                if (!DateTime.TryParseExact(
                        _buyDateInput.Text,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out buyDate))
                {
                    Toast.MakeText(this, "תאריך קנייה לא תקין", ToastLength.Short).Show();
                    return;
                }

                if (buyDate.Date > DateTime.Today)
                {
                    Toast.MakeText(this, "אי אפשר לבחור תאריך עתידי", ToastLength.Short).Show();
                    return;
                }

                var originalCurrency = (_currencySpinner.SelectedItem?.ToString() ?? "ILS")
                    .Trim()
                    .ToUpperInvariant();

                if (originalCurrency != "ILS" && originalCurrency != "USD" && originalCurrency != "EUR")
                {
                    Toast.MakeText(this, "מטבע לא תקין", ToastLength.Short).Show();
                    return;
                }

                if (App.InvestmentService == null)
                {
                    Toast.MakeText(this, "שירות השקעות לא זמין", ToastLength.Short).Show();
                    return;
                }

                await App.InvestmentService.AddInvestment(
                    _userId,
                    symbol,
                    qty,
                    buyPrice,
                    buyDate,
                    originalCurrency
                );

                HideKeyboard(_addFormLayout);

                _symbolInput.Text = "";
                _quantityInput.Text = "";
                _buyPriceInput.Text = "";
                _buyDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
                _currencySpinner.SetSelection(0);

                _addFormLayout.Visibility = ViewStates.Gone;

                if (_addInvestmentButton != null)
                    _addInvestmentButton.Text = "הוסף השקעה";

                await LoadDataAsync();

                Toast.MakeText(this, "השקעה נשמרה", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בשמירת השקעה: " + ex.Message, ToastLength.Long).Show();
            }
            finally
            {
                _isSaving = false;

                if (_saveInvestmentButton != null)
                    _saveInvestmentButton.Enabled = true;

                if (_addInvestmentButton != null)
                {
                    _addInvestmentButton.Enabled = true;
                    _addInvestmentButton.Text = _addFormLayout.Visibility == ViewStates.Visible
                        ? "שמור השקעה"
                        : "הוסף השקעה";
                }
            }
        }

        private bool TryParseDecimalInput(string text, out decimal value)
        {
            text = (text ?? string.Empty).Trim().Replace(",", ".");

            return decimal.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private void HideKeyboard(View view = null)
        {
            try
            {
                var imm = (InputMethodManager)GetSystemService(InputMethodService);
                var targetView = view ?? CurrentFocus ?? Window?.DecorView;

                if (imm != null && targetView != null)
                {
                    imm.HideSoftInputFromWindow(targetView.WindowToken, HideSoftInputFlags.None);
                    targetView.ClearFocus();
                }
            }
            catch
            {
            }
        }

        private async void OnDeleteClicked(Investment inv)
        {
            if (inv == null)
                return;

            new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetTitle("מחיקה")
                .SetMessage($"למחוק את {inv.StockSymbol}?")
                .SetNegativeButton("ביטול", (s, e) => { })
                .SetPositiveButton("מחק", async (s, e) =>
                {
                    try
                    {
                        if (App.InvestmentService == null)
                        {
                            Toast.MakeText(this, "שירות השקעות לא זמין", ToastLength.Short).Show();
                            return;
                        }

                        await App.InvestmentService.DeleteInvestment(_userId, inv.InvestmentID);
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "שגיאה במחיקה: " + ex.Message, ToastLength.Long).Show();
                    }
                })
                .Show();
        }
    }
}