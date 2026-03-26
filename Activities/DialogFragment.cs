// Activities/CategoryExpensesActivity.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using MoneyMap.UI;
using MoneyMap.Models;
using MoneyMap.Services;

namespace MoneyMap.Activities
{
    [Activity(Label = "הוצאות בקטגוריה", Theme = "@style/AppTheme", Exported = false)]
    public class CategoryExpensesActivity : AppCompatActivity
    {
        public const string ExtraCategoryId = "extra_category_id";
        public const string ExtraMonthIso = "extra_month_iso"; // yyyy-MM-01

        private RecyclerView _recycler;
        private ExpenseListAdapter _adapter;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_category_expenses);

            _recycler = FindViewById<RecyclerView>(Resource.Id.rvCategoryExpenses);
            _recycler.SetLayoutManager(new LinearLayoutManager(this));
            _adapter = new ExpenseListAdapter(new List<Expense>());
            _recycler.SetAdapter(_adapter);

            // פרמטרים
            int categoryId = Intent.GetIntExtra(ExtraCategoryId, -1);
            var monthIso = Intent.GetStringExtra(ExtraMonthIso) ?? DateTime.Today.ToString("yyyy-MM-01");
            var month = DateTime.Parse(monthIso);

            int userId = UserSession.LoggedInUserId ?? 0;
            var list = await App.ExpenseService.GetMonthlyExpenses(userId, month);
            list = list.Where(x => x.CategoryID == categoryId).OrderByDescending(x => x.Date).ToList();

            _adapter.Update(list);
        }
    }
}
