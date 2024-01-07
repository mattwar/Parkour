using System.Diagnostics;

namespace Parkour.Parsing;

public record struct ScanResult(bool Success, int Length);
public record struct ParseIntoResult(bool Success, int Length);
public record struct ParseResult<TOutput>(bool Success, int Length, TOutput Output);
public record struct SearchResult(bool Success, int Length, bool AfterMissing);

[System.Diagnostics.DebuggerDisplay("{DebugText}")]
public abstract class Parser<TInput>
{
    public abstract ParseResult<object> ParseAsObject(
        ReadOnlySpan<TInput> input);

    public abstract ScanResult Scan(
        ReadOnlySpan<TInput> input);

    public abstract SearchResult Search(
        ReadOnlySpan<TInput> input,
        bool afterMissing,
        SearchCallback<TInput>? fnCallback);

    public virtual bool IsRequired => false;

    public virtual string? Term => null;

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

public delegate void SearchCallback<TInput>(Parser<TInput> parser, ReadOnlySpan<TInput> remainingInput, bool afterMissing);
public delegate TState BeforeAction<TInput, TState>(Parser<TInput> parser, ReadOnlySpan<TInput> input, TState state);
public delegate TState AfterAction<TInput, TState>(Parser<TInput> parser, ReadOnlySpan<TInput> remainingInput, bool success, TState state);

/// <summary>
/// A parser that can parse the input and return a single output item.
/// </summary>
public abstract class Parser<TInput, TOutput> : Parser<TInput>
{
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
/// A parser that can parse the input and return multiple output items.
/// </summary>
public abstract class MultiParser<TInput, TOutput> : Parser<TInput, IReadOnlyList<TOutput>>
{
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
/// Returns the number of items in the span that match.
/// Returns -1 for failure.
/// </summary>
public delegate int Matcher<TInput>(ReadOnlySpan<TInput> input);

/// <summary>
/// Converts the input to a single output value.
/// </summary>
public delegate TOutput Converter<TInput, TOutput>(ReadOnlySpan<TInput> input);