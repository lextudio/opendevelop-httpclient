using System;
using System.IO;
using System.Text;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class SaveResponseBodyOnlyCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            if (string.IsNullOrEmpty(LastResponseStore.LastResponseBody))
            {
                MessageService.ShowMessage("No response body to save. Send a request first.");
                return;
            }

            var ext = ".txt";
            var ct = LastResponseStore.LastContentType?.ToLowerInvariant() ?? "";
            if (ct.Contains("json")) ext = ".json";
            else if (ct.Contains("xml")) ext = ".xml";
            else if (ct.Contains("html")) ext = ".html";

            var content = Encoding.UTF8.GetBytes(LastResponseStore.LastResponseBody);
            SD.FileService.NewFile("ResponseBodyOnly" + ext, content);
        }
    }
}
