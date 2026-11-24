using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Generates code snippets from HTTP requests in various languages.
    /// </summary>
    public static class HttpCodeSnippetGenerator
    {
        public static string GeneratePython(HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import requests");
            sb.AppendLine();
            var method = request.Method.Method.ToLowerInvariant();
            sb.Append($"response = requests.{method}(\"{request.RequestUri}\"");
            if (request.Headers.Count() > 0)
            {
                sb.AppendLine(",");
                sb.Append("    headers={");
                var headers = new List<string>();
                foreach (var h in request.Headers)
                    foreach (var v in h.Value)
                        headers.Add($"\"{h.Key}\": \"{v}\"");
                sb.Append(string.Join(", ", headers));
                sb.Append("}");
            }
            if (request.Content is not null)
            {
                // Note: body not extracted for simplicity
            }
            sb.AppendLine(")");
            sb.AppendLine("print(response.text)");
            return sb.ToString();
        }

        public static string GenerateJavaScript(HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("fetch(\"" + request.RequestUri + "\", {");
            sb.AppendLine($"  method: \"{request.Method}\",");
            if (request.Headers.Count() > 0)
            {
                sb.AppendLine("  headers: {");
                var headers = new List<string>();
                foreach (var h in request.Headers)
                    foreach (var v in h.Value)
                        headers.Add($"    \"{h.Key}\": \"{v}\"");
                sb.AppendLine(string.Join(",\n", headers));
                sb.AppendLine("  },");
            }
            sb.AppendLine("})");
            sb.AppendLine("  .then(response => response.text())");
            sb.AppendLine("  .then(data => console.log(data))");
            sb.AppendLine("  .catch(error => console.error(error));");
            return sb.ToString();
        }

        public static string GenerateCSharp(HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine();
            sb.AppendLine("var client = new HttpClient();");
            foreach (var h in request.Headers)
                foreach (var v in h.Value)
                    sb.AppendLine($"client.DefaultRequestHeaders.Add(\"{h.Key}\", \"{v}\");");
            sb.AppendLine($"var response = await client.{request.Method.Method.ToLowerInvariant().Replace("delete", "Delete").Replace("get", "Get").Replace("post", "Post").Replace("put", "Put").Replace("patch", "Patch")}Async(\"{request.RequestUri}\");");
            sb.AppendLine("var content = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("Console.WriteLine(content);");
            return sb.ToString();
        }

        public static string GenerateCurl(HttpRequestMessage request)
        {
            return HttpCurlConverter.HttpToCurl(request);
        }
    }
}
