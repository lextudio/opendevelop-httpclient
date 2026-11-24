using System;
using System.Text.Json;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Editor;

namespace OpenDevelop.HttpClient
{
    public sealed class FormatRequestBodyCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var editor = SD.Workbench.ActiveViewContent as ITextEditor;
            if (editor is null)
            {
                MessageService.ShowMessage("No active text editor.");
                return;
            }

            var text = editor.Document.Text;
            // Try to format JSON bodies
            // Simple approach: find JSON blocks and format them
            var lines = text.Split('\n');
            var result = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(trimmed);
                        var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                        result.AppendLine(formatted);
                    }
                    catch
                    {
                        result.AppendLine(line);
                    }
                }
                else
                {
                    result.AppendLine(line);
                }
            }
            editor.Document.Text = result.ToString();
        }
    }
}
