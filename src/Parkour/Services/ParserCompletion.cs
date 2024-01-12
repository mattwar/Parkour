namespace Parkour.Services;
using Parsing;

/// <summary>
/// Produces syntax completions via scanning through language grammar (parser).
/// </summary>
public class ParserCompletion<TInput>
{
    private readonly Parser<TInput> _parser;

    public ParserCompletion(Parser<TInput> parser)
    {
        _parser = parser;
    }

    public void GetCompletions(ReadOnlySpan<TInput> input, int inputIndex, List<CompletionItem> items)
    {
        var nextParsers = new List<Parser<TInput>>();

        _parser.GetNextParsers(
            input,
            inputIndex,
            (parser, afterMissing) => parser.Term != null && !afterMissing,
            nextParsers);

        items.AddRange(nextParsers.Select(p => new CompletionItem(p.Term!, p.Term!, p.Term!)));
    }
}