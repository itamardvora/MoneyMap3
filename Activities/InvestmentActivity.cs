using Android.App;
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
using System.Threading.Tasks;

namespace MoneyMap.Activities
{
    [Activity(Label = "השקעות")]
    public class InvestmentActivity : AppCompatActivity
    {
        private RecyclerView _recyclerView;
        private TextView _portfolioValueText;

        private Button _addInvestmentButton, _saveInvestmentButton;
        private LinearLayout _addFormLayout;
        private EditText _symbolInput, _quantityInput, _buyPriceInput, _buyDateInput;
        private DateTime _selectedDate;

        private InvestmentAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_investment);


            //

            var bottomNav = FindViewById<BottomNavigationView>(Resource.Id.bottomNavigation);
            bottomNav.NavigationItemSelected += (s, e) =>
            {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.menu_home:
                        StartActivity(typeof(HomeActivity));
                        break;

                    case Resource.Id.menu_investments:
                        // כבר במסך השקעות – לא עושים כלום
                        break;

                    case Resource.Id.menu_budget:
                        StartActivity(typeof(BudgetActivity));
                        break;
                }
            };


            //

            // קישור רכיבים
            _portfolioValueText = FindViewById<TextView>(Resource.Id.portfolioValueText);
            _recyclerView = FindViewById<RecyclerView>(Resource.Id.investmentRecyclerView);

            _addInvestmentButton = FindViewById<Button>(Resource.Id.addInvestmentButton);
            _saveInvestmentButton = FindViewById<Button>(Resource.Id.saveInvestmentButton);
            _addFormLayout = FindViewById<LinearLayout>(Resource.Id.addFormLayout);

            _symbolInput = FindViewById<EditText>(Resource.Id.symbolInput);
            _quantityInput = FindViewById<EditText>(Resource.Id.quantityInput);
            _buyPriceInput = FindViewById<EditText>(Resource.Id.buyPriceInput);
            _buyDateInput = FindViewById<EditText>(Resource.Id.buyDateInput);

            // אתחול ברירת מחדל לתאריך קנייה
            _selectedDate = DateTime.Now;
            _buyDateInput.Text = _selectedDate.ToString("dd/MM/yyyy");

            // פתיחת דיאלוג בחירת תאריך
            _buyDateInput.Click += (s, e) =>
            {
                var datePicker = new DatePickerDialog(this,
                    (sender, args) =>
                    {
                        _selectedDate = args.Date;
                        _buyDateInput.Text = _selectedDate.ToString("dd/MM/yyyy");
                    },
                    _selectedDate.Year, _selectedDate.Month - 1, _selectedDate.Day);
                datePicker.Show();
            };

            // הגדרת RecyclerView
            _recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new InvestmentAdapter(new List<Investment>(), async (investment) =>
            {
                await App.Investments.DeleteInvestment(investment.InvestmentID, investment.UserID);
                Toast.MakeText(this, "ההשקעה נמחקה", ToastLength.Short).Show();
                await LoadInvestmentsAsync(); // נטען מחדש את ההשקעות ונעדכן את שווי התיק
            });

            _recyclerView.SetAdapter(_adapter);

            // לחצן לפתיחת הטופס
            _addInvestmentButton.Click += (s, e) =>
            {
                _addFormLayout.Visibility = ViewStates.Visible;
            };

            // לחצן שמירה
            _saveInvestmentButton.Click += async (s, e) =>
            {
                string symbol = _symbolInput.Text?.Trim();
                bool qOk = double.TryParse(_quantityInput.Text, out double quantity);
                bool pOk = double.TryParse(_buyPriceInput.Text, out double buyPrice);

                if (string.IsNullOrWhiteSpace(symbol) || !qOk || !pOk)
                {
                    Toast.MakeText(this, "נא למלא את כל השדות בצורה תקינה", ToastLength.Short).Show();
                    return;
                }

                var newInvestment = new Investment
                {
                    UserID = UserSession.LoggedInUserId ?? 0,
                    StockSymbol = symbol.ToUpper(),
                    Quantity = (decimal)quantity,
                    BuyPrice = (decimal)buyPrice,
                    BuyDate = _selectedDate
                };

                await App.Investments.AddInvestment(newInvestment);
                Toast.MakeText(this, "ההשקעה נוספה בהצלחה", ToastLength.Short).Show();

                _symbolInput.Text = "";
                _quantityInput.Text = "";
                _buyPriceInput.Text = "";
                _buyDateInput.Text = DateTime.Now.ToString("dd/MM/yyyy");
                _selectedDate = DateTime.Now;
                _addFormLayout.Visibility = ViewStates.Gone;

                await LoadInvestmentsAsync();
            };

            await LoadInvestmentsAsync();
        }

        private async Task LoadInvestmentsAsync()
        {
            int userId = UserSession.LoggedInUserId ?? 0;
            Console.WriteLine($"🔍 LoadInvestmentsAsync - userId = {userId}");

            var investments = await App.Investments.GetAllByUser(userId);
            Console.WriteLine($"📦 נמצאו {investments.Count} השקעות");

            foreach (var inv in investments)
            {
                Console.WriteLine($"📝 {inv.StockSymbol}, כמות: {inv.Quantity}, מחיר: {inv.BuyPrice}");
            }

            var enriched = await App.PortfolioService.EnrichInvestmentsWithPrices(investments);

            foreach (var inv in enriched)
            {
                Console.WriteLine($"💰 {inv.StockSymbol}, מחיר נוכחי: {inv.CurrentPrice}");
            }

            _adapter.UpdateData(enriched);

            decimal totalValue = 0m;
            foreach (var inv in enriched)
            {
                totalValue += inv.Quantity * (decimal)inv.CurrentPrice;
            }

            Console.WriteLine($"📊 סך שווי תיק: {totalValue}");
            _portfolioValueText.Text = $"שווי תיק: {totalValue:n2} $";
        }

    }
}
