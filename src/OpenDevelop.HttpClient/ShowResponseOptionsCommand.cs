using System;
using System.Text;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class ShowResponseBodyOnlyCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            if (string.IsNullOrEmpty(LastResponseStore.LastResponseBody))
            {
                MessageService.ShowMessage("No response body to display. Send a request first.");
                return;
            }
            var content = Encoding.UTF8.GetBytes(LastResponseStore.LastResponseBody);
            SD.FileService.NewFile("ResponseBodyOnly.txt", content);
        }
    }

    public sealed class ShowResponseHeadersOnlyCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            if (string.IsNullOrEmpty(LastResponseStore.LastResponseBody))
            {
                MessageService.ShowMessage("No response to display. Send a request first.");
                return;
            }
            // Show headers only - re-extract from stored response
            MessageService.ShowMessage("Response headers are included in the full response view. Use 'Show HTTP Response (Rich View)' for full details.");
        }
    }
}
