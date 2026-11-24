using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Handles WebSocket requests (WEBSOCKET method in .http files).
    /// </summary>
    public static class HttpWebSocketHandler
    {
        public static async Task<string> ConnectAndReceiveAsync(string url, string? message, CancellationToken ct)
        {
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(url), ct);

            var sb = new StringBuilder();
            sb.AppendLine($"Connected to {url}");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(message))
            {
                var sendBytes = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(sendBytes), WebSocketMessageType.Text, true, ct);
                sb.AppendLine($"Sent: {message}");
                sb.AppendLine();
            }

            // Receive messages for up to 5 seconds or until close
            var buffer = new byte[4096];
            var receiveCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var combinedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, receiveCts.Token).Token;

            try
            {
                while (ws.State == WebSocketState.Open && !combinedCt.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), combinedCt);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        sb.AppendLine("Connection closed by server.");
                        break;
                    }
                    var received = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    sb.AppendLine($"Received: {received}");
                }
            }
            catch (OperationCanceledException)
            {
                sb.AppendLine("(Receive timeout)");
            }

            if (ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None);
            }

            return sb.ToString();
        }
    }
}
