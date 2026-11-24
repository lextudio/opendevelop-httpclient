using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// OIDC authorization code flow with local redirect (interactive browser login).
    /// </summary>
    public static class OidcAuthorizationCode
    {
        public static async Task<string?> LoginAsync(string authority, string clientId, string scope, int port = 8765)
        {
            var redirectUri = $"http://localhost:{port}/callback";
            var state = Guid.NewGuid().ToString("N");

            var authUrl = $"{authority}/oauth2/v2.0/authorize?" +
                $"client_id={Uri.EscapeDataString(clientId)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&state={state}";

            // Start local HTTP listener for callback
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            // Open browser
            try
            {
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            }
            catch { }

            // Wait for callback
            var context = await listener.GetContextAsync();
            var code = context.Request.QueryString["code"];
            var returnedState = context.Request.QueryString["state"];

            // Send response to browser
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes("<html><body>Login complete. You can close this window.</body></html>");
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();

            listener.Stop();

            if (string.IsNullOrEmpty(code) || returnedState != state)
                return null;

            // Exchange code for token
            var tokenUrl = $"{authority}/oauth2/v2.0/token";
            using var client = new System.Net.Http.HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("grant_type", "authorization_code"),
                new System.Collections.Generic.KeyValuePair<string, string>("client_id", clientId),
                new System.Collections.Generic.KeyValuePair<string, string>("code", code),
                new System.Collections.Generic.KeyValuePair<string, string>("redirect_uri", redirectUri)
            });
            var tokenResponse = await client.PostAsync(tokenUrl, content);
            var body = await tokenResponse.Content.ReadAsStringAsync();
            if (!tokenResponse.IsSuccessStatusCode) return null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("access_token", out var token))
                    return token.GetString();
            }
            catch { }
            return null;
        }
    }
}
