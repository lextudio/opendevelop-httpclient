using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Stores responses from named requests for variable resolution in subsequent requests.
    /// </summary>
    public static class HttpRequestVariableStore
    {
        private static readonly ConcurrentDictionary<string, HttpResponseMessage> _responses = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, string> _requestBodies = new(StringComparer.OrdinalIgnoreCase);

        public static void StoreResponse(string requestName, HttpResponseMessage response, string requestBody)
        {
            _responses[requestName] = response;
            _requestBodies[requestName] = requestBody;
        }

        public static bool TryGetResponse(string requestName, out HttpResponseMessage? response)
        {
            return _responses.TryGetValue(requestName, out response);
        }

        public static string? GetRequestBody(string requestName)
        {
            return _requestBodies.TryGetValue(requestName, out var body) ? body : null;
        }

        public static void Clear()
        {
            _responses.Clear();
            _requestBodies.Clear();
        }
    }

    /// <summary>
    /// Manages HTTP client environments (like REST Client's environmentVariables).
    /// </summary>
    public class HttpEnvironment
    {
        public string Name { get; set; } = "default";
        public Dictionary<string, string> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static HttpEnvironment LoadFromFile(string filePath)
        {
            var env = new HttpEnvironment();
            if (!System.IO.File.Exists(filePath))
                return env;

            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("environments", out var envs))
                {
                    // Could contain multiple environments; pick first or active
                }
                // Simple flat key-value
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                        env.Variables[prop.Name] = prop.Value.GetString() ?? "";
                }
            }
            catch { }
            return env;
        }
    }

    /// <summary>
    /// Global environment manager for HTTP client.
    /// </summary>
    public static class HttpEnvironmentManager
    {
        private static HttpEnvironment _active = new HttpEnvironment { Name = "No Environment" };
        private static readonly List<HttpEnvironment> _environments = new();

        public static HttpEnvironment ActiveEnvironment => _active;

        public static IReadOnlyList<HttpEnvironment> Environments => _environments;

        public static void SetActiveEnvironment(HttpEnvironment env)
        {
            _active = env ?? new HttpEnvironment { Name = "No Environment" };
        }

        public static void AddEnvironment(HttpEnvironment env)
        {
            _environments.Add(env);
        }

        public static string? ResolveVariable(string name)
        {
            if (_active.Variables.TryGetValue(name, out var value))
                return value;
            return null;
        }

        public static void LoadFromWorkspace(string workspaceDir)
        {
            _environments.Clear();
            // Try http-client.env.json
            var envFile = System.IO.Path.Combine(workspaceDir, "http-client.env.json");
            if (System.IO.File.Exists(envFile))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(envFile);
                    using var doc = JsonDocument.Parse(json);
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            var env = new HttpEnvironment { Name = prop.Name };
                            foreach (var v in prop.Value.EnumerateObject())
                            {
                                if (v.Value.ValueKind == JsonValueKind.String)
                                    env.Variables[v.Name] = v.Value.GetString() ?? "";
                            }
                            _environments.Add(env);
                        }
                    }
                }
                catch { }
            }

            // Also try .rest-client / .vscode settings
            if (_environments.Count == 0)
            {
                _environments.Add(new HttpEnvironment { Name = "No Environment" });
            }
        }
    }
}
