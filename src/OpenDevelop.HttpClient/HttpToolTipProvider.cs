using System;
using System.Text.RegularExpressions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;

namespace OpenDevelop.HttpClient
{
    public class HttpToolTipProvider : ITextAreaToolTipProvider
    {
        static readonly Regex VariableRegex = new Regex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

        public void HandleToolTipRequest(ToolTipRequestEventArgs e)
        {
            if (!e.InDocument)
                return;

            var editor = e.Editor;
            var offset = editor.Document.GetOffset(e.LogicalPosition);
            var text = editor.Document.Text;

            // Check if we're hovering over a variable
            var line = editor.Document.GetLineByOffset(offset);
            var lineText = editor.Document.GetText(line);

            // Find variable at offset
            foreach (Match m in VariableRegex.Matches(lineText))
            {
                var varStart = line.Offset + m.Index;
                var varEnd = varStart + m.Length;
                if (offset >= varStart && offset <= varEnd)
                {
                    var varName = m.Groups[1].Value.Trim();
                    var value = ResolveVariableForTooltip(varName);
                    if (value is not null)
                    {
                        e.SetToolTip(new System.Windows.Controls.ToolTip { Content = varName + " = " + value });
                    }
                    return;
                }
            }
        }

        static string? ResolveVariableForTooltip(string varName)
        {
            if (varName.StartsWith("$"))
            {
                var name = varName.TrimStart('$').ToLowerInvariant();
                switch (name)
                {
                    case "guid": return Guid.NewGuid().ToString();
                    case "timestamp": return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                    case "uuid": return Guid.NewGuid().ToString();
                    default: return null;
                }
            }

            var envValue = HttpEnvironmentManager.ResolveVariable(varName);
            if (envValue is not null)
                return envValue;

            return null;
        }
    }
}
