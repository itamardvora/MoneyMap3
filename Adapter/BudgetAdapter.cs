//using System;
//using System.Collections.Generic;
//using Android.Views;
//using Android.Widget;
//using AndroidX.RecyclerView.Widget;
//using Android.Graphics;

//namespace MoneyMap.Adapters
//{
//    public interface IBudgetItemListener
//    {
//        void OnViewExpenses(int categoryId, string categoryName);
//    }

//    public class FormattedBudgetRow
//    {
//        public int CategoryID { get; set; }
//        public string CategoryName { get; set; }
//        public string PlannedText { get; set; }
//        public string SpentText { get; set; }
//        public string RemainingText { get; set; }
//        public int Progress { get; set; }
//    }

//    public class FormattedBudgetAdapter : RecyclerView.Adapter
//    {
//        private List<FormattedBudgetRow> _items;
//        private readonly IBudgetItemListener _listener;

//        public FormattedBudgetAdapter(List<FormattedBudgetRow> items, IBudgetItemListener listener)
//        {
//            _items = items ?? new List<FormattedBudgetRow>();
//            _listener = listener;
//        }

//        public override int ItemCount => _items?.Count ?? 0;

//        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
//        {
//            var view = LayoutInflater.From(parent.Context)
//                .Inflate(Resource.Layout.item_budget_row, parent, false);

//            return new VH(view, _listener);
//        }

//        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
//        {
//            var row = (_items != null && position >= 0 && position < _items.Count)
//                ? _items[position]
//                : null;

//            ((VH)holder).Bind(row);
//        }

//        public void UpdateData(List<FormattedBudgetRow> items)
//        {
//            _items = items ?? new List<FormattedBudgetRow>();
//            NotifyDataSetChanged();
//        }

//        private sealed class VH : RecyclerView.ViewHolder
//        {
//            private readonly TextView _name;
//            private readonly TextView _planned;
//            private readonly TextView _spent;
//            private readonly TextView _remaining;
//            private readonly ProgressBar _progress;
//            private readonly Button _viewBtn;

//            private readonly IBudgetItemListener _listener;
//            private FormattedBudgetRow _row;

//            public VH(View itemView, IBudgetItemListener listener) : base(itemView)
//            {
//                _listener = listener;

//                _name = itemView.FindViewById<TextView>(Resource.Id.categoryNameText);
//                _planned = itemView.FindViewById<TextView>(Resource.Id.plannedText);
//                _spent = itemView.FindViewById<TextView>(Resource.Id.spentText);
//                _remaining = itemView.FindViewById<TextView>(Resource.Id.remainingText);
//                _progress = itemView.FindViewById<ProgressBar>(Resource.Id.progressBar);
//                _viewBtn = itemView.FindViewById<Button>(Resource.Id.btnViewExpenses);

//                if (_name == null || _planned == null || _spent == null || _remaining == null || _progress == null || _viewBtn == null)
//                {
//                    throw new Exception("item_budget_row.xml לא תואם ל-IDs שה-FormattedBudgetAdapter מחפש");
//                }

//                _viewBtn.SetOnClickListener(new ClickListener(() =>
//                {
//                    if (_row != null)
//                        _listener?.OnViewExpenses(_row.CategoryID, _row.CategoryName ?? "");
//                }));
//            }

//            public void Bind(FormattedBudgetRow row)
//            {
//                _row = row;

//                if (row == null)
//                {
//                    _name.Text = "קטגוריה לא זמינה";
//                    _planned.Text = "מתוכנן: 0";
//                    _spent.Text = "הוצאה: 0";
//                    _remaining.Text = "נותר: 0";
//                    _progress.Progress = 0;
//                    return;
//                }

//                _name.Text = row.CategoryName ?? "";
//                _planned.Text = row.PlannedText ?? "";
//                _spent.Text = row.SpentText ?? "";
//                _remaining.Text = row.RemainingText ?? "";

//                var progress = Math.Max(0, Math.Min(100, row.Progress));
//                _progress.Progress = progress;

//                if (progress < 60)
//                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(0, 160, 0));
//                else if (progress < 85)
//                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(255, 152, 0));
//                else if (progress < 100)
//                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(244, 67, 54));
//                else
//                    _progress.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Color.Rgb(183, 28, 28));
//            }

//            private sealed class ClickListener : Java.Lang.Object, View.IOnClickListener
//            {
//                private readonly Action _onClick;
//                public ClickListener(Action a) { _onClick = a; }
//                public void OnClick(View v) => _onClick?.Invoke();
//            }
//        }
//    }
//}
using System;
using System.Collections.Generic;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace MoneyMap.Adapters
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
        public int Progress { get; set; }
    }

    public class FormattedBudgetAdapter : RecyclerView.Adapter
    {
        private List<FormattedBudgetRow> _items;
        private readonly IBudgetItemListener _listener;

        public FormattedBudgetAdapter(List<FormattedBudgetRow> items, IBudgetItemListener listener)
        {
            _items = items ?? new List<FormattedBudgetRow>();
            _listener = listener;
        }

        public override int ItemCount => _items?.Count ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_budget_row, parent, false);

            return new VH(view, _listener);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var row = (_items != null && position >= 0 && position < _items.Count)
                ? _items[position]
                : null;

            ((VH)holder).Bind(row);
        }

        public void UpdateData(List<FormattedBudgetRow> items)
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
            private readonly View _statusDot;

            private readonly IBudgetItemListener _listener;
            private FormattedBudgetRow _row;

            public VH(View itemView, IBudgetItemListener listener) : base(itemView)
            {
                _listener = listener;

                _name = itemView.FindViewById<TextView>(Resource.Id.categoryNameText);
                _planned = itemView.FindViewById<TextView>(Resource.Id.plannedText);
                _spent = itemView.FindViewById<TextView>(Resource.Id.spentText);
                _remaining = itemView.FindViewById<TextView>(Resource.Id.remainingText);
                _statusDot = itemView.FindViewById<View>(Resource.Id.statusDot);

                if (_name == null || _planned == null || _spent == null || _remaining == null || _statusDot == null)
                    throw new Exception("item_budget_row.axml לא תואם ל-IDs שה-FormattedBudgetAdapter מחפש");

                itemView.Click += (s, e) =>
                {
                    if (_row != null)
                        _listener?.OnViewExpenses(_row.CategoryID, _row.CategoryName ?? "");
                };
            }

            public void Bind(FormattedBudgetRow row)
            {
                _row = row;

                if (row == null)
                {
                    _name.Text = "קטגוריה לא זמינה";
                    _planned.Text = "0";
                    _spent.Text = "0";
                    _remaining.Text = "0";
                    _statusDot.SetBackgroundColor(Color.LightGray);
                    return;
                }

                _name.Text = row.CategoryName ?? "";
                _planned.Text = row.PlannedText ?? "";
                _spent.Text = row.SpentText ?? "";
                _remaining.Text = row.RemainingText ?? "";

                var progress = Math.Max(0, row.Progress);

                if (progress < 50)
                    _statusDot.SetBackgroundColor(Color.Rgb(22, 163, 74));      // ירוק
                else if (progress < 100)
                    _statusDot.SetBackgroundColor(Color.Rgb(249, 115, 22));     // כתום
                else
                    _statusDot.SetBackgroundColor(Color.Rgb(220, 38, 38));      // אדום
            }
        }
    }
}