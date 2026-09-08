using Parkour.Services;
using Parkour.Text;
using VSLSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Parkour.LSP;

public static class LspExtensions
{
    public static int ToTextPosition(this VSLSP.Position position, string text)
    {
        return text.GetTextPosition(position.Line - 1, position.Character - 1);
    }

    public static TextRange ToTextRange(this VSLSP.Range range, string text)
    {
        var start = range.Start.ToTextPosition(text);
        var end = range.End.ToTextPosition(text);
        return TextRange.FromBounds(start, end);
    }

    public static VSLSP.Position ToLspPosition(this int textPosition, string text)
    {
        var lp = text.GetLinePosition(textPosition);
        return new VSLSP.Position(lp.Line + 1, lp.Offset + 1);
    }

    public static VSLSP.Range ToLspRange(this TextRange range, string text)
    {
        var start = range.Start.ToLspPosition(text);
        var end = range.End.ToLspPosition(text);
        return new VSLSP.Range { Start = start, End = end };
    }

    public static VSLSP.Range ToLspRange(this ISourceLocation location)
    {
        return new TextRange(location.Start, location.Length).ToLspRange(location.Document.Text);
    }

    public static VSLSP.DiagnosticSeverity ToLspSeverity(this DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => VSLSP.DiagnosticSeverity.Error,
            DiagnosticSeverity.Warning => VSLSP.DiagnosticSeverity.Warning,
            DiagnosticSeverity.Hint => VSLSP.DiagnosticSeverity.Hint,
            DiagnosticSeverity.Information => VSLSP.DiagnosticSeverity.Information,
            _ => VSLSP.DiagnosticSeverity.Error,
        };
    }

    public static VSLSP.CompletionItemKind ToLspCompletionKind(this CompletionKind kind)
    {
        return kind switch
        {
            CompletionKind.Text => VSLSP.CompletionItemKind.Text,
            CompletionKind.Class => VSLSP.CompletionItemKind.Class,
            CompletionKind.Method => VSLSP.CompletionItemKind.Method,
            CompletionKind.Function => VSLSP.CompletionItemKind.Function,
            CompletionKind.Constructor => VSLSP.CompletionItemKind.Constructor,
            CompletionKind.Field => VSLSP.CompletionItemKind.Field,
            CompletionKind.Variable => VSLSP.CompletionItemKind.Variable,
            CompletionKind.Interface => VSLSP.CompletionItemKind.Interface,
            CompletionKind.Module => VSLSP.CompletionItemKind.Module,
            CompletionKind.Property => VSLSP.CompletionItemKind.Property,
            CompletionKind.Unit => VSLSP.CompletionItemKind.Unit,
            CompletionKind.Value => VSLSP.CompletionItemKind.Value,
            CompletionKind.Enum => VSLSP.CompletionItemKind.Enum,
            CompletionKind.Keyword => VSLSP.CompletionItemKind.Keyword,
            CompletionKind.Snippet => VSLSP.CompletionItemKind.Snippet,
            CompletionKind.Color => VSLSP.CompletionItemKind.Color,
            CompletionKind.File => VSLSP.CompletionItemKind.File,
            CompletionKind.Reference => VSLSP.CompletionItemKind.Reference,
            CompletionKind.Folder => VSLSP.CompletionItemKind.Folder,
            CompletionKind.EnumMember => VSLSP.CompletionItemKind.EnumMember,
            CompletionKind.Constant => VSLSP.CompletionItemKind.Constant,
            CompletionKind.Struct => VSLSP.CompletionItemKind.Struct,
            CompletionKind.Event => VSLSP.CompletionItemKind.Event,
            CompletionKind.Operator => VSLSP.CompletionItemKind.Operator,
            CompletionKind.TypeParameter => VSLSP.CompletionItemKind.TypeParameter,
            CompletionKind.Macro => VSLSP.CompletionItemKind.Keyword,
            CompletionKind.Namespace => VSLSP.CompletionItemKind.Namespace,
            CompletionKind.Template => VSLSP.CompletionItemKind.Snippet,
            CompletionKind.TypeDefinition => VSLSP.CompletionItemKind.TypeParameter,
            CompletionKind.Union => VSLSP.CompletionItemKind.Class,
            CompletionKind.Delegate => VSLSP.CompletionItemKind.Function,
            CompletionKind.TagHelper => VSLSP.CompletionItemKind.Class,
            CompletionKind.ExtensionMethod => VSLSP.CompletionItemKind.Method,
            CompletionKind.Element => VSLSP.CompletionItemKind.Text,
            CompletionKind.LocalResource => VSLSP.CompletionItemKind.File,
            CompletionKind.SystemResource => VSLSP.CompletionItemKind.File,
            CompletionKind.CloseElement => VSLSP.CompletionItemKind.Text,
            _ => VSLSP.CompletionItemKind.Text
        };
    }

}
