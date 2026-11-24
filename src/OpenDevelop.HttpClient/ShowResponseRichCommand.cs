using System;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class ShowResponseRichCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            if (string.IsNullOrEmpty(LastResponseStore.LastResponseBody))
            {
                MessageService.ShowMessage("No response to display. Send a request first.");
                return;
            }

            var window = new Window
            {
                Title = "HTTP Response (Rich View)",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var editor = new TextEditor
            {
                IsReadOnly = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                ShowLineNumbers = true
            };

            var ct = LastResponseStore.LastContentType?.ToLowerInvariant() ?? "";
            if (ct.Contains("json"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JSON");
            else if (ct.Contains("xml") || ct.Contains("html"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            else if (ct.Contains("javascript"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");

            editor.Text = LastResponseStore.LastResponseBody;

            window.Content = editor;
            window.Show();
        }
    }
}
