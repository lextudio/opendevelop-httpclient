using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace OpenDevelop.HttpClient
{
    public sealed class SwitchEnvironmentCommand : AbstractMenuCommand
    {
        public override void Run()
        {
            var envs = HttpEnvironmentManager.Environments;
            if (envs.Count == 0)
            {
                MessageService.ShowMessage("No environments configured. Add http-client.env.json to your workspace.");
                return;
            }

            var names = new string[envs.Count];
            for (int i = 0; i < envs.Count; i++)
                names[i] = envs[i].Name;

            var current = HttpEnvironmentManager.ActiveEnvironment.Name;
            var selected = MessageService.ShowCustomDialog(
                "Switch Environment",
                "Select environment:",
                0, 1,
                names);

            if (selected >= 0 && selected < envs.Count)
            {
                HttpEnvironmentManager.SetActiveEnvironment(envs[selected]);
                MessageService.ShowMessage($"Switched to environment: {envs[selected].Name}");
            }
        }
    }
}
