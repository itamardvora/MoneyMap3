using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Android.Graphics;

namespace MoneyMap.Adapters
    // אחראי על הצגת רשימת תקציבים במסך 
{
    public interface IBudgetItemListener
    {
        void OnViewExpenses(int categoryId, string categoryName);
    }

    public class FormattedBudgetRow
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }

        public string PlannedText { get; set; }
        public string SpentText { get; set; }
        public string RemainingText { get; set; }
        public int Progress { get; set; } // 0–100
    }

    public class FormattedBudgetAdapter : RecyclerView.Adapter
    {
        private List<FormattedBudgetRow> _items;
        private readonly IBudgetItemListener _listener;

        public FormattedBudgetAdapter(List<FormattedBudgetRow> items, IBudgetItemListener listener)
        {
            _items = items ?? new List<FormattedBudgetRow>();// צור רשימה ריקה במקום לקרוס 
            _listener = listener;
        }

        public override int ItemCount => _items.Count;// כמה שורות להציג במסך 

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)// יוצר שורה חדשה ומטפל בנראות 
        {
            var view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_budget_row, parent, false);

            return new VH(view, _listener);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)// אחרי על מילוי נתונים 
        {
            ((VH)holder).Bind(_items[position]);
        }

        public void UpdateData(List<FormattedBudgetRow> items)// אחראי על עדכון הנתנוים 
        {
            _items = items ?? new List<FormattedBudgetRow>();
            NotifyDataSetChanged();
        }

        private sealed class VH : RecyclerView.ViewHolder
        {
            private readonly TextView _name;
            private readonly TextView _planned;
            private readonly TextView _spent;
            private readonly TextView _remaining;
            private readonly ProgressBar _progress;
            private readonly Button _viewBtn;

            private readonly IBudgetItemListener _listener;
            private FormattedBudgetRow _row;

            public VH(View itemView, IBudgetItemListener listener) : base(itemView)
            {
                _listener = listener;
                _name = itemView.FindViewById<TextView>(Resource.Id.categoryNameText);
                _planned = itemView.FindViewById<TextView>(Resource.Id.plannedText);
                _spent = itemView.FindViewById<TextView>(Resource.Id.spentText);
                _remaining = itemView.FindViewById<TextView>(Resource.Id.remainingText);
                _progress = itemView.FindViewById<ProgressBar>(Resource.Id.progressBar);
                _viewBtn = itemView.FindViewById<Button>(Resource.Id.btnViewExpenses);

                _viewBtn?.SetOnClickListener(new ClickListener(() =>
                {
                    if (_row != null)
                        _listener?.OnViewExpenses(_row.CategoryID, _row.CategoryName);
                }));
            }

            public void Bind(FormattedBudgetRow row)
            {
                _row = row;

                _name.Text = row.CategoryName;
                _planned.Text = row.PlannedText;
                _spent.Text = row.SpentText;
                _remaining.Text = row.RemainingText;

                _progress.Progress = Math.Max(0, Math.Min(100, row.Progress));

                // ---- צבעים חכמים לפס ----

                if (row.Progress < 60)
                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(0, 160, 0)); // ירוק
                else if (row.Progress < 85)
                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(255, 152, 0)); // כתום-ירוק
                else if (row.Progress < 100)
                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(244, 67, 54)); // אדום-כתום
                else
                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(183, 28, 28)); // אדום כהה
            }

            private sealed class ClickListener : Java.Lang.Object, View.IOnClickListener
            {
                private readonly Action _onClick;
                public ClickListener(Action a) { _onClick = a; }
                public void OnClick(View v) => _onClick?.Invoke();
            }
        }
    }
}
