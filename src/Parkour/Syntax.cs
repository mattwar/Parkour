using System;
using System.Linq;
using System.Text;

namespace Parkour
{
    public class Syntax
    {
        private readonly string _text;
        private readonly Parser<LexicalToken> _parser;
        private readonly LexicalToken[] _tokens;
        private readonly SyntaxElement _root;

        public Syntax(
            string text,
            Parser<LexicalToken> parser,
            LexicalToken[] tokens,
            SyntaxElement root)
        {
            _text = text;
            _parser = parser;
            _tokens = tokens;
            _root = root;
        }

        public string Text => _text;
        public SyntaxElement Root => _root;

        /// <summary>
        /// Gets the set of terms that could appear at the text position
        /// </summary>
        public IReadOnlyList<string> GetNextTermsAt(int textPosition)
        {
            var terms = new List<string>();

            if (TryGetTokenIndex(textPosition, out var tokenIndex, out var textOffsetInToken))
            {
                // affinitize with previous token?
                if (tokenIndex < _tokens.Length && tokenIndex > 0 && textOffsetInToken == 0)
                {
                    var token = _tokens[tokenIndex];
                    if (token.Trivia.Length > 0 
                        || (token.Text.Length > 0 && char.IsLetter(token.Text[0])))
                    {
                        tokenIndex--;
                    }
                }

                var input = _tokens.AsSpan();
                var nextParsers = _parser.GetNextParsers(
                    input, tokenIndex, 
                    (parser, afterMissing) => parser.Term != null && !afterMissing);
                terms.AddRange(nextParsers.Select(p => p.Term).ToHashSet()!);
            }

            return terms.ToArray();
        }

        private record struct NextTermsInfo(int InputLength, bool Required, bool PrevWasMissing);

        /// <summary>
        /// Returns the index of the token that contains the text position.
        /// </summary>
        public bool TryGetTokenIndex(int textPosition, out int tokenIndex)
        {
            return TryGetTokenIndex(textPosition, out tokenIndex, out _);
        }

        /// <summary>
        /// Returns the index of the token that contains the text position.
        /// </summary>
        public bool TryGetTokenIndex(int textPosition, out int tokenIndex, out int textOffsetInToken)
        {
            if (textPosition < _text.Length)
            {
                for (int i = 0; i < _tokens.Length; i++)
                {
                    var token = _tokens[i];
                    if (textPosition < token.Length)
                    {
                        tokenIndex = i;
                        textOffsetInToken = token.Length - textPosition;
                        return true;
                    }

                    textPosition -= token.Length;
                }
            }
            else if (textPosition == _text.Length && _tokens.Length > 0)
            {
                tokenIndex = _tokens.Length - 1;
                textOffsetInToken = _tokens[tokenIndex].Length;
                return true;
            }

            tokenIndex = default;
            textOffsetInToken = default;
            return false;
        }

        private IReadOnlyList<Diagnostic>? _diagnostics;

        /// <summary>
        /// Gets a list of all the diagnostics produced during parsing.
        /// </summary>
        public IReadOnlyList<Diagnostic> GetDiagnostics()
        {
            if (_diagnostics == null)
            {
                var list = new List<Diagnostic>();
                Gather(_root);
                _diagnostics = list;

                void Gather(SyntaxElement element)
                {
                    if (element.Diagnostic != null)
                        list.Add(element.Diagnostic.WithLocation(element));

                    if (element is SyntaxNode node)
                    {
                        foreach (var subElem in node.Elements)
                        {
                            Gather(subElem);
                        }
                    }
                }
            }

            return _diagnostics;
        }
    }

    [System.Diagnostics.DebuggerDisplay("{Kind}: {Text}")]
    public struct LexicalToken
    {
        public string Kind { get; }
        public string Trivia { get; }
        public string Text { get; }
        public Diagnostic? Diagnostic { get; }

        public LexicalToken(string kind, string trivia, string text, Diagnostic? diagnostic = null)
        {
            this.Kind = kind ?? "";
            this.Trivia = trivia ?? "";
            this.Text = text ?? "";
            this.Diagnostic = diagnostic;
        }

        public int Length => Trivia.Length + Text.Length;
    }

    public class Diagnostic
    {
        public string Code { get; }
        public string Severity { get; }
        public string Message { get; }

        private readonly int _start;
        public bool HasLocation => _start >= 0;
        public int Start => this.HasLocation ? _start : 0;
        public int Length { get; }

        private Diagnostic(string code, string severity, string message, int start, int length)
        {
            this.Code = code;
            this.Severity = severity;
            this.Message = message;
            _start = start;
            this.Length = length;
        }

        public Diagnostic(string code, string severity, string message)
            : this(code, severity, message, -1, 0)
        {
        }

        public Diagnostic(string message)
            : this("", "Error", message, -1, 0)
        {
        }

        public Diagnostic WithLocation(int start, int length)
        {
            return new Diagnostic(this.Code, this.Severity, this.Message, start, length);
        }

        public Diagnostic WithLocation(SyntaxElement element) =>
            WithLocation(element.TextStart, element.TextLength);
    }

    [System.Diagnostics.DebuggerDisplay("{Kind}: {DebugText}")]
    public abstract class SyntaxElement
    {
        public string Kind { get; }
        public Diagnostic? Diagnostic { get; }

        private SyntaxElement? _parent;
        private int _offsetInParent = -1;

        protected SyntaxElement(string kind, Diagnostic? diagnostic)
        {
            this.Kind = kind;
            this.Diagnostic = diagnostic;
            _parent = default!;
        }

