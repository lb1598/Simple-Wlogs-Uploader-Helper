using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace SimpleLogUploader {
    public static class AuthHelper {
        public static string ClientId = string.Empty;
        public static string RedirectUrl = string.Empty;
        private const string AuthUrl = "https://www.warcraftlogs.com/oauth/authorize";
        private const string TokenUrl = "https://www.warcraftlogs.com/oauth/token";

        public static async Task<string> GetAccessToken() {
            string codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Step 1 - Start listening FIRST before opening browser
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8181/callback/");
            listener.Start();

            // Step 2 - Now open the browser
            string authRequest = $"{AuthUrl}?client_id={ClientId}" +
                $"&redirect_uri={RedirectUrl}" +
                $"&response_type=code" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method=S256";

            Process.Start(new ProcessStartInfo(authRequest) { UseShellExecute = true });

            // Step 3 - Wait for callback
            string code = await ListenForCallback(listener);

            // Step 4 - Exchange code for token
            return await ExchangeCodeForToken(code, codeVerifier);
        }

        private static async Task<string> ListenForCallback(HttpListener listener) {
            var context = await listener.GetContextAsync();
            string code = context.Request.QueryString["code"]!;

            var response = context.Response;
            string responseText = "<html><body>Login successful! You can close this tab.</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();
            listener.Stop();

            return code;
        }


        private static async Task<string> ExchangeCodeForToken(string code, string codeVerifier) {
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("redirect_uri", RedirectUrl),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });

            var response = await client.PostAsync(TokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();
            return ParseAndSaveToken(json);
        }

        private static string GenerateCodeVerifier() {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string GenerateCodeChallenge(string codeVerifier) {
            var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static readonly string TokenFilePath =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "WinFormsApp1", "token.json");

        // Call this on app startup
        public static async Task<string> GetOrRefreshToken() {
            var saved = LoadToken();

            if (saved != null && saved.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                return saved.AccessToken; // still valid, reuse it

            if (saved?.RefreshToken != null)
                return await RefreshAccessToken(saved.RefreshToken); // silently refresh

            return await GetAccessToken(); // need full login
        }

        private static async Task<string> RefreshAccessToken(string refreshToken) {
            using var client = new HttpClient();

            var content = new FormUrlEncodedContent(new[] {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

            var response = await client.PostAsync(TokenUrl, content);
            var json = await response.Content.ReadAsStringAsync();
            return ParseAndSaveToken(json);
        }

        private class SavedToken {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }

        private static string ParseAndSaveToken(string json) {
            var parsed = System.Text.Json.JsonDocument.Parse(json);
            var root = parsed.RootElement;

            var token = new SavedToken {
                AccessToken = root.GetProperty("access_token").GetString()!,
                RefreshToken = root.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString()! : string.Empty,
                ExpiresAt = DateTime.UtcNow.AddSeconds(
                    root.GetProperty("expires_in").GetInt32())
            };

            Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath)!);
            File.WriteAllText(TokenFilePath,
                System.Text.Json.JsonSerializer.Serialize(token));

            return token.AccessToken;
        }

        private static SavedToken? LoadToken() {
            if (!File.Exists(TokenFilePath))
                return null;

            var json = File.ReadAllText(TokenFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<SavedToken>(json);
        }
    }
}