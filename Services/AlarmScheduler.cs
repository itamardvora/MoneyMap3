using System;
using Android.App;
using Android.Content;

namespace MoneyMap.Services
{
    // קובע את העזעקה מתי לפעול שם את הטטימר 
    public static class AlarmScheduler
    {
        private const int RequestCode = 1001;

        public static void ScheduleDaily(Context ctx, int hour, int minute)
        {
            var am = (AlarmManager)ctx.GetSystemService(Context.AlarmService);

            var intent = new Intent(ctx, typeof(AlarmFireReceiver));
            var pending = PendingIntent.GetBroadcast(
                ctx, RequestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var nowMs = Java.Lang.JavaSystem.CurrentTimeMillis();
            var cal = Java.Util.Calendar.Instance;
            cal.TimeInMillis = nowMs;
            cal.Set(Java.Util.CalendarField.HourOfDay, hour);
            cal.Set(Java.Util.CalendarField.Minute, minute);
            cal.Set(Java.Util.CalendarField.Second, 0);

            var firstMs = cal.TimeInMillis;
            if (firstMs <= nowMs)
            {
                cal.Add(Java.Util.CalendarField.DayOfYear, 1);
                firstMs = cal.TimeInMillis;
            }

            am.SetInexactRepeating(AlarmType.RtcWakeup, firstMs, AlarmManager.IntervalDay, pending);
        }

        public static void Cancel(Context ctx)
        {
            var am = (AlarmManager)ctx.GetSystemService(Context.AlarmService);
            var intent = new Intent(ctx, typeof(AlarmFireReceiver));
            var pending = PendingIntent.GetBroadcast(
                ctx, RequestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            am.Cancel(pending);
        }
    }
}
