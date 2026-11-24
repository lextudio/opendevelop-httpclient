using System;
using System.Text;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public enum ResponsePreviewOption
    {
        Full,
        HeadersOnly,
        BodyOnly,
        Exchange
    }

    public sealed class PreviewResponseCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            // Show a dialog to choose preview option
            var options = new[] { "Full Response", "Headers Only", "Body Only", "Exchange (Request + Response)" };
            var selected = MessageService.ShowCustomDialog("Preview Response", "Select preview option:", 0, 1, options);

            if (selected < 0 || selected >= options.Length)
                return;

            var option = (ResponsePreviewOption)selected;
            ShowPreview(option);
        }

        static void ShowPreview(ResponsePreviewOption option)
        {
            if (string.IsNullOrEmpty(LastResponseStore.LastResponseBody))
            {
                MessageService.ShowMessage("No response to preview. Send a request first.");
                return;
            }

            var sb = new StringBuilder();
            switch (option)
            {
                case ResponsePreviewOption.Full:
                    sb.AppendLine("=== Full Response ===");
                    sb.AppendLine(LastResponseStore.LastResponseBody);
                    break;
                case ResponsePreviewOption.HeadersOnly:
                    sb.AppendLine("=== Response Headers ===");
                    sb.AppendLine($"Content-Type: {LastResponseStore.LastContentType}");
                    break;
                case ResponsePreviewOption.BodyOnly:
                    sb.AppendLine("=== Response Body ===");
                    sb.AppendLine(LastResponseStore.LastResponseBody);
                    break;
                case ResponsePreviewOption.Exchange:
                    sb.AppendLine("=== HTTP Exchange ===");
                    sb.AppendLine("--- Request ---");
                    sb.AppendLine("(request details not stored)");
                    sb.AppendLine();
                    sb.AppendLine("--- Response ---");
                    sb.AppendLine(LastResponseStore.LastResponseBody);
                    break;
            }

            var content = Encoding.UTF8.GetBytes(sb.ToString());
            SD.FileService.NewFile($"ResponsePreview_{option}.txt", content);
        }
    }
}
