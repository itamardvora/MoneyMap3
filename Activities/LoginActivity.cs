using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;
using Android.Content;
using MoneyMap.Services;

namespace MoneyMap.Activities
{
    [Activity(
        Name = "com.companyname.moneymap.Activities.LoginActivity",
        Label = "התחברות",
        Theme = "@style/AppTheme",
        Exported = true)]
    public class LoginActivity : Activity
    {
        private EditText _emailInput;
        private EditText _passwordInput;
        private CheckBox _rememberMeCheckBox;
        private Button _loginButton;
        private Button _goToRegisterButton;

        private bool _isAutoLoginRunning = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_login);

            _emailInput = FindViewById<EditText>(Resource.Id.emailInput);
            _passwordInput = FindViewById<EditText>(Resource.Id.passwordInput);
            _rememberMeCheckBox = FindViewById<CheckBox>(Resource.Id.rememberMeCheckBox);
            _loginButton = FindViewById<Button>(Resource.Id.loginButton);
            _goToRegisterButton = FindViewById<Button>(Resource.Id.goToRegisterButton);

            if (_emailInput == null ||
                _passwordInput == null ||
                _rememberMeCheckBox == null ||
                _loginButton == null ||
                _goToRegisterButton == null)
            {
                Toast.MakeText(this, "שגיאה במסך התחברות: חסר רכיב XML", ToastLength.Long).Show();
                return;
            }

            _goToRegisterButton.Click += (s, e) =>
            {
                if (_isAutoLoginRunning)
                {
                    Toast.MakeText(this, "מתבצעת התחברות אוטומטית, נסה שוב בעוד רגע", ToastLength.Short).Show();
                    return;
                }

                try
                {
                    StartActivity(typeof(RegisterActivity));
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("GO_REGISTER_ERROR", ex.ToString());
                    Toast.MakeText(this, "שגיאה במעבר להרשמה: " + ex.Message, ToastLength.Long).Show();
                }
            };

            _loginButton.Click += async (s, e) =>
            {
                if (_isAutoLoginRunning)
                {
                    Toast.MakeText(this, "מתבצעת התחברות אוטומטית, נסה שוב בעוד רגע", ToastLength.Short).Show();
                    return;
                }

                await LoginAsync();
            };

            _ = TryAutoLoginAsync();
        }

        private async Task TryAutoLoginAsync()
        {
            bool rememberMe = Preferences.Get("RememberMe", false);

            if (!rememberMe)
                return;

            int savedUserId = Preferences.Get("LoggedInUserId", 0);
            string savedUserName = Preferences.Get("LoggedInUserName", "");
            string savedFirebaseUid = Preferences.Get("FirebaseUid", "");

            if (savedUserId <= 0 || string.IsNullOrWhiteSpace(savedFirebaseUid))
                return;

            try
            {
                _isAutoLoginRunning = true;

                _loginButton.Enabled = false;
                _goToRegisterButton.Enabled = false;
                _loginButton.Text = "מתחבר אוטומטית...";

                if (!App.IsCoreReady())
                    await App.InitForAuthAsync();

                FireBaseHelper.InitializeFirebase(this);

                UserSession.SetUser(savedUserId, savedUserName, savedFirebaseUid);

                await App.InitAfterLoginAsync();

                StartActivity(typeof(HomeActivity));
                Finish();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("AUTO_LOGIN_ERROR", ex.ToString());

                Preferences.Set("RememberMe", false);
                Preferences.Set("IsLoggedIn", false);
                Preferences.Set("LoggedInUserId", 0);
                Preferences.Set("LoggedInUserName", "");
                Preferences.Set("FirebaseUid", "");

                UserSession.Clear();

                Toast.MakeText(this, "התחברות אוטומטית נכשלה, התחבר מחדש", ToastLength.Long).Show();
            }
            finally
            {
                _isAutoLoginRunning = false;

                if (!IsFinishing)
                {
                    _loginButton.Enabled = true;
                    _goToRegisterButton.Enabled = true;
                    _loginButton.Text = "כניסה";
                }
            }
        }

        private async Task LoginAsync()
        {
            var email = (_emailInput?.Text ?? "").Trim().ToLowerInvariant();
            var password = _passwordInput?.Text ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                Toast.MakeText(this, "אנא מלא אימייל", ToastLength.Short).Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Toast.MakeText(this, "אנא מלא סיסמה", ToastLength.Short).Show();
                return;
            }

            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Toast.MakeText(this, "אין חיבור לאינטרנט", ToastLength.Long).Show();
                return;
            }

            try
            {
                _loginButton.Enabled = false;
                _goToRegisterButton.Enabled = false;
                _loginButton.Text = "מתחבר...";

                if (!App.IsCoreReady())
                    await App.InitForAuthAsync();

                FireBaseHelper.InitializeFirebase(this);

                if (App.UserService.IsAdminCredentials(email, password))
                {
                    App.UserService.SignInAsAdmin();

                    Preferences.Set("RememberMe", _rememberMeCheckBox.Checked);
                    Preferences.Set("IsLoggedIn", true);

                    Preferences.Set("LoggedInUserId", 0);
                    Preferences.Set("LoggedInUserName", "Admin");
                    Preferences.Set("FirebaseUid", "");

                    StartActivity(typeof(AdminActivity));
                    Finish();
                    return;
                }

                var firebaseUid = await FireBaseHelper.SignInUserAsync(email, password);

                var user = await FireBaseHelper.GetUserByIdAsync(firebaseUid);

                if (user == null)
                {
                    Toast.MakeText(this, "המשתמש התחבר, אבל לא נמצא ב-Firestore", ToastLength.Long).Show();
                    return;
                }

                if (user.UserID <= 0)
                {
                    Toast.MakeText(this, "שגיאה: מזהה משתמש לא תקין", ToastLength.Long).Show();
                    return;
                }

                UserSession.SetUser(user.UserID, user.FullName, firebaseUid);

                Preferences.Set("RememberMe", _rememberMeCheckBox.Checked);
                Preferences.Set("IsLoggedIn", true);
                Preferences.Set("LoggedInUserId", user.UserID);
                Preferences.Set("LoggedInUserName", user.FullName ?? "");
                Preferences.Set("FirebaseUid", firebaseUid ?? "");

                await App.InitAfterLoginAsync();

                AlarmScheduler.ScheduleDaily(this, 6, 0);

                var serviceIntent = new Intent(this, typeof(RatesSyncService));

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    StartForegroundService(serviceIntent);
                else
                    StartService(serviceIntent);

                StartActivity(typeof(HomeActivity));
                Finish();
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("LOGIN_ERROR", ex.ToString());

                Toast.MakeText(
                    this,
                    "שגיאה בהתחברות: " + ex.GetType().Name + " - " + ex.Message,
                    ToastLength.Long
                ).Show();
            }
            finally
            {
                if (!IsFinishing)
                {
                    _loginButton.Enabled = true;
                    _goToRegisterButton.Enabled = true;
                    _loginButton.Text = "כניסה";
                }
            }
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}