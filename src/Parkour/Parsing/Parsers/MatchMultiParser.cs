namespace Parkour.Parsers;

public sealed class MatchMultiParser<TInput> : MultiParser<TInput, TInput>
{
    private readonly Matcher<TInput> _matcher;
    private string? _term;

    public MatchMultiParser(Matcher<TInput> matcher, string? term)
    {
        _matcher = matcher;
        _term = term;
    }

    public override string? Term => _term;

    public override ParseIntoResult ParseInto(ReadOnlySpan<TInput> input, List<TInput> outputList)
    {
        var length = _matcher(input);
        if (length > 0 && length <= input.Length)
        {
            for (int i = 0; i < length; i++)
            {
                outputList.Add(input[i]);
            }

            return new ParseIntoResult(true, length);
        }

        return default;
    }

    public override ScanResult Scan(ReadOnlySpan<TInput> input)
    {
        var length = _matcher(input);
        if (length > 0)
        {
            return new ScanResult(true, length);
        }

        return default;
    }

    public override SearchResult Search(ReadOnlySpan<TInput> input, bool afterMissing, SearchCallback<TInput>? fnCallback)
    {
        fnCallback?.Invoke(this, input, afterMissing);

        var result = Scan(input);
        if (result.Success)
        {
            return new SearchResult(true, result.Length, false);
        }

        return default;
    }
}
