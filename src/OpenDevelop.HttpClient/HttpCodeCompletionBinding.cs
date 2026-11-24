using System;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.CodeCompletion;

namespace OpenDevelop.HttpClient
{
    public class HttpCodeCompletionBinding : ICodeCompletionBinding
    {
        static readonly string[] HttpMethods = { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE" };

        static readonly string[] CommonHeaders = {
            "Accept", "Accept-Encoding", "Accept-Language", "Authorization", "Cache-Control",
            "Content-Type", "Content-Length", "Content-Encoding", "Cookie", "Host",
            "User-Agent", "X-Requested-With", "X-Api-Key", "X-Auth-Token"
        };

        static readonly string[] SystemVariables = {
            "{{$guid}}", "{{$timestamp}}", "{{$datetime}}", "{{$randomInt}}",
            "{{$processEnv}}", "{{$dotenv}}", "{{$localDatetime}}"
        };

        static readonly string[] ContentTypes = {
            "application/json", "application/xml", "application/x-www-form-urlencoded",
            "multipart/form-data", "text/plain", "text/html", "text/xml"
        };

        public CodeCompletionKeyPressResult HandleKeyPress(ITextEditor editor, char ch)
        {
            return CodeCompletionKeyPressResult.None;
        }

        public bool HandleKeyPressed(ITextEditor editor, char ch)
        {
            return false;
        }

        public bool CtrlSpace(ITextEditor editor)
        {
            var items = new DefaultCompletionItemList();
            var offset = editor.Caret.Offset;

            var line = editor.Document.GetLineByOffset(offset);
            var lineText = editor.Document.GetText(line.Offset, offset - line.Offset).TrimStart();

            bool atMethodPosition = string.IsNullOrEmpty(lineText) ||
                HttpMethods.Any(m => m.StartsWith(lineText, StringComparison.OrdinalIgnoreCase));

            if (atMethodPosition)
            {
                foreach (var m in HttpMethods)
                    items.Items.Add(new DefaultCompletionItem(m) { Description = "HTTP method" });
            }

            foreach (var h in CommonHeaders)
                items.Items.Add(new DefaultCompletionItem(h) { Description = "HTTP header" });

            foreach (var v in SystemVariables)
                items.Items.Add(new DefaultCompletionItem(v) { Description = "System variable" });

            if (lineText.StartsWith("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var ct in ContentTypes)
                    items.Items.Add(new DefaultCompletionItem(ct) { Description = "MIME type" });
            }

            foreach (var env in HttpEnvironmentManager.Environments)
            {
                foreach (var kv in env.Variables)
                {
                    items.Items.Add(new DefaultCompletionItem("{{" + kv.Key + "}}") { Description = $"Environment variable ({env.Name})" });
                }
            }

            if (items.Items.Count > 0)
            {
                items.SortItems();
                var window = editor.ShowCompletionWindow(items);
                return window != null;
            }
            return false;
        }
    }
}
