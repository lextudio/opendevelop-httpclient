using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// OIDC device code flow for interactive authentication.
    /// </summary>
    public static class OidcDeviceCode
    {
        public static async Task<(string? DeviceCode, string? UserCode, string? VerificationUri, int Interval)> RequestDeviceCodeAsync(
            string authority, string clientId, string scope)
        {
            var url = $"{authority}/oauth2/v2.0/devicecode";
            using var client = new System.Net.Http.HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                new System.Collections.Generic.KeyValuePair<string, string>("scope", scope)
            });
            var response = await client.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return (null, null, null, 5);

            try
            {
                using var doc = JsonDocument.Parse(body);
                var deviceCode = doc.RootElement.TryGetProperty("device_code", out var dc) ? dc.GetString() : null;
                var userCode = doc.RootElement.TryGetProperty("user_code", out var uc) ? uc.GetString() : null;
                var verificationUri = doc.RootElement.TryGetProperty("verification_uri", out var vu) ? vu.GetString() : null;
                var interval = doc.RootElement.TryGetProperty("interval", out var iv) ? iv.GetInt32() : 5;
                return (deviceCode, userCode, verificationUri, interval);
            }
            catch { }
            return (null, null, null, 5);
        }

        public static async Task<string?> PollForTokenAsync(
            string authority, string clientId, string deviceCode, int interval, CancellationToken ct)
        {
            var url = $"{authority}/oauth2/v2.0/token";
            var deadline = DateTime.UtcNow.AddMinutes(15);

            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), ct);

                using var client = new System.Net.Http.HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                    new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                    new System.Collections.Generic.KeyValuePair<string, string>("device_code", deviceCode)
                });
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("access_token", out var token))
                        return token.GetString();

                    if (doc.RootElement.TryGetProperty("error", out var error))
                    {
                        var err = error.GetString();
                        if (err == "authorization_pending")
                            continue;
                        if (err == "slow_down")
                        {
                            interval += 5;
                            continue;
                        }
                        // Other errors: fail
                        return null;
                    }
                }
                catch { }
            }
            return null;
        }
    }
}
