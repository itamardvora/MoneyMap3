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

namespace MoneyMap.Activities
{
    [Activity(Label = "השקעות", Theme = "@style/AppTheme", Exported = false)]
    public class InvestmentActivity : AppCompatActivity
    {
        private RecyclerView _recycler;
        private InvestmentAdapter _adapter;
        private TextView _portfolioValueText;
        private LinearLayout _addFormLayout;
        private Button _addInvestmentButton, _saveInvestmentButton;

        private EditText _symbolInput, _quantityInput, _buyPriceInput, _buyDateInput;
        private Spinner _currencySpinner; // מטבע קנייה של ההשקעה

        private int _userId;
        private List<Investment> _investments = new List<Investment>();
        private List<InvestmentDisplayRow> _rows = new List<InvestmentDisplayRow>();

        private BottomNavigationView _bottomNavigation;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_investment);

            // Views
            _portfolioValueText = FindViewById<TextView>(Resource.Id.portfolioValueText);
            _recycler = FindViewById<RecyclerView>(Resource.Id.investmentRecyclerView);
            _recycler.SetLayoutManager(new LinearLayoutManager(this));

            _addFormLayout = FindViewById<LinearLayout>(Resource.Id.addFormLayout);
            _addInvestmentButton = FindViewById<Button>(Resource.Id.addInvestmentButton);
            _saveInvestmentButton = FindViewById<Button>(Resource.Id.saveInvestmentButton);

            _symbolInput = FindViewById<EditText>(Resource.Id.symbolInput);
            _quantityInput = FindViewById<EditText>(Resource.Id.quantityInput);
            _buyPriceInput = FindViewById<EditText>(Resource.Id.buyPriceInput);
            _buyDateInput = FindViewById<EditText>(Resource.Id.buyDateInput);
            _currencySpinner = FindViewById<Spinner>(Resource.Id.investmentCurrencySpinner);