        public SyntaxElement? Parent => _parent;

        public abstract int Length { get; }
        public abstract int TextStart { get; }

        public int TextLength => this.Length - (this.TextStart - this.Start);
        public int End => this.Start + this.Length;

        public bool IsMissing => this.Diagnostic != null && this.TextLength == 0;

        internal SyntaxElement WithParent(SyntaxElement parent, int offsetInParent)
        {
            if (_parent == null)
            {
                _parent = parent;
                _offsetInParent = offsetInParent;
                _start = -1;
                return this;
            }
            else
            {
                return Clone().WithParent(parent, offsetInParent);
            }
        }

        private int _start;
        public int Start
        {
            get
            {
                if (_start == -1 && _parent != null)
                {
                    _start = _parent.Start + _offsetInParent;
                }

                return _start;
            }
        }

        public abstract SyntaxElement Clone();

        public override string ToString()
        {
            return ToString(0, this.Length);
        }

        public string ToString(int start, int length)
        {
            var builder = new StringBuilder();
            WriteString(builder, start, length);
            return builder.ToString();
        }

        internal abstract void WriteString(StringBuilder builder, int start, int length);

        private string DebugText => ToString(this.TextStart, 80);
    }

    public class SyntaxToken : SyntaxElement
    {
        public string Trivia { get; }
        public string Text { get; }

        public SyntaxToken(string kind, string trivia, string text, Diagnostic? diagnostic = null)
            : base(kind, diagnostic)
        {
            this.Trivia = trivia;
            this.Text = text;
        }

        public SyntaxToken(LexicalToken token)
            : this(token.Kind, token.Trivia, token.Text, token.Diagnostic)
        {
        }

        public override int Length => this.Trivia.Length + this.Text.Length;
        public override int TextStart => this.Start + this.Trivia.Length;

        public override SyntaxElement Clone()
        {
            return new SyntaxToken(this.Kind, this.Text, this.Trivia, this.Diagnostic);
        }

        public override string ToString()
        {
            return $"{this.Trivia}{this.Text}";
        }

        internal override void WriteString(StringBuilder builder, int start, int length)
        {
            var trLen = Math.Min(this.Trivia.Length, start + length) - start;
            if (trLen > 0)
            {
                builder.Append(this.Trivia, start, trLen);
                start -= Math.Min(start, trLen);
                length -= trLen;
            }
            else
            {
                start = -trLen;
            }

            var txLen = Math.Min(this.Text.Length, start + length) - start;
            if (txLen > 0)
            {
                builder.Append(this.Text, start, txLen);
            }
        }
    }

    public class SyntaxNode : SyntaxElement
    {
        public IReadOnlyList<SyntaxElement> Elements { get; }
        public override int Length { get; }

        public SyntaxNode(string kind, IReadOnlyList<SyntaxElement> elements, Diagnostic? diagnostic = null)
            : base(kind, diagnostic)
        {
            int offsetInParent = 0;

            this.Elements = elements.Select(e =>
            {
                if (e != null)
                {
                    var newElement = e.WithParent(this, offsetInParent);
                    offsetInParent += e.Length;
                    return newElement;
                }
                else
                {
                    return e!;
                }
            }).ToList();

            this.Length = offsetInParent;
        }

        public SyntaxNode(string Kind, IEnumerable<SyntaxElement> elements)
            : this(Kind, elements.ToArray())
        {
        }

        public SyntaxNode(string Kind, params SyntaxElement[] elements)
            : this(Kind, (IReadOnlyList<SyntaxElement>)elements)
        {
        }

        // TODO: should be the first non-null element
        public override int TextStart => this.Elements[0]!.TextStart;

        public SyntaxNode Update(IReadOnlyList<SyntaxElement> elements)
        {
            return this.Elements == elements ? this : new SyntaxNode(this.Kind, elements);
        }

        public override SyntaxElement Clone()
        {
            return new SyntaxNode(this.Kind, this.Elements.Select(e => e.Clone()).ToList());
        }

        internal override void WriteString(StringBuilder builder, int start, int length)
        {
            foreach (var element in this.Elements)
            {
                var len = Math.Min(element.Length, start + length) - start;               
                if (len > 0)
                {
                    element.WriteString(builder, start, len);
                    start -= Math.Min(start, len);
                    length -= len;
                }
                else
                {
                    start = -len;
                }

                if (length == 0)
                    break;
            }
        }
    }

    public class SyntaxRewriter
    {
        protected SyntaxElement Rewrite(SyntaxElement element)
        {
            if (element is SyntaxToken token)
            {
                return Rewrite(token);
            }
            else if (element is SyntaxNode node)
            {
                return Rewrite(node);
            }
            else
            {
                return null!;
            }
        }

        public virtual SyntaxElement Rewrite(SyntaxToken token)
        {
            return token;
        }

        public virtual SyntaxElement Rewrite(SyntaxNode node)
        {
            var newList = Rewrite(node.Elements);
            return node.Update(newList);
        }

        private IReadOnlyList<SyntaxElement> Rewrite(IReadOnlyList<SyntaxElement> list)
        {
            List<SyntaxElement> newList = null!;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var newItem = Rewrite(item);
                if (newItem != item || newList != null)
                {
                    if (newList == null)
                    {
                        newList = new List<SyntaxElement>();
                        for (int j = 0; j < i; j++)
                        {
                            newList.Add(list[j]);
                        }
                    }

                    newList.Add(newItem);
                }
            }

            return newList ?? list;
        }
    }
}