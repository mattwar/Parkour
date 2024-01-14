namespace Parkour.Services;

public class CompletionItem
{
    /// <summary>
    /// The text that is displayed in the completion list.
    /// </summary>
    public string DisplayText { get; }

    /// <summary>
    /// The text used to match keys typed to narrow the list.
    /// </summary>
    public string MatchText { get; }

    /// <summary>
    /// The part of the insertion text that is applied before the editor caret.
    /// </summary>
    public string BeforeText { get; }

    /// <summary>
    /// The part of the insertion text that is applied after the editor caret.
    /// </summary>
    public string AfterText { get; }

    public CompletionItem(
        string displayText,
        string matchText,
        string beforeText,
        string? afterText = null)
    {
        this.DisplayText = displayText;
        this.MatchText = matchText;
        this.BeforeText = beforeText;
        this.AfterText = afterText ?? "";
    }
}
