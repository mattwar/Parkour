using System.Diagnostics.CodeAnalysis;

namespace Parkour.Utils
{
    internal struct TextKey : IEquatable<TextKey>
    {
        private readonly string _text;
        private readonly int _start;
        private readonly int _length;
        private readonly int _hashcode;

        public TextKey(string text, int start, int length)
        {
            _text = text;
            _start = start;
            _length = length;
            _hashcode = length > 0
                ? (int)text[start] + (int)text[start + length - 1] + length
                : 0;
        }

        public bool Equals(TextKey other)
        {
            return other._length == _length
                && string.Compare(_text, _start, other._text, other._start, _length) == 0;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is TextKey other && Equals(other);

        public override int GetHashCode() =>
            _hashcode;

        public static implicit operator TextKey(string text) =>
            new TextKey(text, 0, text.Length);

        public override string ToString() =>
            _text.Substring(_start, _length);
    }
}
