using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Simple cookie jar for subsequent requests to the same host.
    /// </summary>
    public static class HttpCookieJar
    {
        private static readonly ConcurrentDictionary<string, CookieContainer> _containers = new(StringComparer.OrdinalIgnoreCase);

        public static CookieContainer GetContainer(string host)
        {
            return _containers.GetOrAdd(host, _ => new CookieContainer());
        }

        public static void StoreCookies(string host, HttpResponseHeaders headers)
        {
            if (headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var container = GetContainer(host);
                foreach (var cookie in cookies)
                {
                    try
                    {
                        container.SetCookies(new Uri("http://" + host), cookie);
                    }
                    catch { }
                }
            }
        }

        public static void ApplyCookies(HttpRequestMessage request)
        {
            if (request.RequestUri is null) return;
            var host = request.RequestUri.Host;
            var container = GetContainer(host);
            var cookieHeader = container.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }
        }

        public static void Clear()
        {
            _containers.Clear();
        }
    }
}
