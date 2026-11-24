using System;
using System.Text;
using System.Text.Json;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Editor;

namespace OpenDevelop.HttpClient
{
    public sealed class IndentRequestBodyCommand : AbstractMenuCommand
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
            var lines = text.Split('\n');
            var result = new StringBuilder();
            var inBody = false;
            var bodyBuffer = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Detect start of body (after blank line following headers)
                if (string.IsNullOrEmpty(trimmed) && !inBody)
                {
                    // Flush any pending body
                    if (bodyBuffer.Length > 0)
                    {
                        result.Append(FormatBody(bodyBuffer.ToString()));
                        bodyBuffer.Clear();
                    }
                    inBody = true;
                    result.AppendLine(line);
                    continue;
                }

                if (inBody && !string.IsNullOrEmpty(trimmed))
                {
                    // Check if next request separator
                    if (trimmed.StartsWith("###"))
                    {
                        if (bodyBuffer.Length > 0)
                        {
                            result.Append(FormatBody(bodyBuffer.ToString()));
                            bodyBuffer.Clear();
                        }
                        inBody = false;
                        result.AppendLine(line);
                        continue;
                    }
                    bodyBuffer.AppendLine(line);
                }
                else
                {
                    if (bodyBuffer.Length > 0 && inBody)
                    {
                        result.Append(FormatBody(bodyBuffer.ToString()));
                        bodyBuffer.Clear();
                    }
                    inBody = false;
                    result.AppendLine(line);
                }
            }

            // Flush remaining body
            if (bodyBuffer.Length > 0)
            {
                result.Append(FormatBody(bodyBuffer.ToString()));
            }

            editor.Document.Text = result.ToString();
        }

        static string FormatBody(string body)
        {
            var trimmed = body.Trim();
            if (string.IsNullOrEmpty(trimmed)) return body;

            // Try JSON
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true }) + "\n";
            }
            catch { }

            // Try XML (basic)
            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(trimmed);
                using var sw = new System.IO.StringWriter();
                using var xw = new System.Xml.XmlTextWriter(sw) { Formatting = System.Xml.Formatting.Indented };
                xmlDoc.WriteTo(xw);
                return sw.ToString() + "\n";
            }
            catch { }

            return body;
        }
    }
}
