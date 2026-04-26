using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoneyMap.Services
{
    public sealed class FirebaseAuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public string FirebaseUid { get; set; }
        public string Email { get; set; }

        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresInSeconds { get; set; }
    }

    public class FirebaseAuthService
    {
        private static readonly HttpClient _http = new HttpClient();

        public async Task<FirebaseAuthResult> RegisterWithEmailPasswordAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseConfig.ApiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            return await SendJsonRequestAsync(url, payload);
        }

        public async Task<FirebaseAuthResult> SignInWithEmailPasswordAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseConfig.ApiKey}";

            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            return await SendJsonRequestAsync(url, payload);
        }

        public async Task<FirebaseAuthResult> RefreshIdTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new FirebaseAuthResult
                {
                    Success = false,
                    ErrorMessage = "Missing refresh token"
                };
            }

            var url = $"https://securetoken.googleapis.com/v1/token?key={FirebaseConfig.ApiKey}";

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            });

            var response = await _http.PostAsync(url, form);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new FirebaseAuthResult
                {
                    Success = false,
                    ErrorMessage = ExtractFirebaseError(body)
                };
            }

            var json = JObject.Parse(body);

            return new FirebaseAuthResult
            {
                Success = true,
                FirebaseUid = json["user_id"]?.ToString(),
                IdToken = json["id_token"]?.ToString(),
                RefreshToken = json["refresh_token"]?.ToString(),
                ExpiresInSeconds = SafeToInt(json["expires_in"]?.ToString())
            };
        }

        private async Task<FirebaseAuthResult> SendJsonRequestAsync(string url, object payload)
        {
            var jsonText = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonText, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new FirebaseAuthResult
                {
                    Success = false,
                    ErrorMessage = ExtractFirebaseError(body)
                };
            }

            var json = JObject.Parse(body);

            return new FirebaseAuthResult
            {
                Success = true,
                FirebaseUid = json["localId"]?.ToString(),
                Email = json["email"]?.ToString(),
                IdToken = json["idToken"]?.ToString(),
                RefreshToken = json["refreshToken"]?.ToString(),
                ExpiresInSeconds = SafeToInt(json["expiresIn"]?.ToString())
            };
        }

        private static int SafeToInt(string value)
        {
            if (int.TryParse(value, out var n))
                return n;

            return 3600;
        }

        private static string ExtractFirebaseError(string rawBody)
        {
            try
            {
                var json = JObject.Parse(rawBody);
                var code = json["error"]?["message"]?.ToString() ?? "";

                if (code.StartsWith("WEAK_PASSWORD"))
                    return "הסיסמה חלשה מדי. מינימום 6 תווים.";

                switch (code)
                {
                    case "EMAIL_EXISTS":
                        return "האימייל כבר קיים";
                    case "EMAIL_NOT_FOUND":
                        return "האימייל לא נמצא";
                    case "INVALID_PASSWORD":
                        return "סיסמה שגויה";
                    case "USER_DISABLED":
                        return "המשתמש נחסם";
                    case "OPERATION_NOT_ALLOWED":
                        return "Email/Password לא הופעל ב-Firebase";
                    case "TOO_MANY_ATTEMPTS_TRY_LATER":
                        return "יותר מדי ניסיונות. נסה שוב מאוחר יותר.";
                    default:
                        return string.IsNullOrWhiteSpace(code) ? "שגיאה לא ידועה מול Firebase" : code;
                }
            }
            catch
            {
                return "שגיאה לא ידועה מול Firebase";
            }
        }
    }
}