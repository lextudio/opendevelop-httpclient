using System;
using System.Net.Http;
using Microsoft.DotNet.Interactive.Http.Parsing;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class GenerateCodeSnippetFullCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var editor = SD.Workbench.ActiveViewContent as ICSharpCode.SharpDevelop.Editor.ITextEditor;
            if (editor is null)
            {
                MessageService.ShowMessage("No active text editor.");
                return;
            }

            var text = editor.Document.Text;
            var parseResult = HttpRequestParser.Parse(text);
            var requests = new System.Collections.Generic.List<HttpRequestNode>();
            foreach (var child in parseResult.SyntaxTree.RootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }

            if (requests.Count == 0)
            {
                MessageService.ShowMessage("No HTTP request found.");
                return;
            }

            var targetRequest = requests[0];
            foreach (var req in requests)
            {
                if (req.Span.Start <= editor.Caret.Offset && editor.Caret.Offset <= req.Span.End)
                {
                    targetRequest = req;
                    break;
                }
            }

            // Build request
            HttpBindingDelegate bind = node =>
            {
                var varName = node.Text.Trim();
                if (varName.StartsWith("$"))
                    return SendRequestCommand.ResolveSystemVariable(varName);
                var envValue = HttpEnvironmentManager.ResolveVariable(varName);
                if (envValue is not null)
                    return HttpBindingResult<object?>.Success(envValue);
                return HttpBindingResult<object?>.Failure(Array.Empty<Microsoft.CodeAnalysis.Diagnostic>());
            };

            var result = targetRequest.TryGetHttpRequestMessage(bind);
            if (!result.IsSuccessful || result.Value is null)
            {
                MessageService.ShowMessage("Failed to build request.");
                return;
            }

            var request = result.Value;

            var window = new Window
            {
                Title = "Generate Code Snippet",
                Width = 700,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var langCombo = new ComboBox { Margin = new Thickness(5), Width = 150 };
            langCombo.Items.Add("Python");
            langCombo.Items.Add("JavaScript");
            langCombo.Items.Add("C#");
            langCombo.Items.Add("cURL");
            langCombo.SelectedIndex = 0;

            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(5)
            };

            Action updateSnippet = () =>
            {
                var lang = langCombo.SelectedItem?.ToString() ?? "Python";
                string snippet = lang switch
                {
                    "Python" => HttpCodeSnippetGenerator.GeneratePython(request),
                    "JavaScript" => HttpCodeSnippetGenerator.GenerateJavaScript(request),
                    "C#" => HttpCodeSnippetGenerator.GenerateCSharp(request),
                    "cURL" => HttpCodeSnippetGenerator.GenerateCurl(request),
                    _ => ""
                };
                textBox.Text = snippet;
            };

            langCombo.SelectionChanged += (s, e) => updateSnippet();
            updateSnippet();

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(langCombo, 0);
            Grid.SetRow(textBox, 1);

            grid.Children.Add(langCombo);
            grid.Children.Add(textBox);

            window.Content = grid;
            window.Show();
        }
    }
}
