// MoneyMap/Services/RatesSyncService.cs
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Xamarin.Essentials;

namespace MoneyMap.Services
{
    [Service(Exported = false, Enabled = true, Name = "com.companyname.moneymap.Services.RatesSyncService")]
    public sealed class RatesSyncService : Service
    {
        const string CHANNEL_ID = "sync_channel"; // 
        const int NOTIF_ID = 88017;  // מזהה של ההתראה
        bool _running; // מניעת הפעלה כפולה

        // חייב לממש את הפעולה אבל אצלי לא בשימוש אז הוא נל
        public override IBinder OnBind(Intent intent) => null;


        // מופעל כאשר Android מפעיל את השירות 
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (_running) return StartCommandResult.NotSticky;
            _running = true; // מסמן שהשירות התחיל לעבוד


            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                EnsureChannel();
                var n = new NotificationCompat.Builder(this, CHANNEL_ID) // יוצר התראה שתוצג בזמן הסנכרון
                    .SetContentTitle("סנכרון נתונים")
                    .SetContentText("מעדכן שערי מטבע ומחירי מניות…")
                    .SetSmallIcon(Resource.Mipmap.ic_launcher) // תוודא שיש אייקון
                    .SetOngoing(true)
                    .Build();
                StartForeground(NOTIF_ID, n);
            }

            Task.Run(RunAsync);
            return StartCommandResult.NotSticky;
        }

        private async Task RunAsync() // מבצע את כל תהליך עדכון הנתונים
        {
            try
            {
                var uid = Preferences.Get("LoggedInUserId", 0);
                if (uid <= 0) return;

                if (!App.IsCoreReady())
                    await App.InitForAuthAsync();
                if (!App.IsFullyReady())
                    await App.InitAfterLoginAsync();

                // שערי מטבע
                if (App.CurrencyService != null)
                {
                    try { await App.CurrencyService.EnsureBaseRatesAsync(); } catch { }
                }

                // מחירי מניות חימום קאש לפי הסמלים אצל המשתמש
                if (App.InvestmentService != null && App.StockPriceService != null)
                {
                    try
                    {
                        var inv = await App.InvestmentService.GetInvestmentsForUserAsync(uid);
                        var symbols = inv.Select(i => (i?.StockSymbol ?? "").Trim().ToUpperInvariant())
                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                         .Distinct()
                                         .ToList();

                        foreach (var s in symbols)
                        {
                            try { await App.StockPriceService.GetPriceOrFetch(s); } catch { }
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    StopForeground(true);

                StopSelf();
                _running = false;
            }
        }

        void EnsureChannel() // יוצר ערוץ התראות במערכת Android
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
            var mgr = (NotificationManager)GetSystemService(NotificationService);
            var ch = new NotificationChannel(CHANNEL_ID, "Sync", NotificationImportance.Low)
            {
                Description = "עדכון יומי של שערי מטבע ומניות"
            };
            mgr.CreateNotificationChannel(ch);
        }
    }
}
