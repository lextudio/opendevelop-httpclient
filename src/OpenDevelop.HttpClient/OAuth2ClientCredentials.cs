using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// OAuth2 client credentials flow.
    /// </summary>
    public static class OAuth2ClientCredentials
    {
        public static async Task<string?> GetAccessTokenAsync(
            string tokenUrl, string clientId, string clientSecret, string scope)
        {
            using var client = new System.Net.Http.HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", scope)
            });
            request.Content = content;

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) return null;

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("access_token", out var token))
                    return token.GetString();
            }
            catch { }
            return null;
        }
    }
}
