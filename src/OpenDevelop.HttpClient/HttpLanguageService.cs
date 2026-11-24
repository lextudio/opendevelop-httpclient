using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.LanguageServices;
using Microsoft.DotNet.Interactive.Http.Parsing;

namespace OpenDevelop.HttpClient
{
    /// <summary>
    /// Minimal ILanguageService for .http files. Delegates to NoOpLanguageService for most
    /// methods, but provides diagnostics and quick info for HTTP variables.
    /// </summary>
    public class HttpLanguageService : ICSharpCode.SharpDevelop.LanguageServices.ILanguageService
    {
        readonly NoOpLanguageService _noop = NoOpLanguageService.Instance;

        public WorkspaceDocumentInfo? GetWorkspaceDocumentInfo(DocumentId documentId) => _noop.GetWorkspaceDocumentInfo(documentId);
        public Task<LensDocumentResult> GetLensDocumentAsync(DocumentId documentId, CancellationToken cancellationToken) => _noop.GetLensDocumentAsync(documentId, cancellationToken);
        public DocumentReadiness GetDocumentReadiness(DocumentId documentId) => DocumentReadiness.Ready;
        public long GetWorkspaceRevision() => 0;

        public Task UpsertDocumentAsync(DocumentId documentId, string text, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<CompletionResult> GetCompletionsAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) =>
            Task.FromResult(CompletionResult.Empty);

        public Task<QuickInfo?> GetQuickInfoAsync(DocumentId documentId, int offset, CancellationToken cancellationToken)
        {
            // Try to provide quick info for variables at the offset
            try
            {
                var text = System.IO.File.Exists(documentId.FileName)
                    ? System.IO.File.ReadAllText(documentId.FileName)
                    : "";
                // Simple: find variable at offset
                var varName = GetVariableAtOffset(text, offset);
                if (varName is not null)
                {
                    string? value = null;
                    if (varName.StartsWith("$"))
                    {
                        var name = varName.TrimStart('$').ToLowerInvariant();
                        switch (name)
                        {
                            case "guid": case "uuid": value = Guid.NewGuid().ToString(); break;
                            case "timestamp": value = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(); break;
                        }
                    }
                    else
                    {
                        value = HttpEnvironmentManager.ResolveVariable(varName);
                    }
                    if (value is not null)
                        return Task.FromResult<QuickInfo?>(new QuickInfo(varName + " = " + value));
                }
            }
            catch { }
            return Task.FromResult<QuickInfo?>(null);
        }

        public Task<IReadOnlyList<LanguageDiagnostic>> GetDiagnosticsAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            var diagnostics = new List<LanguageDiagnostic>();
            try
            {
                var text = System.IO.File.Exists(documentId.FileName)
                    ? System.IO.File.ReadAllText(documentId.FileName)
                    : "";
                var parseResult = HttpRequestParser.Parse(text);
                var tree = parseResult.SyntaxTree;
                foreach (var diag in tree.RootNode.GetDiagnostics())
                {
                    // Convert Roslyn Diagnostic to LanguageDiagnostic
                    var span = new TextSpan(
                        new TextPosition(diag.Location.GetLineSpan().StartLinePosition.Line + 1,
                                         diag.Location.GetLineSpan().StartLinePosition.Character + 1),
                        new TextPosition(diag.Location.GetLineSpan().EndLinePosition.Line + 1,
                                         diag.Location.GetLineSpan().EndLinePosition.Character + 1));
                    var severity = diag.Severity switch
                    {
                        Microsoft.CodeAnalysis.DiagnosticSeverity.Error => DiagnosticSeverity.Error,
                        Microsoft.CodeAnalysis.DiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                        _ => DiagnosticSeverity.Info
                    };
                    diagnostics.Add(new LanguageDiagnostic(diag.Id, diag.GetMessage(), severity, span));
                }
            }
            catch { }
            return Task.FromResult<IReadOnlyList<LanguageDiagnostic>>(diagnostics);
        }

