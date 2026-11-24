using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class ResponseViewerWindowCommand : AbstractMenuCommand
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
                Title = "HTTP Response Viewer",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Toolbar
            var toolbar = new ToolBar();
            var fullBtn = new Button { Content = "Full", Margin = new Thickness(2) };
            var headersBtn = new Button { Content = "Headers Only", Margin = new Thickness(2) };
            var bodyBtn = new Button { Content = "Body Only", Margin = new Thickness(2) };
            var exchangeBtn = new Button { Content = "Exchange", Margin = new Thickness(2) };
            var fontSizeLabel = new Label { Content = "Font Size:", Margin = new Thickness(10, 0, 0, 0) };
            var fontSizeCombo = new ComboBox { Width = 60, Margin = new Thickness(2) };
            foreach (var size in new[] { "10", "12", "14", "16", "18" })
                fontSizeCombo.Items.Add(size);
            fontSizeCombo.SelectedIndex = 1; // 12

            toolbar.Items.Add(fullBtn);
            toolbar.Items.Add(headersBtn);
            toolbar.Items.Add(bodyBtn);
            toolbar.Items.Add(exchangeBtn);
            toolbar.Items.Add(fontSizeLabel);
            toolbar.Items.Add(fontSizeCombo);

            // AvalonEdit editor
            var editor = new TextEditor
            {
                IsReadOnly = true,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                ShowLineNumbers = true,
                WordWrap = true
            };

            var ct = LastResponseStore.LastContentType?.ToLowerInvariant() ?? "";
            if (ct.Contains("json"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JSON");
            else if (ct.Contains("xml") || ct.Contains("html"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            else if (ct.Contains("javascript"))
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript");

            // Full response text
            var fullText = LastResponseStore.LastResponseBody;

            Action<string> updateText = (mode) =>
            {
                switch (mode)
                {
                    case "full":
                        editor.Text = fullText;
                        break;
                    case "headers":
                        editor.Text = $"Content-Type: {LastResponseStore.LastContentType}\n";
                        break;
                    case "body":
                        editor.Text = LastResponseStore.LastResponseBody;
                        break;
                    case "exchange":
                        editor.Text = $"=== HTTP Exchange ===\n\n--- Response ---\n{LastResponseStore.LastResponseBody}";
                        break;
                }
            };

            fullBtn.Click += (s, e) => updateText("full");
            headersBtn.Click += (s, e) => updateText("headers");
            bodyBtn.Click += (s, e) => updateText("body");
            exchangeBtn.Click += (s, e) => updateText("exchange");
            fontSizeCombo.SelectionChanged += (s, e) =>
            {
                if (fontSizeCombo.SelectedItem is string sizeStr && double.TryParse(sizeStr, out var size))
                    editor.FontSize = size;
            };

            updateText("full");

            Grid.SetRow(toolbar, 0);
            Grid.SetRow(editor, 1);

            grid.Children.Add(toolbar);
            grid.Children.Add(editor);

            window.Content = grid;
            window.Show();
        }
    }
}
