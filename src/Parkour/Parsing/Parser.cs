using System.Diagnostics;

namespace Parkour.Parsing;

/// <summary>
/// A parser that can parse the input into a single weakly-typed output item.
/// (Also scan and search)
/// </summary>
[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Parser<TInput>
{
    /// <summary>
    /// Parse zero or more input items and produce a single weakly-typed output item.
    /// </summary>
    public abstract ParseResult<object> ParseAsObject(
        ReadOnlySpan<TInput> input);

    /// <summary>
    /// Determine if (and how many) input items match the parser's grammar.
    /// </summary>
    public abstract ScanResult Scan(
        ReadOnlySpan<TInput> input);

    /// <summary>
    /// Analyze the scanning of the input items, by calling the callback for each
    /// parser that considered.
    /// </summary>
    public abstract SearchResult Search(
        ReadOnlySpan<TInput> input,
        bool afterMissing,
        SearchCallback<TInput>? fnCallback);

    /// <summary>
    /// True if the parser is required to succeed even if the grammar does not match.
    /// Similar to optional, but will always return non-null result.
    /// </summary>
    public virtual bool IsRequired => false;

    /// <summary>
    /// A list of zero or more annotations associated with the parser.
    /// </summary>
    public virtual ImmutableList<object> Annotations =>
        ImmutableList<object>.Empty;

    /// <summary>
    /// The basic term associated with the parser (if any).
    /// Used mostly for debugging.
    /// </summary>
    public virtual string? Term =>
        this.Annotations.OfType<string>().FirstOrDefault();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual string DebugText => $"{DebugParserName}: {DebugContent}";

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual string DebugContent => Term ?? "";

    protected string DebugParserName 
    { 
        get 
        { 
            var name = this.GetType().Name;
            var index = name.IndexOf("Parser");
            return index > 0 ? name.Substring(0, index) : name;
        } 
    }
}

/// <summary>
/// A parser that can parse the input into a single strongly-typed output item.
/// </summary>
public abstract class Parser<TInput, TOutput> : Parser<TInput>
{
    /// <summary>
    /// Parse zero or more input items and produce a single strongly-typed output item.
    /// </summary>
    public abstract ParseResult<TOutput> Parse(ReadOnlySpan<TInput> input);

    public override ParseResult<object> ParseAsObject(ReadOnlySpan<TInput> input)
    {
        var (success, consumed, typedOutput) = this.Parse(input);
        if (success)
        {
            return new ParseResult<object>(true, consumed, (object)typedOutput!);
        }

        return default;
    }
}

/// <summary>
/// A parser that can parse the input into multiple output items.
/// </summary>
public abstract class MultiParser<TInput, TOutput> : Parser<TInput, IReadOnlyList<TOutput>>
{
    /// <summary>
    /// Parse zero or more input items and produce zero or more strongly-typed output items into the output list.
    /// </summary>
    public abstract ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList);

    public override ParseResult<IReadOnlyList<TOutput>> Parse(ReadOnlySpan<TInput> input)
    {
        var list = new List<TOutput>();
        var result = ParseInto(input, list);
        if (result.Success)
        {
            return new ParseResult<IReadOnlyList<TOutput>>(true, result.Length, list);
        }
        else
        {
            return default;
        }
    }
}

/// <summary>
/// Result of calling Parser.Scan
/// </summary>
public record struct ScanResult(bool Success, int Length);

/// <summary>
/// Result of calling Parser.ParseInto
/// </summary>
public record struct ParseIntoResult(bool Success, int Length);

/// <summary>
/// Result of calling Parser.Parse
/// </summary>
public record struct ParseResult<TOutput>(bool Success, int Length, TOutput Output);

/// <summary>
/// Result of calling Parser.Search
/// </summary>
public record struct SearchResult(bool Success, int Length, bool AfterMissing);

/// <summary>
/// A function that is called for each parser while searching.
/// </summary>
public delegate void SearchCallback<TInput>(Parser<TInput> parser, ReadOnlySpan<TInput> remainingInput, bool afterMissing);

/// <summary>
/// Returns the number of items in the span that match.
/// Returns -1 for failure.
/// </summary>
public delegate int Matcher<TInput>(ReadOnlySpan<TInput> input);

/// <summary>
/// Converts the input to a single output value.
/// </summary>
public delegate TOutput Converter<TInput, TOutput>(ReadOnlySpan<TInput> input);