        public Task<IReadOnlyList<NavigationTarget>> GoToDefinitionAsync(DocumentId documentId, int offset, CancellationToken cancellationToken)
        {
            var targets = new List<NavigationTarget>();
            try
            {
                var text = System.IO.File.Exists(documentId.FileName)
                    ? System.IO.File.ReadAllText(documentId.FileName)
                    : "";
                if (string.IsNullOrEmpty(text)) return Task.FromResult<IReadOnlyList<NavigationTarget>>(targets);

                // Find the word at offset
                var varName = GetVariableAtOffset(text, offset);
                if (varName is null) return Task.FromResult<IReadOnlyList<NavigationTarget>>(targets);

                // Search for variable definition: @varName =
                var lines = text.Split('\n');
                int lineNum = 0;
                int colNum = 0;
                foreach (var line in lines)
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("@") && trimmed.Contains("="))
                    {
                        var parts = trimmed.Substring(1).Split(new[] { '=' }, 2);
                        if (parts.Length >= 1 && parts[0].Trim().Equals(varName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Found definition
                            var col = line.IndexOf('@');
                            targets.Add(new NavigationTarget(documentId.FileName, new TextPosition(lineNum + 1, col + 2)));
                            break;
                        }
                    }

                    // Also check for named request: # @name requestName
                    if (trimmed.Contains("@name") && trimmed.Contains(varName))
                    {
                        var idx = trimmed.IndexOf("@name");
                        var rest = trimmed.Substring(idx + 5).Trim();
                        if (rest.StartsWith(varName, StringComparison.OrdinalIgnoreCase))
                        {
                            targets.Add(new NavigationTarget(documentId.FileName, new TextPosition(lineNum + 1, 1)));
                            break;
                        }
                    }
                    lineNum++;
                }
            }
            catch { }
            return Task.FromResult<IReadOnlyList<NavigationTarget>>(targets);
        }

        public Task<SymbolReferencesResult?> FindReferencesAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) =>
            Task.FromResult<SymbolReferencesResult?>(null);

        public Task<IReadOnlyList<TextEdit>> FormatAsync(DocumentId documentId, TextSpan? span, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TextEdit>>(Array.Empty<TextEdit>());

        public void OnTextChanged(DocumentId documentId, TextChange change) { }

        public Task<IReadOnlyList<DocumentOutlineNode>> GetDocumentOutlineAsync(DocumentId documentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DocumentOutlineNode>>(Array.Empty<DocumentOutlineNode>());

        public Task<IReadOnlyDictionary<string, IReadOnlyList<TextEdit>>> RenameSymbolAsync(DocumentId documentId, int offset, string newName, CancellationToken cancellationToken, bool renameOverloads = false, bool renameInStrings = false, bool renameInComments = false) =>
            Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<TextEdit>>>(new Dictionary<string, IReadOnlyList<TextEdit>>());

        public Task<string?> GetSymbolNameAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
        public Task<bool> IsValidIdentifierAsync(DocumentId documentId, string name, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<NavigationTarget>> FindMemberAsync(string typeFullName, string methodName, int? parameterCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NavigationTarget>>(Array.Empty<NavigationTarget>());

        public Task<IReadOnlyList<CodeActionInfo>> GetCodeActionsAsync(DocumentId documentId, TextSpan span, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CodeActionInfo>>(Array.Empty<CodeActionInfo>());

        public Task<IReadOnlyDictionary<string, IReadOnlyList<TextEdit>>> ApplyCodeActionAsync(DocumentId documentId, string actionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<TextEdit>>>(new Dictionary<string, IReadOnlyList<TextEdit>>());

        public Task<ExtractInterfaceInfo?> GetExtractInterfaceInfoAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<ExtractInterfaceInfo?>(null);
        public Task<ExtractInterfaceResult?> ExtractInterfaceAsync(DocumentId documentId, int offset, string interfaceName, IReadOnlyList<string> memberIds, bool addInterfaceToClass, bool includeComments, CancellationToken cancellationToken) => Task.FromResult<ExtractInterfaceResult?>(null);
        public Task<SymbolKindInfo?> GetSymbolKindAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<SymbolKindInfo?>(null);
        public Task<IReadOnlyList<SemanticToken>> GetSemanticTokensAsync(DocumentId documentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SemanticToken>>(Array.Empty<SemanticToken>());

        public Task<SymbolHierarchyResult?> GetBaseSymbolsAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<SymbolHierarchyResult?>(null);
        public Task<SymbolHierarchyResult?> GetDerivedSymbolsAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<SymbolHierarchyResult?>(null);
        public Task<string?> GetHelpKeywordAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
        public Task<string?> GetContainingTypeNameAsync(DocumentId documentId, int offset, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
        public Task RefreshProjectAsync(DocumentId documentId, CancellationToken cancellationToken) => Task.CompletedTask;

        static string? GetVariableAtOffset(string text, int offset)
        {
            // Find {{...}} at offset
            var idx = text.LastIndexOf("{{", offset);
            if (idx < 0 || idx + 2 > text.Length) return null;
            var end = text.IndexOf("}}", idx);
            if (end < 0 || offset > end + 2) return null;
            return text.Substring(idx + 2, end - idx - 2).Trim();
        }
    }

    public class RegisterHttpLanguageServiceCommand : AbstractCommand
    {
        IDisposable? registration;

        public override void Run()
        {
            var registry = SD.GetRequiredService<LanguageServiceRegistry>();
            registration = registry.RegisterExtension(".http", new HttpLanguageService());
            registry.RegisterExtension(".rest", new HttpLanguageService());
            LoggingService.Debug("Registered HTTP language service.");
        }

        public void Dispose() => registration?.Dispose();
    }
}
