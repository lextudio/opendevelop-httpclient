using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Microsoft Identity Platform (Azure AD v2) token acquisition.
    /// Supports client credentials flow.
    /// </summary>
    public static class AzureAdV2
    {
        public static async Task<string?> GetAccessTokenAsync(
            string tenantId, string clientId, string clientSecret, string scope)
        {
            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            using var client = new System.Net.Http.HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            var content = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "client_credentials"),
                new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                new System.Collections.Generic.KeyValuePair<string, string>("client_secret", clientSecret),
                new System.Collections.Generic.KeyValuePair<string, string>("scope", scope)
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
