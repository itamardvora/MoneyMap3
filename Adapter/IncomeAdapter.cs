using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Models;

namespace MoneyMap.Adapters
{
    public class IncomeAdapter : RecyclerView.Adapter
    {
        private List<Income> _items;

        public IncomeAdapter(List<Income> items)
        {
            _items = items ?? new List<Income>();
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

        public void Update(List<Income> items)
        {
            _items = items ?? new List<Income>();
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
                    throw new Exception("item_income_row.axml לא תואם ל-IDs שה-IncomeAdapter מחפש");
            }

            public void Bind(Income income)
            {
                if (income == null)
                {
                    _sourceText.Text = "הכנסה";
                    _dateText.Text = "-";
                    _amountText.Text = "0 ₪";
                    return;
                }

                _sourceText.Text = string.IsNullOrWhiteSpace(income.Source) ? "ללא מקור" : income.Source;
                _dateText.Text = income.Date.ToString("dd/MM/yyyy");
                _amountText.Text = $"{income.Amount:N0} ₪";
            }
        }
    }
}