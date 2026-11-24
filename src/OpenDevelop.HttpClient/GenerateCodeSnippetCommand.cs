using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class GenerateCodeSnippetCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            MessageService.ShowMessage("Code snippet generation: use the HTTP request in your .http file, then invoke this command. (Full language picker UI coming soon.)");
        }
    }
}
