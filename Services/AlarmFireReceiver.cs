using Android.App;
using Android.Content;
using Android.OS;

namespace MoneyMap.Services
    // מופםעל שהעזקה פעולת וקורא לסרוויס 
{
    [BroadcastReceiver(Enabled = true, Exported = false,
        Name = "com.companyname.moneymap.Services.AlarmFireReceiver")]
    public class AlarmFireReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            var svc = new Intent(context, typeof(RatesSyncService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                context.StartForegroundService(svc);
            else
                context.StartService(svc);

            // קובע את התור הבא (למקרה שהמערכת ניכתה את האזעקה)
            AlarmScheduler.ScheduleDaily(context, 6, 0);
        }
    }
}