            _bottomNavigation = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);

            // משתמש מחובר – כרגע לפי מה שהיה אצלך
            _userId = Preferences.Get("LoggedInUserId", 1);

            // אתחול הליבה אם צריך
            if (!App.IsFullyReady())
                await App.InitAfterLoginAsync();

            SetupBottomNavigation();
            SetupBuyDatePicker();
            SetupCurrencySpinnerForPurchase();
            SetupAddFormButtons();

            // טען נתונים + חישובים + המרות מטבע
            await LoadDataAsync();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            // אם המטבע המועדף השתנה במסך הבית – נעדכן שוב פורמט
            await LoadDataAsync();
        }

        // ===== תפריט תחתון =====
        private void SetupBottomNavigation()
        {
            if (_bottomNavigation == null) return;

            _bottomNavigation.SelectedItemId = Resource.Id.menu_investments;

            _bottomNavigation.NavigationItemSelected += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        StartActivity(typeof(HomeActivity));
                        Finish();
                        break;
                    case Resource.Id.menu_investments:
                        // כבר כאן
                        break;
                    case Resource.Id.menu_budget:
                        StartActivity(typeof(BudgetActivity));
                        Finish();
                        break;
                }
            };
        }

        // ===== DatePicker לשדה תאריך קנייה =====
        private void SetupBuyDatePicker()
        {
            if (_buyDateInput == null) return;

            _buyDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
            _buyDateInput.Focusable = false;
            _buyDateInput.Click += (s, e) =>
            {
                var today = DateTime.Today;
                var dp = new DatePickerDialog(this, (sender, ev) =>
                {
                    _buyDateInput.Text = ev.Date.ToString("yyyy-MM-dd");
                }, today.Year, today.Month - 1, today.Day);

                dp.Show();
            };
        }

        // ===== Spinner למטבע קנייה של ההשקעה =====
        private void SetupCurrencySpinnerForPurchase()
        {
            var options = new[] { "ILS", "USD", "EUR" };
            var adapter = new ArrayAdapter<string>(
                this,
                Android.Resource.Layout.SimpleSpinnerItem,
                options);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            _currencySpinner.Adapter = adapter;
            _currencySpinner.SetSelection(0); // ברירת מחדל – ILS
        }

        // ===== כפתורי פתיחה/שמירה של הטופס =====
        private void SetupAddFormButtons()
        {
            _addInvestmentButton.Click += (s, e) =>
            {
                _addFormLayout.Visibility = _addFormLayout.Visibility == ViewStates.Visible
                    ? ViewStates.Gone
                    : ViewStates.Visible;
            };

            _saveInvestmentButton.Click += async (s, e) => await SaveInvestmentAsync();
        }

        // ===== טענת נתונים, חישוב שווי תיק והכנת שורות תצוגה =====
        private async Task LoadDataAsync()
        {
            try
            {
                if (App.InvestmentService == null || App.PortfolioService == null)
                {
                    Toast.MakeText(this, "שירותי השקעות לא זמינים כרגע", ToastLength.Short).Show();
                    return;
                }

                _investments = await App.InvestmentService.GetInvestmentsForUserAsync(_userId)
                               ?? new List<Investment>();

                // מעשירים במחירים נוכחיים
                _investments = await App.PortfolioService.EnrichInvestmentsWithPrices(_investments);

                var rows = new List<InvestmentDisplayRow>();
                decimal totalPortfolioValueIls = 0m;

                foreach (var inv in _investments)
                {
                    decimal qty = inv.Quantity;
                    decimal buyPrice = inv.BuyPrice;
                    decimal currentPerShareUsd = 0m;

                    try
                    {
                        // CurrentPrice שמגיע מה-API – בדולרים
                        currentPerShareUsd = (decimal)inv.CurrentPrice;
                    }
                    catch
                    {
                        currentPerShareUsd = 0m;
                    }

                    // עלות ב-ILS (הנחה: BuyPrice כבר מאוחסן בשקלים)
                    decimal costIls = qty * buyPrice;

                    // שווי נוכחי: קודם בדולר, ואז המרה ל-ILS לפני פורמט
                    decimal currentValueIls = 0m;
                    if (currentPerShareUsd > 0)
                    {
                        var currentValueUsd = qty * currentPerShareUsd;

                        if (App.CurrencyService != null)
                        {
                            try
                            {
                                currentValueIls = await App.CurrencyService.ConvertAsync(currentValueUsd, "USD", "ILS");
                            }
                            catch
                            {
                                // במקרה כשל – עדיף להציג את הערך בדולר כאילו הוא שקלים מאשר להראות 0
                                currentValueIls = currentValueUsd;
                            }
                        }
                        else
                        {
                            currentValueIls = currentValueUsd;
                        }
                    }

                    decimal profitIls = currentValueIls - costIls;

                    totalPortfolioValueIls += currentValueIls;

                    string qtyText = $"כמות: {qty:n2}";
                    string buyPriceText = "מחיר קנייה: " + await SafeFormatAsync(buyPrice, 2);
                    string costText = "עלות: " + await SafeFormatAsync(costIls, 2);
                    string currentValueText = "שווי נוכחי: " + await SafeFormatAsync(currentValueIls, 2);

                    string pnlLabel = profitIls >= 0 ? "רווח" : "הפסד";
                    string pnlTextAmount = await SafeFormatAsync(profitIls, 2);
                    string pnlText = $"{pnlLabel}: {pnlTextAmount}";

                    rows.Add(new InvestmentDisplayRow
                    {
                        Model = inv,
                        Symbol = inv.StockSymbol,
                        BuyDateText = inv.BuyDate.ToString("yyyy-MM-dd"),
                        QuantityText = qtyText,
                        BuyPriceText = buyPriceText,
                        CostText = costText,
                        CurrentValueText = currentValueText,
                        PnlText = pnlText,
                        IsProfit = profitIls >= 0
                    });
                }

                _rows = rows;

                // עדכון כרטיס "שווי תיק" במסך – כולל מטבע וסימן
                _portfolioValueText.Text = "שווי תיק: " + await SafeFormatAsync(totalPortfolioValueIls, 2);

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
                Toast.MakeText(this, "שגיאה בטעינת נתונים: " + ex.Message, ToastLength.Long).Show();
            }
        }

        // ===== שמירת השקעה חדשה =====
        private async Task SaveInvestmentAsync()
        {
            try
            {
                var symbol = (_symbolInput.Text ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    Toast.MakeText(this, "סימבול חובה", ToastLength.Short).Show();
                    return;
                }

                if (!decimal.TryParse(_quantityInput.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
                {
                    Toast.MakeText(this, "כמות לא תקינה", ToastLength.Short).Show();
                    return;
                }

                if (!decimal.TryParse(_buyPriceInput.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var buyPrice) || buyPrice <= 0)
                {
                    Toast.MakeText(this, "מחיר קנייה לא תקין", ToastLength.Short).Show();
                    return;
                }

                DateTime buyDate;
                if (!DateTime.TryParseExact(_buyDateInput.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out buyDate))
                {
                    buyDate = DateTime.Today;
                }

                var originalCurrency = (_currencySpinner?.SelectedItem?.ToString() ?? "ILS")
                    .Trim()
                    .ToUpperInvariant();

                if (App.InvestmentService == null)
                {
                    Toast.MakeText(this, "שירות השקעות לא זמין", ToastLength.Short).Show();
                    return;
                }

                // שמירה דרך InvestmentService המלא – כולל תמיכה במטבע וב-TotalInIls
                await App.InvestmentService.AddInvestment(
                    _userId,
                    symbol,
                    qty,
                    buyPrice,
                    buyDate,
                    originalCurrency
                );

                // רענון רשימה ושווי תיק
                await LoadDataAsync();

                // ניקוי טופס
                _symbolInput.Text = "";
                _quantityInput.Text = "";
                _buyPriceInput.Text = "";
                _buyDateInput.Text = DateTime.Today.ToString("yyyy-MM-dd");
                _currencySpinner.SetSelection(0);
                _addFormLayout.Visibility = ViewStates.Gone;

                Toast.MakeText(this, "השקעה נשמרה", ToastLength.Short).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "שגיאה בשמירת השקעה: " + ex.Message, ToastLength.Long).Show();
            }
        }

        // ===== מחיקת השקעה =====
        private async void OnDeleteClicked(Investment inv)
        {
            if (inv == null) return;

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

                        // לטעון שוב את כל הנתונים – גם רשימה וגם שווי תיק
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, "שגיאה במחיקה: " + ex.Message, ToastLength.Long).Show();
                    }
                })
                .Show();
        }

        // ===== עוזרי פורמט למטבע (כמו במסכים אחרים) =====
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
            var code = await SafePreferredCodeAsync();

            try
            {
                if (App.CurrencyService != null)
                    return await App.CurrencyService.FormatAsync(amountIls, "ILS", code, decimals);
            }
            catch
            {
                // נפילה ל-ILS ברירת מחדל
            }

            // פלט בסיסי בשקלים אם אין שירות/רשת
            return amountIls.ToString("N" + decimals) + " ₪";
        }
    }
}
