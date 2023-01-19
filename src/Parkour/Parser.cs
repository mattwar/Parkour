using System;
using System.Diagnostics;

namespace Parkour
{
    [System.Diagnostics.DebuggerDisplay("{DebugText}")]
    public abstract class Parser<TInput>
    {
        public abstract bool ParseAsObject(
            ReadOnlySpan<TInput> input, 
            out object output, 
            out ReadOnlySpan<TInput> remainingInput);

        public abstract bool Scan(
            ReadOnlySpan<TInput> input, 
            out ReadOnlySpan<TInput> remainingInput);

        public abstract bool Search(
            ReadOnlySpan<TInput> input,
            ref bool afterMissing,
            out ReadOnlySpan<TInput> remainingInput, 
            SearchCallback<TInput> fnCallback);

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
        public abstract bool Parse(ReadOnlySpan<TInput> input, out TOutput output, out ReadOnlySpan<TInput> remainingInput);

        public override bool ParseAsObject(ReadOnlySpan<TInput> input, out object output, out ReadOnlySpan<TInput> remainingInput)
        {
            if (this.Parse(input, out var typedOutput, out remainingInput))
            {
                output = typedOutput!;
                return true;
            }

            output = default!;
            return false;
        }
    }

    /// <summary>
    /// A parser that can parse the input and return multiple output items.
    /// </summary>
    public abstract class MultiParser<TInput, TOutput> : Parser<TInput, IReadOnlyList<TOutput>>
    {
        public abstract bool ParseInto(ReadOnlySpan<TInput> input, List<TOutput> outputList, out ReadOnlySpan<TInput> remainingInput);

        public override bool Parse(ReadOnlySpan<TInput> input, out IReadOnlyList<TOutput> output, out ReadOnlySpan<TInput> remainingInput)
        {
            var list = new List<TOutput>();
            if (ParseInto(input, list, out remainingInput))
            {
                output = list;
                return true;
            }

            output = default!;
            return false;
        }
    }

    /// <summary>
    /// Returns the number of items in the span that match
    /// </summary>
    public delegate int Matcher<TInput>(ReadOnlySpan<TInput> input);

    /// <summary>
    /// Converts the input to a single output value.
    /// </summary>
    public delegate TOutput Converter<TInput, TOutput>(ReadOnlySpan<TInput> input);
}