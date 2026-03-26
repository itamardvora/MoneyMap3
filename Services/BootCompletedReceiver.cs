using Android.App;
using Android.Content;
using Android.OS;

namespace MoneyMap.Services
    //קובע אזעקה מחדש אם הטלפון נכבה או האאפליקציה עודכנה
{
    [BroadcastReceiver(Enabled = true, Exported = true,
        Name = "com.companyname.moneymap.Services.BootCompletedReceiver")]
    [IntentFilter(new[]
    {
        Intent.ActionBootCompleted,
        Intent.ActionMyPackageReplaced
    })]
    public class BootCompletedReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            // קובע תזמון יומי לשעה 06:00
            AlarmScheduler.ScheduleDaily(context, 6, 0);

            // מריץ סנכרון ראשון מיידית אחרי אתחול/עדכון
            var svc = new Intent(context, typeof(RatesSyncService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(svc);
            else
                context.StartService(svc);
        }
    }
}
