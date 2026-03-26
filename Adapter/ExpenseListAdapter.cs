

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System.Collections.Generic;
using MoneyMap.Models;
using System;

namespace MoneyMap.UI
{
    public class ExpenseListAdapter : RecyclerView.Adapter
    {
        private List<Expense> _items;
        public ExpenseListAdapter(List<Expense> items) { _items = items ?? new List<Expense>(); }
        public override int ItemCount => _items.Count;// אומר כמה שורות להציג 

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)// טוען את קובץ העיצוב 
        {
            var v = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_expense, parent, false);
            return new VH(v);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var h = (VH)holder;
            var e = _items[position];

            h.tvDate.Text = e.Date.ToString("dd.MM.yyyy");
            h.tvReason.Text = e.Reason;
            h.tvAmount.Text = $"{e.Amount:n2} ₪";

            // תמונת קבלה
            if (!string.IsNullOrWhiteSpace(e.ReceiptPath))
            {
                h.imgThumb.Visibility = ViewStates.Visible;
                h.imgThumb.SetImageURI(Android.Net.Uri.Parse(e.ReceiptPath));

                h.imgThumb.Click -= h.ImageClickHandler;
                h.ImageClickHandler = (s, ev) =>
                {
                    var img = new ImageView(holder.ItemView.Context);
                    img.SetImageURI(Android.Net.Uri.Parse(e.ReceiptPath));
                    new AndroidX.AppCompat.App.AlertDialog.Builder(holder.ItemView.Context)
                        .SetView(img)
                        .SetPositiveButton("סגור", (d, _) => { })
                        .Show();
                };
                h.imgThumb.Click += h.ImageClickHandler;
            }
            else
            {
                h.imgThumb.Visibility = ViewStates.Gone;
                h.imgThumb.SetImageDrawable(null);
                if (h.ImageClickHandler != null) h.imgThumb.Click -= h.ImageClickHandler;
                h.ImageClickHandler = null;
            }
        }

        public void Update(List<Expense> list)
        {
            _items = list ?? new List<Expense>();
            NotifyDataSetChanged();
        }

        class VH : RecyclerView.ViewHolder
        {
            public TextView tvDate { get; }
            public TextView tvReason { get; }
            public TextView tvAmount { get; }
            public ImageView imgThumb { get; }
            public EventHandler ImageClickHandler;

            public VH(View itemView) : base(itemView)
            {
                tvDate = itemView.FindViewById<TextView>(Resource.Id.tvExpenseDate);
                tvReason = itemView.FindViewById<TextView>(Resource.Id.tvExpenseReason);
                tvAmount = itemView.FindViewById<TextView>(Resource.Id.tvExpenseAmount);
                imgThumb = itemView.FindViewById<ImageView>(Resource.Id.imgReceiptThumb);
            }
        }
    }
}
