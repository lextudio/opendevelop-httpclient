using System;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class OidcAuthCodeLoginCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var authority = Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? "https://login.microsoftonline.com/common";
            var clientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID") ?? "";
            var scope = Environment.GetEnvironmentVariable("OIDC_SCOPE") ?? "openid profile";

            if (string.IsNullOrEmpty(clientId))
            {
                MessageService.ShowMessage("Set OIDC_CLIENT_ID environment variable first.");
                return;
            }

            LoginAsync(authority, clientId, scope);
        }

        static async void LoginAsync(string authority, string clientId, string scope)
        {
            try
            {
                var token = await OidcAuthorizationCode.LoginAsync(authority, clientId, scope);
                if (token is not null)
                {
                    Environment.SetEnvironmentVariable("OIDC_ACCESS_TOKEN", token);
                    MessageService.ShowMessage("OIDC login successful. Token stored in OIDC_ACCESS_TOKEN.");
                }
                else
                {
                    MessageService.ShowMessage("OIDC login failed.");
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowMessage("OIDC error: " + ex.Message);
            }
        }
    }
}
