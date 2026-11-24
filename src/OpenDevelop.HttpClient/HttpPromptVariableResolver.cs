using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Detects and handles prompt variables (@prompt) in .http files.
    /// </summary>
    public static class HttpPromptVariableResolver
    {
        // Regex to match: // @prompt varName [description]
        // or: # @prompt varName [description]
        static readonly Regex PromptRegex = new Regex(
            @"(?:#|//)\s*@prompt\s+(\w+)(?:\s+(.+))?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<(string Name, string Description)> FindPromptVariables(string text, int requestStartOffset)
        {
            var results = new List<(string, string)>();
            // Look backwards from request start to find prompt declarations
            var lines = text.Substring(0, requestStartOffset).Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    break;
                if (line.StartsWith("###"))
                    break;

                var match = PromptRegex.Match(line);
                if (match.Success)
                {
                    var name = match.Groups[1].Value;
                    var desc = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";
                    results.Insert(0, (name, desc));
                }
            }
            return results;
        }

        public static string? PromptForValue(string varName, string description)
        {
            var message = string.IsNullOrEmpty(description)
                ? $"Enter value for '{varName}':"
                : $"{description} (for '{varName}'):";

            return ICSharpCode.Core.MessageService.ShowInputBox("HTTP Request", message, "");
        }
    }
}
