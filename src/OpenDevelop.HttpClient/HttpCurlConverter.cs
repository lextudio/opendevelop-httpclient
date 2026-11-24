using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Converts cURL commands to .http format and vice versa.
    /// </summary>
    public static class HttpCurlConverter
    {
        /// <summary>
        /// Converts a cURL command string to .http format.
        /// </summary>
        public static string CurlToHttp(string curlCommand)
        {
            var lines = curlCommand.Trim().Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var method = "GET";
            var url = "";
            var headers = new List<string>();
            var body = "";
            var followRedirects = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var arg = lines[i];
                switch (arg.ToLowerInvariant())
                {
                    case "curl":
                        break;
                    case "-x":
                    case "--request":
                        if (i + 1 < lines.Length) method = lines[++i].ToUpperInvariant();
                        break;
                    case "-h":
                    case "--header":
                        if (i + 1 < lines.Length) headers.Add(lines[++i]);
                        break;
                    case "-d":
                    case "--data":
                    case "--data-ascii":
                    case "--data-binary":
                    case "--data-raw":
                        if (i + 1 < lines.Length)
                        {
                            body = lines[++i];
                            if (method == "GET") method = "POST";
                        }
                        break;
                    case "-l":
                    case "--location":
                        followRedirects = true;
                        break;
                    case "-u":
                    case "--user":
                        if (i + 1 < lines.Length)
                        {
                            var creds = lines[++i];
                            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(creds));
                            headers.Add($"Authorization: Basic {encoded}");
                        }
                        break;
                    case "-i":
                    case "--head":
                        method = "HEAD";
                        break;
                    default:
                        if (arg.StartsWith("http://") || arg.StartsWith("https://"))
                            url = arg;
                        else if (!arg.StartsWith("-"))
                            url = arg;
                        break;
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(method + " " + url);
            if (followRedirects)
                sb.Append(" HTTP/1.1");
            sb.AppendLine();
            foreach (var h in headers)
                sb.AppendLine(h);
            if (!string.IsNullOrEmpty(body))
            {
                sb.AppendLine();
                sb.AppendLine(body);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Converts an HttpRequestMessage to a cURL command.
        /// </summary>
        public static string HttpToCurl(HttpRequestMessage request)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("curl");
            if (request.Method != HttpMethod.Get)
                sb.Append($" -X {request.Method}");
            foreach (var h in request.Headers)
            {
                foreach (var v in h.Value)
                    sb.Append($" -H \"{h.Key}: {v}\"");
            }
            if (request.Content is not null)
            {
                foreach (var h in request.Content.Headers)
                {
                    foreach (var v in h.Value)
                        sb.Append($" -H \"{h.Key}: {v}\"");
                }
                // Note: body not included for simplicity
            }
            sb.Append($" \"{request.RequestUri}\"");
            return sb.ToString();
        }
    }
}
