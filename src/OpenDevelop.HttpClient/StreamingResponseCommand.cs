using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class StreamingResponseCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            // This command demonstrates streaming response display
            // Full implementation would parse the current .http file for a streaming request
            MessageService.ShowMessage("Streaming response display: Send a request with Accept: text/event-stream header, then use this command to view the stream in real-time.");
        }
    }

    /// <summary>
    /// Helper to display streaming responses in a WPF window.
    /// </summary>
    public static class StreamingResponseViewer
    {
        public static async Task ShowAsync(string url, CancellationToken ct)
        {
            var window = new Window
            {
                Title = "Streaming Response",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            };

            window.Content = textBox;
            window.Show();

            try
            {
                using var client = new System.Net.Http.HttpClient();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                var buffer = new byte[4096];
                while (!ct.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
                    if (read == 0) break;
                    var text = Encoding.UTF8.GetString(buffer, 0, read);
                    textBox.Dispatcher.Invoke(() => textBox.AppendText(text));
                }
            }
            catch (OperationCanceledException)
            {
                textBox.Dispatcher.Invoke(() => textBox.AppendText("\n[Cancelled]"));
            }
            catch (Exception ex)
            {
                textBox.Dispatcher.Invoke(() => textBox.AppendText($"\n[Error: {ex.Message}]"));
            }
        }
    }
}
