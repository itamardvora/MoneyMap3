using Android.Graphics;
using AndroidX.RecyclerView.Widget;

namespace MoneyMap.Utils
{
    public class CardSpacingDecoration : RecyclerView.ItemDecoration
    {
        private readonly int _spacePx;
        public CardSpacingDecoration(int spacePx) { _spacePx = spacePx; }

        public override void GetItemOffsets(Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
        {
            outRect.Left = _spacePx;
            outRect.Right = _spacePx;
            outRect.Top = _spacePx / 2;
            outRect.Bottom = _spacePx / 2;
        }
    }
}
