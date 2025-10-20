using Android.Content.Res;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Services;
using System.Collections.Generic;
using Android.Graphics;


namespace MoneyMap.Adapters
{
    public class BudgetAdapter : RecyclerView.Adapter
    {
        private List<BudgetStatus> _budgets;

        public BudgetAdapter(List<BudgetStatus> budgets)
        {
            _budgets = budgets;
        }

        public override int ItemCount => _budgets.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var budget = _budgets[position];
            var viewHolder = holder as BudgetViewHolder;

            viewHolder.CategoryText.Text = $"קטגוריה: {budget.CategoryName}"; // בשלב הבא אפשר להמיר לשם הקטגוריה דרך Service
            viewHolder.LimitText.Text = $"תקציב: {budget.MonthlyLimit:n2} ₪";
            viewHolder.SpentText.Text = $"הוצא: {budget.Spent:n2} ₪";
            viewHolder.RemainingText.Text = $"נותר: {budget.Remaining:n2} ₪";

            // צבע לפי אחוז שימוש
            if (budget.UsagePct < 70)
                viewHolder.Progress.ProgressTintList = ColorStateList.ValueOf(Color.ParseColor("#4CAF50"));
            else if (budget.UsagePct < 100)
                viewHolder.Progress.ProgressTintList = ColorStateList.ValueOf(Color.ParseColor("#FF9800"));
            else
            {
                viewHolder.Progress.ProgressTintList = ColorStateList.ValueOf(Color.ParseColor("#F44336"));
            }

            viewHolder.Progress.Progress = (int)budget.UsagePct;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.budget_item, parent, false);
            return new BudgetViewHolder(view);
        }

        public void UpdateData(List<BudgetStatus> newData)
        {
            _budgets = newData;
            NotifyDataSetChanged();
        }
    }

    public class BudgetViewHolder : RecyclerView.ViewHolder
    {
        public TextView CategoryText { get; }

        public TextView LimitText { get; }
        public TextView SpentText { get; }
        public TextView RemainingText { get; }
        public ProgressBar Progress { get; }

        public BudgetViewHolder(View itemView) : base(itemView)
        {
            CategoryText = itemView.FindViewById<TextView>(Resource.Id.categoryNameText);
            LimitText = itemView.FindViewById<TextView>(Resource.Id.plannedText);
            SpentText = itemView.FindViewById<TextView>(Resource.Id.spentText);
            RemainingText = itemView.FindViewById<TextView>(Resource.Id.remainingText);
            Progress = itemView.FindViewById<ProgressBar>(Resource.Id.progressBar);

        }
    }
}
