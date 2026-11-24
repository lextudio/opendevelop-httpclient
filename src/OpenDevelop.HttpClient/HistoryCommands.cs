using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class ViewHistoryCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var history = HttpRequestHistory.GetHistory();
            if (string.IsNullOrEmpty(history))
            {
                MessageService.ShowMessage("No request history yet.");
                return;
            }
            SD.FileService.NewFile("HttpHistory.txt", System.Text.Encoding.UTF8.GetBytes(history));
        }
    }

    public sealed class ClearHistoryCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            HttpRequestHistory.Clear();
            MessageService.ShowMessage("Request history cleared.");
        }
    }
}
