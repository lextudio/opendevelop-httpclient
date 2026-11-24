using System;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class ShowHistoryPadCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var window = new Window
            {
                Title = "HTTP Request History",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            var textBox = new TextBox
            {
                Text = HttpRequestHistory.GetHistory(),
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
            var refreshBtn = new Button { Content = "Refresh", Margin = new Thickness(5, 0, 5, 0) };
            var clearBtn = new Button { Content = "Clear", Margin = new Thickness(5, 0, 5, 0) };

            refreshBtn.Click += (s, e) => textBox.Text = HttpRequestHistory.GetHistory();
            clearBtn.Click += (s, e) =>
            {
                HttpRequestHistory.Clear();
                textBox.Text = "";
            };

            buttonPanel.Children.Add(refreshBtn);
            buttonPanel.Children.Add(clearBtn);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(buttonPanel, 0);
            Grid.SetRow(textBox, 1);

            grid.Children.Add(buttonPanel);
            grid.Children.Add(textBox);

            window.Content = grid;
            window.Show();
        }
    }
}
