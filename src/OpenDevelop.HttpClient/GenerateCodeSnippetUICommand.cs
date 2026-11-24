using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class GenerateCodeSnippetUICommand : AbstractMenuCommand
    {
        public override void Run()
        {
            // For now, generate a Python snippet from the last response/request
            // Full implementation would parse the current request
            var window = new Window
            {
                Title = "Generate Code Snippet",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var langCombo = new ComboBox { Margin = new Thickness(5) };
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

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(langCombo, 0);
            Grid.SetRow(textBox, 1);

            grid.Children.Add(langCombo);
            grid.Children.Add(textBox);

            window.Content = grid;
            window.Show();

            // Generate placeholder
            textBox.Text = "# Select a language and send a request first.\n# Then the code snippet will be generated here.";
        }
    }
}
