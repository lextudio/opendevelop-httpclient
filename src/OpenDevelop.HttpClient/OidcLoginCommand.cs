using System;
using System.Threading;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class OidcLoginCommand : AbstractMenuCommand
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
                var (deviceCode, userCode, verificationUri, interval) =
                    await OidcDeviceCode.RequestDeviceCodeAsync(authority, clientId, scope);

                if (string.IsNullOrEmpty(deviceCode) || string.IsNullOrEmpty(userCode))
                {
                    MessageService.ShowMessage("Failed to request device code.");
                    return;
                }

                MessageService.ShowMessage($"Go to {verificationUri} and enter code: {userCode}");

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));
                var token = await OidcDeviceCode.PollForTokenAsync(authority, clientId, deviceCode!, interval, cts.Token);

                if (token is not null)
                {
                    Environment.SetEnvironmentVariable("OIDC_ACCESS_TOKEN", token);
                    MessageService.ShowMessage("OIDC login successful. Token stored in OIDC_ACCESS_TOKEN.");
                }
                else
                {
                    MessageService.ShowMessage("OIDC login failed or timed out.");
                }
            }
            catch (Exception ex)
            {
                MessageService.ShowMessage("OIDC error: " + ex.Message);
            }
        }
    }
}
