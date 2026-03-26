using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Models;
using System;
using System.Collections.Generic;
using Android.Graphics;

namespace MoneyMap.Adapters
{
    // שורת תצוגה מוכנה – כל הטקסטים כבר עם מטבע וסכומים
    public class InvestmentDisplayRow
    {
        public Investment Model { get; set; }

        public string Symbol { get; set; }
        public string BuyDateText { get; set; }
        public string QuantityText { get; set; }
        public string BuyPriceText { get; set; }
        public string CostText { get; set; }
        public string CurrentValueText { get; set; }
        public string PnlText { get; set; }
        public bool IsProfit { get; set; }
    }

    public class InvestmentAdapter : RecyclerView.Adapter
    {
        private List<InvestmentDisplayRow> _rows;
        private readonly Action<Investment> _onDeleteClicked;

        public InvestmentAdapter(List<InvestmentDisplayRow> rows, Action<Investment> onDeleteClicked = null)
        {
            _rows = rows ?? new List<InvestmentDisplayRow>();
            _onDeleteClicked = onDeleteClicked;
        }

        public override int ItemCount => _rows.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.investment_item, parent, false);
            return new InvestmentViewHolder(itemView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var vh = (InvestmentViewHolder)holder;
            var row = _rows[position];

            vh.SymbolText.Text = row.Symbol;
            vh.BuyDateText.Text = row.BuyDateText;
            vh.QuantityText.Text = row.QuantityText;
            vh.BuyPriceText.Text = row.BuyPriceText;
            vh.CostText.Text = row.CostText;
            vh.CurrentValueText.Text = row.CurrentValueText;
            vh.PnlText.Text = row.PnlText;

            // צבע רווח/הפסד
            vh.PnlText.SetTextColor(row.IsProfit
                ? Color.Rgb(0, 150, 0)
                : Color.Rgb(200, 0, 0));

            vh.BindDelete(row.Model, _onDeleteClicked);
        }

        public void UpdateData(List<InvestmentDisplayRow> newRows)
        {
            _rows = newRows ?? new List<InvestmentDisplayRow>();
            NotifyDataSetChanged();
        }
    }

    public class InvestmentViewHolder : RecyclerView.ViewHolder, View.IOnClickListener
    {
        public TextView SymbolText { get; }
        public TextView BuyDateText { get; }
        public TextView QuantityText { get; }
        public TextView BuyPriceText { get; }
        public TextView CostText { get; }
        public TextView CurrentValueText { get; }
        public TextView PnlText { get; }
        public Button DeleteButton { get; }

        private Investment _boundItem;
        private Action<Investment> _onDelete;

        public InvestmentViewHolder(View itemView) : base(itemView)
        {
            SymbolText = itemView.FindViewById<TextView>(Resource.Id.symbolText);
            BuyDateText = itemView.FindViewById<TextView>(Resource.Id.buyDateText);
            QuantityText = itemView.FindViewById<TextView>(Resource.Id.qtyText);
            BuyPriceText = itemView.FindViewById<TextView>(Resource.Id.buyPriceText);
            CostText = itemView.FindViewById<TextView>(Resource.Id.costText);
            CurrentValueText = itemView.FindViewById<TextView>(Resource.Id.currentValueText);
            PnlText = itemView.FindViewById<TextView>(Resource.Id.pnlText);
            DeleteButton = itemView.FindViewById<Button>(Resource.Id.deleteButton);

            DeleteButton?.SetOnClickListener(this);
        }

        public void BindDelete(Investment item, Action<Investment> onDelete)
        {
            _boundItem = item;
            _onDelete = onDelete;
        }

        public void OnClick(View v)
        {
            if (v == DeleteButton)
                _onDelete?.Invoke(_boundItem);
        }
    }
}
