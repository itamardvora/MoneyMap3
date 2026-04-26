using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MoneyMap.Models;

namespace MoneyMap.Adapters
{
    public class AdminUserAdapter : RecyclerView.Adapter
    {
        private List<User> _items;
        private readonly Action<User> _onDeleteClicked;

        public AdminUserAdapter(List<User> items, Action<User> onDeleteClicked)
        {
            _items = items ?? new List<User>();
            _onDeleteClicked = onDeleteClicked;
        }

        public override int ItemCount => _items?.Count ?? 0;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.item_admin_user, parent, false);

            return new AdminUserViewHolder(view, _onDeleteClicked);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = (_items != null && position >= 0 && position < _items.Count)
                ? _items[position]
                : null;

            ((AdminUserViewHolder)holder).Bind(item);
        }

        public void UpdateData(List<User> items)
        {
            _items = items ?? new List<User>();
            NotifyDataSetChanged();
        }

        private sealed class AdminUserViewHolder : RecyclerView.ViewHolder
        {
            private readonly TextView _nameText;
            private readonly Button _deleteButton;
            private readonly Action<User> _onDeleteClicked;
            private User _currentUser;

            public AdminUserViewHolder(View itemView, Action<User> onDeleteClicked) : base(itemView)
            {
                _nameText = itemView.FindViewById<TextView>(Resource.Id.adminUserNameText);
                _deleteButton = itemView.FindViewById<Button>(Resource.Id.adminDeleteButton);
                _onDeleteClicked = onDeleteClicked;

                _deleteButton.Click += (s, e) =>
                {
                    if (_currentUser != null)
                        _onDeleteClicked?.Invoke(_currentUser);
                };
            }

            public void Bind(User user)
            {
                _currentUser = user;

                if (user == null)
                {
                    _nameText.Text = string.Empty;
                    _deleteButton.Enabled = false;
                    return;
                }

                _nameText.Text = string.IsNullOrWhiteSpace(user.FullName)
                    ? user.Email
                    : user.FullName;

                _deleteButton.Enabled = true;
            }
        }
    }
}