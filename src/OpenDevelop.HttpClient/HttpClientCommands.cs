using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Http.Parsing;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class RegisterServicesCommand : AbstractCommand
    {
        public override void Run()
        {
            // Register any services needed by the HTTP client addin.
            // For now, no special services are required.
        }
    }

    public sealed class SendRequestCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            _ = SendRequestAsync();
        }

        static async Task SendRequestAsync()
        {
            var editor = SD.Workbench.ActiveViewContent as ICSharpCode.SharpDevelop.Editor.ITextEditor;
            if (editor is null)
            {
                MessageService.ShowMessage("No active text editor.");
                return;
            }

            var text = editor.Document.Text;
            var parseResult = HttpRequestParser.Parse(text);
            var syntaxTree = parseResult.SyntaxTree;
            var rootNode = syntaxTree.RootNode;

            // Find the request at the caret position, or use the first request
            var caretOffset = editor.Caret.Offset;
            var requests = new List<HttpRequestNode>();
            foreach (var child in rootNode.ChildNodes)
            {
                if (child is HttpRequestNode req)
                    requests.Add(req);
            }

            if (requests.Count == 0)
            {
                MessageService.ShowMessage("No HTTP request found in the document.");
                return;
            }

            HttpRequestNode? targetRequest = null;
            foreach (var req in requests)
            {
                if (req.Span.Start <= caretOffset && caretOffset <= req.Span.End)
                {
                    targetRequest = req;
                    break;
                }
            }
            targetRequest ??= requests[0];

            // Build binding delegate for variable resolution (environment + system variables).
            // File variables (@var = ...) are resolved internally by TryGetHttpRequestMessage.
            HttpBindingDelegate bind = node =>
            {
                var varName = node.Text.Trim();
                if (varName.StartsWith("$"))
                {
                    return ResolveSystemVariable(varName);
                }
                // Environment variables would be resolved here
                return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
            };

            var result = targetRequest.TryGetHttpRequestMessage(bind);
            if (!result.IsSuccessful)
            {
                var sb = new StringBuilder();
                foreach (var d in result.Diagnostics)
                    sb.AppendLine(d.GetMessage());
                MessageService.ShowMessage("Failed to build request:\n" + sb.ToString());
                return;
            }

            var request = result.Value;
            if (request is null || request.RequestUri is null)
            {
                MessageService.ShowMessage("Invalid request: missing URL.");
                return;
            }

            try
            {
                using var client = new System.Net.Http.HttpClient();
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseText = new StringBuilder();
                responseText.AppendLine($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");
                responseText.AppendLine();
                foreach (var header in response.Headers)
                {
                    responseText.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                foreach (var header in response.Content.Headers)
                {
                    responseText.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                responseText.AppendLine();
                responseText.AppendLine(responseBody);

                // Open response in a new untitled document
                var content = Encoding.UTF8.GetBytes(responseText.ToString());
                SD.FileService.NewFile("Response.txt", content);
            }
            catch (Exception ex)
            {
                MessageService.ShowMessage("Request failed: " + ex.Message);
            }
        }

        static HttpBindingResult<object?> ResolveSystemVariable(string varName)
        {
            var name = varName.TrimStart('$').ToLowerInvariant();
            switch (name)
            {
                case "guid":
                    return HttpBindingResult<object?>.Success(Guid.NewGuid().ToString());
                case "timestamp":
                    return HttpBindingResult<object?>.Success(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                default:
                    return HttpBindingResult<object?>.Failure(Array.Empty<Microsoft.CodeAnalysis.Diagnostic>());
            }
        }
    }
}
