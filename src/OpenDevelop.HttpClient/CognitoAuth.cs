using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// AWS Cognito authentication (USER_PASSWORD_AUTH flow).
    /// </summary>
    public static class CognitoAuth
    {
        public static async Task<string?> GetAccessTokenAsync(
            string username, string password, string region, string userPoolId, string clientId)
        {
            var url = $"https://cognito-idp.{region}.amazonaws.com/";
            var body = new
            {
                AuthFlow = "USER_PASSWORD_AUTH",
                ClientId = clientId,
                AuthParameters = new
                {
                    USERNAME = username,
                    PASSWORD = password
                }
            };

            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");
            client.DefaultRequestHeaders.Add("Content-Type", "application/x-amz-json-1.1");

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/x-amz-json-1.1");
            var response = await client.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("AuthenticationResult", out var result) &&
                    result.TryGetProperty("AccessToken", out var token))
                {
                    return token.GetString();
                }
            }
            catch { }
            return null;
        }
    }
}
