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
    /// The text used to order the item in the completion list.
    /// </summary>
    public string OrderText { get; }

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
        string? matchText = null,
        string? orderText = null,
        string? beforeText = null,
        string? afterText = null)
    {
        this.DisplayText = displayText;
        this.MatchText = matchText ?? displayText;
        this.OrderText = orderText ?? displayText;
        this.BeforeText = beforeText ?? displayText;
        this.AfterText = afterText ?? "";
    }
}
