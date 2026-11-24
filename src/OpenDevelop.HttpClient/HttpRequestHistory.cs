using System;
using System.IO;
using System.Net.Http;
using System.Text;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Manages HTTP request history.
    /// </summary>
    public static class HttpRequestHistory
    {
        private static readonly string HistoryDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".opendevelop-httpclient");

        private static string HistoryFile => Path.Combine(HistoryDir, "history.txt");

        public static void Record(string method, string url, int statusCode, string? requestName)
        {
            try
            {
                Directory.CreateDirectory(HistoryDir);
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {method} {url} -> {statusCode}" +
                            (string.IsNullOrEmpty(requestName) ? "" : $" ({requestName})") + "\n";
                File.AppendAllText(HistoryFile, entry);
            }
            catch { }
        }

        public static string GetHistory()
        {
            try
            {
                if (File.Exists(HistoryFile))
                    return File.ReadAllText(HistoryFile);
            }
            catch { }
            return "";
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(HistoryFile))
                    File.Delete(HistoryFile);
            }
            catch { }
        }
    }
}
