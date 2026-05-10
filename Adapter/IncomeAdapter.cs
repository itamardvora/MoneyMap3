using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace MoneyMap.Adapters
{
    public class IncomeDisplayRow
    {
        public string SourceText { get; set; }
        public string DateText { get; set; }
        public string AmountText { get; set; }
    }

    public class IncomeAdapter : RecyclerView.Adapter
    {
        private List<IncomeDisplayRow> _items;

        public IncomeAdapter(List<IncomeDisplayRow> items)
        {
            _items = items ?? new List<IncomeDisplayRow>();
        }

        public override int ItemCount => _items?.Count ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_income_row, parent, false);

            return new IncomeViewHolder(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = (_items != null && position >= 0 && position < _items.Count)
                ? _items[position]
                : null;

            ((IncomeViewHolder)holder).Bind(item);
        }

        public void Update(List<IncomeDisplayRow> items)
        {
            _items = items ?? new List<IncomeDisplayRow>();
            NotifyDataSetChanged();
        }

        private sealed class IncomeViewHolder : RecyclerView.ViewHolder
        {
            private readonly TextView _sourceText;
            private readonly TextView _dateText;
            private readonly TextView _amountText;

            public IncomeViewHolder(View itemView) : base(itemView)
            {
                _sourceText = itemView.FindViewById<TextView>(Resource.Id.incomeSourceText);
                _dateText = itemView.FindViewById<TextView>(Resource.Id.incomeDateText);
                _amountText = itemView.FindViewById<TextView>(Resource.Id.incomeAmountText);

                if (_sourceText == null || _dateText == null || _amountText == null)
                    throw new Exception("item_income_row.xml לא תואם ל-IDs שה-IncomeAdapter מחפש");
            }

            public void Bind(IncomeDisplayRow income)
            {
                if (income == null)
                {
                    _sourceText.Text = "הכנסה";
                    _dateText.Text = "-";
                    _amountText.Text = "0 ₪";
                    return;
                }

                _sourceText.Text = string.IsNullOrWhiteSpace(income.SourceText)
                    ? "ללא מקור"
                    : income.SourceText;

                _dateText.Text = string.IsNullOrWhiteSpace(income.DateText)
                    ? "-"
                    : income.DateText;

                _amountText.Text = string.IsNullOrWhiteSpace(income.AmountText)
                    ? "0 ₪"
                    : income.AmountText;
            }
        }
    }
}