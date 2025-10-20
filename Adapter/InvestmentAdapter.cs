using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Models;
using System;
using System.Collections.Generic;

namespace MoneyMap.Adapters
{
    public class InvestmentAdapter : RecyclerView.Adapter
    {
        private List<Investment> _investments;
        private readonly Action<Investment> _onDeleteClicked;

        public InvestmentAdapter(List<Investment> investments, Action<Investment> onDeleteClicked = null)
        {
            _investments = investments;
            _onDeleteClicked = onDeleteClicked;
        }

        public override int ItemCount => _investments.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var investment = _investments[position];
            var viewHolder = holder as InvestmentViewHolder;

            viewHolder.SymbolText.Text = investment.StockSymbol;
            viewHolder.QuantityText.Text = $"כמות: {investment.Quantity}";
            viewHolder.BuyPriceText.Text = $"מחיר קנייה: {investment.BuyPrice:n2} $";
            viewHolder.CurrentPriceText.Text = $"מחיר נוכחי: {investment.CurrentPrice:n2} $";

            decimal totalCost = investment.Quantity * investment.BuyPrice;
            decimal currentValue = investment.Quantity * (decimal)investment.CurrentPrice;
            decimal profit = currentValue - totalCost;
            decimal returnPercent = totalCost != 0 ? (profit / totalCost) * 100 : 0;

            viewHolder.ProfitText.Text = $"רווח: {profit:n2} $ ({returnPercent:n2}%)";
            //if (profit >= 0)
            //{
            //    viewHolder.ProfitText.Text = $"רווח: {profit:n2} $ ({returnPercent:n2}%)";

            //}
            //else
            //{
            //    viewHolder.ProfitText.Text = $"הפסד: {profit:n2} $ ({returnPercent:n2}%)";


            //}
            //// צבע ירוק לרווח, אדום להפסד
            //if (profit >= 0)
            //{
            //    viewHolder.ProfitText.SetTextColor(Android.Graphics.Color.Rgb(0, 128, 0)); // ירוק כהה
            //}
            //else
            //{
            //    viewHolder.ProfitText.SetTextColor(Android.Graphics.Color.Red); // אדום
            //}
            if (profit >= 0)
            {
                viewHolder.ProfitText.Text = $"רווח: {profit:n2} $ ({returnPercent:n2}%)";
                viewHolder.ProfitText.SetTextColor(Android.Graphics.Color.Green);
            }
            else
            {
                viewHolder.ProfitText.Text = $"הפסד: {profit:n2} $ ({returnPercent:n2}%)";
                viewHolder.ProfitText.SetTextColor(Android.Graphics.Color.Red);
            }

            // לחיצה על כפתור מחיקה
            viewHolder.DeleteButton.Click -= null;
            viewHolder.DeleteButton.Click += (s, e) =>
            {
                _onDeleteClicked?.Invoke(investment);
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.investment_item, parent, false);
            return new InvestmentViewHolder(itemView);
        }

        public void UpdateData(List<Investment> newData)
        {
            _investments = newData;
            NotifyDataSetChanged();
        }
    }

    public class InvestmentViewHolder : RecyclerView.ViewHolder
    {
        public TextView SymbolText { get; private set; }
        public TextView QuantityText { get; private set; }
        public TextView BuyPriceText { get; private set; }
        public TextView CurrentPriceText { get; private set; }
        public TextView ProfitText { get; private set; }
        public Button DeleteButton { get; private set; }

        public InvestmentViewHolder(View itemView) : base(itemView)
        {
            SymbolText = itemView.FindViewById<TextView>(Resource.Id.symbolText);
            QuantityText = itemView.FindViewById<TextView>(Resource.Id.quantityText);
            BuyPriceText = itemView.FindViewById<TextView>(Resource.Id.buyPriceText);
            CurrentPriceText = itemView.FindViewById<TextView>(Resource.Id.currentPriceText);
            ProfitText = itemView.FindViewById<TextView>(Resource.Id.profitText);
            DeleteButton = itemView.FindViewById<Button>(Resource.Id.deleteButton); // ודא שקיים ב־XML
        }
    }
}
