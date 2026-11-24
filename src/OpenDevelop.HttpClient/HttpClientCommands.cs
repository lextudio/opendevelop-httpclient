using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private static System.Threading.CancellationTokenSource? _cts;

        public override void Run()
        {
            if (_cts is not null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _cts = null;
                return;
            }
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

            // Load environment from the file's directory
            var filePath = editor.FileName;
            if (!string.IsNullOrEmpty(filePath))
            {
                var dir = System.IO.Path.GetDirectoryName(filePath);
                if (dir is not null)
                    HttpEnvironmentManager.LoadFromWorkspace(dir);
            }
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

            // Find prompt variables for this request
            var promptVars = HttpPromptVariableResolver.FindPromptVariables(text, targetRequest.Span.Start);
            var promptValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, desc) in promptVars)
            {
                var value = HttpPromptVariableResolver.PromptForValue(name, desc);
                if (value is null)
                    return; // User cancelled
                promptValues[name] = value;
            }

            // Build binding delegate for variable resolution
            HttpBindingDelegate bind = node =>
            {
                var varName = node.Text.Trim();
                if (varName.StartsWith("$"))
                {
                    return ResolveSystemVariable(varName);
                }

                // Prompt variables (highest precedence)
                if (promptValues.TryGetValue(varName, out var promptValue))
                {
                    return HttpBindingResult<object?>.Success(promptValue);
                }

                // Request variables
                if (varName.Contains(".response.", StringComparison.OrdinalIgnoreCase))
                {
                    return ResolveRequestVariable(varName);
                }

                // Environment variables
                var envValue = HttpEnvironmentManager.ResolveVariable(varName);
                if (envValue is not null)
                {
                    return HttpBindingResult<object?>.Success(envValue);
                }

                // .env file variables
                if (varName.StartsWith("dotenv", StringComparison.OrdinalIgnoreCase))
                {
                    var dotenvVar = varName.Substring("dotenv".Length).Trim().Trim('%');
                    if (!string.IsNullOrEmpty(dotenvVar))
                    {
                        var envFile = System.IO.Path.Combine(Environment.CurrentDirectory, ".env");
                        if (System.IO.File.Exists(envFile))
                        {
                            foreach (var line in System.IO.File.ReadAllLines(envFile))
                            {
                                var parts = line.Split(new[] { '=' }, 2);
                                if (parts.Length == 2 && parts[0].Trim().Equals(dotenvVar, StringComparison.OrdinalIgnoreCase))
                                {
                                    return HttpBindingResult<object?>.Success(parts[1].Trim());
                                }
                            }
                        }
                    }
                }

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

            // Handle file body references: "< ./file.json" or "<@ ./file.json"
            if (request.Content is System.Net.Http.StringContent sc)
            {
                var bodyText = await sc.ReadAsStringAsync();
                var trimmed = bodyText.TrimStart();
                if (trimmed.StartsWith("<"))
                {
                    var bodyFilePath = trimmed.TrimStart('<', '@').Trim();
                    // Remove encoding specifier if present (e.g., "<@latin1 ./file.json")
                    // Simple: just take the last token as file path
                    var parts = bodyFilePath.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var actualPath = parts.Length > 1 ? parts[parts.Length - 1] : parts[0];
                    // Resolve relative to the .http file's directory
                    var httpFileDir = "";
                    var editorFile = editor.FileName?.ToString();
                    if (!string.IsNullOrEmpty(editorFile))
                        httpFileDir = System.IO.Path.GetDirectoryName(editorFile) ?? "";

                    var fullPath = System.IO.Path.IsPathRooted(actualPath)
                        ? actualPath
                        : System.IO.Path.Combine(httpFileDir, actualPath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        var fileContent = await System.IO.File.ReadAllTextAsync(fullPath);
                        request.Content = new System.Net.Http.StringContent(fileContent);
                        // Copy content type if set
                        if (sc.Headers.ContentType is not null)
                            request.Content.Headers.ContentType = sc.Headers.ContentType;
                    }
                    else
                    {
                        MessageService.ShowMessage($"File not found: {fullPath}");
                        return;
                    }
                }
            }

            // Basic auth auto-encoding: "Basic user:pass" or "Basic user pass" -> base64
            if (request.Headers.Authorization is not null &&
                request.Headers.Authorization.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                var param = request.Headers.Authorization.Parameter ?? "";
                if (!param.Contains('=') && !param.EndsWith("==") && !IsBase64(param))
                {
                    // Try "user:pass" or "user pass"
                    var credentials = param.Contains(':') ? param : param.Replace(' ', ':');
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
                }
            }

            // Detect named request
            var namedRequestNode = targetRequest.TryGetCommentNamedRequestNode();
            var requestName = namedRequestNode?.ValueNode?.Text?.Trim();

            _cts = new System.Threading.CancellationTokenSource();
            try
            {
                HttpCookieJar.ApplyCookies(request);

                var handler = new System.Net.Http.HttpClientHandler();
                var certPath = Environment.GetEnvironmentVariable("HTTP_CLIENT_CERT");
                var certPassword = Environment.GetEnvironmentVariable("HTTP_CLIENT_CERT_PASSWORD");
                if (!string.IsNullOrEmpty(certPath) && System.IO.File.Exists(certPath))
                {
                    var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath, certPassword);
                    handler.ClientCertificates.Add(cert);
                }

                // Digest auth: if Authorization header is "Digest user pass", use credentials
                if (request.Headers.Authorization is not null &&
                    request.Headers.Authorization.Scheme.Equals("Digest", StringComparison.OrdinalIgnoreCase))
                {
                    var param = request.Headers.Authorization.Parameter ?? "";
                    var parts = param.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var username = parts[0];
                        var password = parts[1];
                        handler.Credentials = new System.Net.NetworkCredential(username, password);
                        // Remove the manual Authorization header so handler can manage it
                        request.Headers.Authorization = null;
                    }
                }

                using var client = new System.Net.Http.HttpClient(handler);
                var response = await client.SendAsync(request, _cts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(_cts.Token);

                // Store cookies for subsequent requests
                if (request.RequestUri is not null)
                    HttpCookieJar.StoreCookies(request.RequestUri.Host, response.Headers);
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

                // Determine response file extension based on Content-Type
                var ext = ".txt";
                var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
                if (contentType.Contains("json")) ext = ".json";
                else if (contentType.Contains("xml")) ext = ".xml";
                else if (contentType.Contains("html")) ext = ".html";
                else if (contentType.Contains("javascript")) ext = ".js";
                else if (contentType.Contains("css")) ext = ".css";
                else if (contentType.Contains("text/plain")) ext = ".txt";

                // Store response for named request variable resolution
                if (!string.IsNullOrWhiteSpace(requestName))
                {
                    HttpRequestVariableStore.StoreResponse(requestName, response, responseBody);
                }

                // Open response in a new untitled document
                var content = Encoding.UTF8.GetBytes(responseText.ToString());
                SD.FileService.NewFile("Response" + ext, content);
            }
            catch (OperationCanceledException)
            {
                MessageService.ShowMessage("Request cancelled.");
            }
            catch (Exception ex)
            {
                MessageService.ShowMessage("Request failed: " + ex.Message);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        static bool IsBase64(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length % 4 != 0)
                return false;
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static HttpBindingResult<object?> ResolveRequestVariable(string varName)
        {
            var parts = varName.Split(new[] { '.' }, 4);
            if (parts.Length < 3)
                return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());

            var requestName = parts[0];
            if (!HttpRequestVariableStore.TryGetResponse(requestName, out var response) || response is null)
                return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());

            var responseType = parts[1]; // "response"
            var part = parts[2]; // "body" or "headers"

            if (part.Equals("headers", StringComparison.OrdinalIgnoreCase) && parts.Length >= 4)
            {
                var headerName = parts[3];
                if (response.Headers.TryGetValues(headerName, out var values))
                    return HttpBindingResult<object?>.Success(string.Join(", ", values));
                if (response.Content.Headers.TryGetValues(headerName, out var cvalues))
                    return HttpBindingResult<object?>.Success(string.Join(", ", cvalues));
                return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
            }

            if (part.Equals("body", StringComparison.OrdinalIgnoreCase))
            {
                var body = HttpRequestVariableStore.GetRequestBody(requestName);
                if (body is null)
                    return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());

                if (parts.Length < 4 || parts[3] == "*")
                    return HttpBindingResult<object?>.Success(body);

                var jsonPath = parts[3];
                if (jsonPath.StartsWith("$.") || jsonPath.StartsWith("$["))
                {
                    // Simple JSONPath: $.property or $.array[0].property
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var path = jsonPath.TrimStart('$').TrimStart('.');
                        var element = doc.RootElement;
                        foreach (var segment in path.Split('.'))
                        {
                            if (segment.Contains('['))
                            {
                                var idx = segment.IndexOf('[');
                                var prop = segment.Substring(0, idx);
                                if (!string.IsNullOrEmpty(prop))
                                    element = element.GetProperty(prop);
                                var indexStr = segment.Substring(idx + 1).TrimEnd(']');
                                var index = int.Parse(indexStr);
                                element = element[index];
                            }
                            else if (!string.IsNullOrEmpty(segment))
                            {
                                element = element.GetProperty(segment);
                            }
                        }
                        return HttpBindingResult<object?>.Success(element.ToString());
                    }
                    catch
                    {
                        return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
                    }
                }
                return HttpBindingResult<object?>.Success(body);
            }

            return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
        }

        static HttpBindingResult<object?> ResolveSystemVariable(string varName)
        {
            var name = varName.TrimStart('$').ToLowerInvariant();

            // Handle parameterized system variables
            if (name.StartsWith("randomint"))
            {
                var args = name.Replace("randomint", "").Trim();
                var parts = args.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[0], out var min) && int.TryParse(parts[1], out var max))
                {
                    var rng = new Random();
                    return HttpBindingResult<object?>.Success(rng.Next(min, max).ToString());
                }
                return HttpBindingResult<object?>.Success(new Random().Next(0, 1000).ToString());
            }

            if (name.StartsWith("uuid"))
            {
                return HttpBindingResult<object?>.Success(Guid.NewGuid().ToString());
            }

            if (name.StartsWith("datetime") || name.StartsWith("localdatetime"))
            {
                var isLocal = name.StartsWith("localdatetime");
                var rest = name.Replace("localdatetime", "").Replace("datetime", "").Trim();
                // Parse format: iso8601, rfc1123, or custom format in quotes
                var dto = isLocal ? DateTimeOffset.Now : DateTimeOffset.UtcNow;
                if (string.IsNullOrEmpty(rest) || rest == "iso8601")
                    return HttpBindingResult<object?>.Success(dto.ToString("o"));
                if (rest == "rfc1123")
                    return HttpBindingResult<object?>.Success(dto.ToString("R"));
                // Custom format in quotes
                if (rest.StartsWith("\"") && rest.EndsWith("\""))
                {
                    var format = rest.Trim('"');
                    return HttpBindingResult<object?>.Success(dto.ToString(format));
                }
                if (rest.StartsWith("'") && rest.EndsWith("'"))
                {
                    var format = rest.Trim('\'');
                    return HttpBindingResult<object?>.Success(dto.ToString(format));
                }
                return HttpBindingResult<object?>.Success(dto.ToString("o"));
            }

            if (name.StartsWith("timestamp"))
            {
                return HttpBindingResult<object?>.Success(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            }

            if (name.StartsWith("processenv"))
            {
                var envVar = name.Replace("processenv", "").Trim().Trim('%');
                if (!string.IsNullOrEmpty(envVar))
                {
                    var value = Environment.GetEnvironmentVariable(envVar);
                    if (value is not null)
                        return HttpBindingResult<object?>.Success(value);
                }
                return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
            }

            if (name.StartsWith("guid"))
            {
                return HttpBindingResult<object?>.Success(Guid.NewGuid().ToString());
            }

            switch (name)
            {
                case "guid":
                    return HttpBindingResult<object?>.Success(Guid.NewGuid().ToString());
                case "timestamp":
                    return HttpBindingResult<object?>.Success(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                default:
                    return HttpBindingResult<object?>.Failure(Array.Empty<Diagnostic>());
            }
        }
    }
}
