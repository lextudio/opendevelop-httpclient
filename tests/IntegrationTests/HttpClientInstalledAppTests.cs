using OpenDevelop.IntegrationTests;
using System.Text.Json;
using Xunit;

namespace OpenDevelopHttpClient.IntegrationTests;

[Collection(OpenDevelopInstalledAppCollection.Name)]
public sealed class HttpClientInstalledAppTests
{
    readonly OpenDevelopAppFixture app;

    public HttpClientInstalledAppTests(OpenDevelopAppFixture app) => this.app = app;

    [Fact]
    public async Task HttpClientAddin_LoadsSuccessfully()
    {
        // Create a .http file
        var httpFile = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid().ToString("N") + ".http");
        await File.WriteAllTextAsync(httpFile, """
            GET https://httpbin.org/get HTTP/1.1
            Accept: application/json
            """);

        try
        {
            // Open the file in OpenDevelop
            var opened = await app.InvokeAsync("od.open-file", httpFile);
            Assert.True(opened.TryGetProperty("opened", out var openedProp) && openedProp.GetBoolean(),
                "Failed to open .http file: " + opened.ToString());

            // Verify the file is opened
            var active = await app.InvokeAsync("od.active-view");
            Assert.Equal(httpFile, active.GetProperty("fileName").GetString());
        }
        finally
        {
            File.Delete(httpFile);
        }
    }

    [Fact]
    public async Task HttpClientAddin_SyntaxHighlightingRegistered()
    {
        // Verify that .http files are recognized
        var httpFile = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid().ToString("N") + ".http");
        await File.WriteAllTextAsync(httpFile, "GET https://example.com/api HTTP/1.1");

        try
        {
            var opened = await app.InvokeAsync("od.open-file", httpFile);
            Assert.True(opened.TryGetProperty("opened", out var openedProp) && openedProp.GetBoolean(),
                "Failed to open .http file: " + opened.ToString());
        }
        finally
        {
            File.Delete(httpFile);
        }
    }
}
