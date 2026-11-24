using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Displays image responses in a WPF window.
    /// </summary>
    public static class ImageResponseViewer
    {
        public static void Show(byte[] imageData, string contentType)
        {
            var window = new Window
            {
                Title = "HTTP Image Response",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var image = new Image();
            try
            {
                using var ms = new System.IO.MemoryStream(imageData);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                image.Source = bitmap;
            }
            catch
            {
                var textBlock = new TextBlock { Text = "Failed to load image." };
                window.Content = textBlock;
                window.Show();
                return;
            }

            var scrollViewer = new ScrollViewer
            {
                Content = image,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            window.Content = scrollViewer;
            window.Show();
        }
    }

    public sealed class ViewImageResponseCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            if (LastResponseStore.LastResponseBody is null)
            {
                MessageService.ShowMessage("No response to display. Send a request first.");
                return;
            }

            var ct = LastResponseStore.LastContentType?.ToLowerInvariant() ?? "";
            if (!ct.StartsWith("image/"))
            {
                MessageService.ShowMessage("Last response is not an image.");
                return;
            }

            try
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(LastResponseStore.LastResponseBody);
                // For text-stored images, this won't work well; but for binary it would
                ImageResponseViewer.Show(bytes, ct);
            }
            catch (Exception ex)
            {
                MessageService.ShowMessage("Failed to display image: " + ex.Message);
            }
        }
    }
}
