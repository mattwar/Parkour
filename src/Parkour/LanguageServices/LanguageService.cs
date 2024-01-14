namespace Parkour.Services;

public abstract class LanguageService
{
    /// <summary>
    /// Gets the <see cref="CompletionItem"/> available at the text position, 
    /// given the last key pressed.
    /// </summary>
    public virtual void GetCompletions(int position, char? lastKey, List<CompletionItem> completions)
    {
    }

    /// <summary>
    /// Gets the diagnostics that overlap the text position.
    /// </summary>
    public virtual void GetDiagnostics(int position, List<Diagnostic> diagnostics)
    {
    }

    /// <summary>
    /// Gets the classified text segments in the text range, in order.
    /// This information is used for text colorization in the editor.
    /// </summary>
    public virtual void GetClassifications(int start, int length, List<ClassifiedSpan> list)
    {
    }

    /// <summary>
    /// Gets the list of all categories produced by the language.
    /// </summary>
    public virtual ImmutableList<string> GetCategories()
    {
        return ImmutableList<string>.Empty;
    }

    /// <summary>
    /// Gets the text that would be displayed in a hover window above the mouse.
    /// </summary>
    public virtual HoverText? GetHoverText(int position)
    {
        return null;
    }

    /// <summary>
    /// Gets the list of <see cref="CodeAction"/> that can be applied at this position.
    /// These are used to form a menu for the user to choose.
    /// </summary>
    public virtual void GetActions(int position, List<CodeAction> actions)
    {
    }

    /// <summary>
    /// Get the list of operations to be applied in the text editor to perform the code action.
    /// </summary>
    public virtual void GetOperations(CodeAction action, List<CodeOperation> operations)
    {
    }
}

public class CodeAction
{   
}

public class CodeOperation
{

}

public class HoverText
{
}

/// <summary>
/// The classification for a specific text range.
/// </summary>
public struct ClassifiedSpan
{
    public string Classification { get; }
    public int Start { get; }
    public int Length { get; }

    public ClassifiedSpan(string classification, int start, int length)
    {
        this.Classification = classification;
        this.Start = start;
        this.Length = length;
    }
}

/// <summary>
/// This is a set of common classifications that a language service may produce.
/// </summary>
public static class Classifications
{
    public const string Text = nameof(Text);
    public const string Keyword = nameof(Keyword);
    public const string Punctuation = nameof(Punctuation);
    public const string Trivia = nameof(Trivia);
    public const string Comment = nameof(Comment);
    public const string Annotation = nameof(Annotation);
    public const string Literal = nameof(Literal);
    public const string NumericLiteral = nameof(NumericLiteral);
    public const string StringLiteral = nameof(StringLiteral);
    public const string DateTimeLiteral = nameof(DateTimeLiteral);
    public const string Intrinsic = nameof(Intrinsic);
    public const string TypeName = nameof(Type);
    public const string TypeMemberName = nameof(TypeMemberName);
    public const string TypeParameterName = nameof(TypeParameterName);
    public const string InterfaceName = nameof(InterfaceName);
    public const string NamespaceName = nameof(NamespaceName);
    public const string MethodName = nameof(MethodName);
    public const string FieldName = nameof(FieldName);
    public const string PropertyName = nameof(PropertyName);
    public const string EventName = nameof(EventName);
}

public static class Styles
{
    public const string Plain = nameof(Plain);
    public const string Bold = nameof(Bold);
    public const string Italic = nameof(Italic);
}

