using System;
using System.Collections.Generic;
using System.Text;
using Parkour.Semantics;

namespace Parkour.Text;

/// <summary>
/// An immutable string-like type that remembers the changes made to construct its current form,
/// and allow you to translate text positions between old and new forms.
/// </summary>
public sealed class EditString
{
    /// <summary>
    /// The text before any changes.
    /// </summary>
    public string OriginalText { get; }

    /// <summary>
    /// The text after all the changes.
    /// </summary>
    public string CurrentText { get; }

    /// <summary>
    /// The list of in-order, non-overlapping edits, each relative the the original text.
    /// </summary>
    private ImmutableList<RangeEdit> _changes;

    /// <summary>
    /// Constructs a new <see cref="EditString"/>.
    /// </summary>
    private EditString(string originalText, string currentText, ImmutableList<RangeEdit> changes)
    {
        this.OriginalText = originalText ?? "";
        this.CurrentText = currentText ?? "";
        _changes = changes;
    }

    /// <summary>
    /// Create a new <see cref="EditString"/> in a pre-edit state.
    /// </summary>
    public EditString(string text)
        : this(text, text, ImmutableList<RangeEdit>.Empty)
    {
    }

    /// <summary>
    /// An empty <see cref="EditString"/>
    /// </summary>
    public static readonly EditString Empty = new EditString("");

    /// <summary>
    /// The length of the current text.
    /// </summary>
    public int Length => this.CurrentText.Length;

    /// <summary>
    /// The character at the index of the current text.
    /// </summary>
    public char this[int index] => this.CurrentText[index];

    /// <summary>
    /// The starting index of the value within the current text.
    /// </summary>
    public int IndexOf(string value) => this.CurrentText.IndexOf(value);

    /// <summary>
    /// Converts a string into an <see cref="EditString"/> without any edits.
    /// </summary>
    public static implicit operator EditString(string text)
    {
        return new EditString(text);
    }

    /// <summary>
    /// Returns a list of parallel edits between the original and current text
    /// such that if applied to the original text (via ApplyAll) would produce the current text.
    /// This list of changes is not guaranteed to match the exact sequence of edits originally made to create the current text.
    /// </summary>
    public ParallelEdits GetChanges()
    {
        var textChanges = new List<TextEdit>();

        var delta = 0;
        foreach (var edit in _changes)
        {
            textChanges.Add(TextEdit.Replacement(edit.Start, edit.DeleteLength, this.CurrentText.Substring(edit.Start + delta, edit.InsertLength)));
            delta = delta - edit.DeleteLength + edit.InsertLength;
        }

        return new ParallelEdits(textChanges);
    }

    /// <summary>
    /// Returns the current text.
    /// </summary>
    public override string ToString() => this.CurrentText;

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the range of characters replaced with the specified text.
    /// </summary>
    public EditString ReplaceAt(int start, int length, string text)
    {
        return Apply(TextEdit.Replacement(start, length, text));
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the text inserted as the position.
    /// </summary>
    public EditString Insert(int position, string text)
    {
        return ReplaceAt(position, 0, text);
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the text appended to the end.
    /// </summary>
    public EditString Append(string text)
    {
        return ReplaceAt(this.CurrentText.Length, 0, text);
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the text prepending to the start.
    /// </summary>
    public EditString Prepend(string text)
    {
        return ReplaceAt(0, 0, text);
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the range of characters removed.
    /// </summary>
    public EditString Remove(int start, int length)
    {
        return ReplaceAt(start, length, "");
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> containing only the range of characters.
    /// </summary>
    public EditString Substring(int start, int length)
    {
        var newText = this.CurrentText.Substring(start, length);

        var newEdits = new List<RangeEdit>(2);

        var endDeleteStart = start + length;
        var endDeleteLength = this.CurrentText.Length - endDeleteStart;

        if (start > 0)
        {
            // remove first 'start' characters
            newEdits.Add(new RangeEdit(0, start, 0));
        }

        if (endDeleteStart > 0)
        {
            // remove last 'endDeleteLength' characters
            newEdits.Add(new RangeEdit(endDeleteStart, endDeleteLength, 0));
        }

        return ApplyRangeEdits(newText, newEdits.ToImmutableList());
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> containing only the characters from the start position until the end.
    /// </summary>
    public EditString Substring(int start) =>
        Substring(start, this.CurrentText.Length - start);

    /// <summary>
    /// Trims the whitespace from the start of the string.
    /// </summary>
    public EditString TrimLeft()
    {
        var len = TextFacts.GetWhitespaceCount(this.CurrentText, 0);
        if (len > 0)
            return this.Remove(0, len);
        return this;
    }

    /// <summary>
    /// Trims the whitespace from the end of the string.
    /// </summary>
    public EditString TrimRight()
    {
        var len = TextFacts.GetWhitespaceCountBefore(this.CurrentText, this.CurrentText.Length);
        if (len > 0)
            return this.Remove(this.CurrentText.Length - len, len);
        return this;
    }

    /// <summary>
    /// Trims the whitespace from the start and end of the string.
    /// </summary>
    public EditString Trim()
    {
        return this.TrimLeft().TrimRight();
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with all occurrances of the old text replaced with the new text.
    /// </summary>
    public EditString Replace(string oldText, string newText)
    {
        if (oldText == null)
            throw new ArgumentNullException(nameof(oldText));

        if (newText == null)
            throw new ArgumentNullException(nameof(newText));

        var newCurrentText = this.CurrentText.Replace(oldText, newText);

        // if nothing changed, return same EditString
        if (newCurrentText == this.CurrentText)
            return this;

        int startIndex = 0;
        var newEdits = new List<RangeEdit>();

        while (true)
        {
            int oldValueStart = this.CurrentText.IndexOf(oldText, startIndex);
            if (oldValueStart < startIndex)
                break;

            newEdits.Add(new RangeEdit(oldValueStart, oldText.Length, newText.Length));

            startIndex = oldValueStart + oldText.Length;
        }

        return ApplyRangeEdits(newCurrentText, newEdits.ToImmutableList());
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the single edit applied.
    /// </summary>
    public EditString Apply(TextEdit edit)
    {
        if (edit.Start < 0 || edit.Start > this.Length + 1)
            throw new ArgumentOutOfRangeException(nameof(edit.Start));

        if (edit.Start + edit.DeleteLength > this.Length + 1)
            throw new ArgumentOutOfRangeException(nameof(edit.DeleteLength));

        var newText = this.CurrentText;

        if (edit.DeleteLength > 0)
        {
            newText = newText.Remove(edit.Start, edit.DeleteLength);
        }

        if (edit.InsertText.Length > 0)
        {
            newText = newText.Insert(edit.Start, edit.InsertText);
        }

        // only apply edit if it actually makes a change.
        if (edit.InsertText.Length != edit.DeleteLength
            || string.Compare(this.CurrentText, edit.Start, edit.InsertText, 0, edit.InsertText.Length) != 0)
        {
            return ApplyRangeEdits(newText, ImmutableList<RangeEdit>.Empty.Add(new RangeEdit(edit.Start, edit.DeleteLength, edit.InsertText.Length)));
        }
        else
        {
            return new EditString(this.OriginalText, newText, _changes);
        }
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the parallel edits applied.
    /// </summary>
    public EditString Apply(ParallelEdits parallelEdits)
    {
        var edits = parallelEdits.Edits;

        if (edits == null || edits.Count == 0)
            return this;

        var newText = parallelEdits.ApplyTo(this.CurrentText);
        var newRangeEdits = edits.Select(e => new RangeEdit(e.Start, e.DeleteLength, e.InsertText.Length)).ToImmutableList();

        return ApplyRangeEdits(newText, newRangeEdits);
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the sequential edits applied.
    /// </summary>
    public EditString Apply(SequentialEdits sequentialEdits)
    {
        var es = this;

        foreach (var edit in sequentialEdits.Edits)
        {
            es = es.Apply(edit);           
        }

        return es;
    }

    /// <summary>
    /// Applies the edits all at once, as a single change.
    /// Throws if any edit overlaps with another edit or any edit is out of bounds relative to the current text.
    /// </summary>
    public EditString ApplyParallel(IEnumerable<TextEdit> edits)
    {
        return Apply(new ParallelEdits(edits));
    }

    /// <summary>
    /// Applies the edits, one at a time, in order.
    /// Throws if an edit is out of bounds relative to the text resulting from all the prior edits.
    /// </summary>
    public EditString ApplySequential(IEnumerable<TextEdit> edits)
    {
        return Apply(new SequentialEdits(edits));
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> containing both old and new edits.
    /// </summary>
    private EditString ApplyRangeEdits(string newText, ImmutableList<RangeEdit> rangeEdits)
    {
        var combinedEdits = CombineEdits(_changes, rangeEdits);
        return new EditString(this.OriginalText, newText, combinedEdits);
    }

    /// <summary>
    /// Combines a list of old edits with a list of new edits.
    /// The new edits' positions are relative to after the old edits have been applied.
    /// </summary>
    private static ImmutableList<RangeEdit> CombineEdits(ImmutableList<RangeEdit> oldEdits, ImmutableList<RangeEdit> newEdits)
    {
        if (newEdits.Count == 0)
            return oldEdits;

        var oldDelta = 0;

        var nextOldIndex = 1;
        var nextNewIndex = 1;
        var hasOldEdit = oldEdits.Count > 0;
        var hasNewEdit = newEdits.Count > 0;
        var oldEdit = hasOldEdit ? oldEdits[0] : default;
        var newEdit = hasNewEdit ? newEdits[0] : default;

        var combinedEdits = ImmutableList<RangeEdit>.Empty.ToBuilder();

        while (hasOldEdit && hasNewEdit)
        {
            if (oldEdit.DeleteLength == 0 && oldEdit.InsertLength == 0)
            {
                // there is no actual old edit here, so go to next old edit.
                if (hasOldEdit = nextOldIndex < oldEdits.Count)
                {
                    oldEdit = oldEdits[nextOldIndex];
                    nextOldIndex++;
                    continue;
                }
                break;
            }
            else if (newEdit.DeleteLength == 0 && newEdit.InsertLength == 0)
            {
                // there is no actual new edit here, go to the next new edit
                if (hasNewEdit = nextNewIndex < newEdits.Count)
                {
                    newEdit = newEdits[nextNewIndex];
                    nextNewIndex++;
                    continue;
                }
                break;
            }
            else if (newEdit.DeleteEnd <= oldEdit.Start + oldDelta)
            {
                // new edit is entirely before the old edit, so this where the new edit belongs

                // add adjusted new edit
                combinedEdits.Add(new RangeEdit(newEdit.Start - oldDelta, newEdit.DeleteLength, newEdit.InsertLength));

                // go to the next new edit
                if (hasNewEdit = nextNewIndex < newEdits.Count)
                {
                    newEdit = newEdits[nextNewIndex];
                    nextNewIndex++;
                    continue;
                }
                break;
            }
            else if (newEdit.Start >= oldEdit.InsertEnd + oldDelta)
            {
                // new edit is entirely after the old edit, so go to next old edit and try again
                combinedEdits.Add(oldEdit);
                oldDelta = oldDelta - oldEdit.DeleteLength + oldEdit.InsertLength;

                if (hasOldEdit = nextOldIndex < oldEdits.Count)
                {
                    oldEdit = oldEdits[nextOldIndex];
                    nextOldIndex++;
                    continue;
                }
                break;
            }
            else if (newEdit.Start < oldEdit.Start + oldDelta)
            {
                // new edit starts before the old edit but the new edit deletion overlaps with the old edit insertion
                //var partialDeleteLength = oldEdit.Start - newEdit.Start;
                var partialDeleteLength = (oldEdit.Start + oldDelta) - newEdit.Start;

                // add the portion of the delete before the overlap
                combinedEdits.Add(new RangeEdit(newEdit.Start - oldDelta, partialDeleteLength, 0));

                // adjust new edit to coincide with old edit with the remaining delete
                newEdit = new RangeEdit(oldEdit.Start + oldDelta, newEdit.DeleteLength - partialDeleteLength, newEdit.InsertLength);
                continue;
            }
            else if (newEdit.Start > oldEdit.Start + oldDelta)
            {
                // new edit starts after old edit, but overlaps
                // split up old edit around new edit start and try again
                var partialInsertLength = newEdit.Start - (oldEdit.Start + oldDelta);
                var partialDeleteLength = Math.Min(oldEdit.DeleteLength, partialInsertLength);

                // add the old edits partial delete & insert
                combinedEdits.Add(new RangeEdit(oldEdit.Start, partialDeleteLength, partialInsertLength));
                oldDelta = oldDelta - partialDeleteLength + partialInsertLength;

                // adjust old edit now coincide with new edit with the remaining delete & insert
                oldEdit = new RangeEdit(newEdit.Start - oldDelta, oldEdit.DeleteLength - partialDeleteLength, oldEdit.InsertLength - partialInsertLength);
                continue;
            }
            // otherwise both edits start at the same position
            else if (newEdit.DeleteLength <= oldEdit.InsertLength)
            {
                // new edit deletes less than old edit inserts

                // adjust old edit to insert less and insert new edit w/deletes before it.
                oldEdit = new RangeEdit(oldEdit.Start, oldEdit.DeleteLength, oldEdit.InsertLength - newEdit.DeleteLength);
                oldDelta = oldDelta + newEdit.DeleteLength;

                // add the remaining portion of the new edit here with only the insert part
                combinedEdits.Add(new RangeEdit(newEdit.DeleteEnd - oldDelta, 0, newEdit.InsertLength));

                // go to next new edit
                if (hasNewEdit = nextNewIndex < newEdits.Count)
                {
                    newEdit = newEdits[nextNewIndex];
                    nextNewIndex++;
                    continue;
                }
                break;
            }
            else
            {
                // new edit deletes more than old edit inserts

                // adjust beyond this old edit
                oldDelta = oldDelta - oldEdit.DeleteLength + oldEdit.InsertLength;

                // adjust new edit to delete less (and subsume this old edit)
                var newDeletion = newEdit.DeleteLength + oldEdit.DeleteLength - oldEdit.InsertLength;
                newEdit = new RangeEdit(oldEdit.Start + oldDelta, newDeletion, newEdit.InsertLength);

                // go to next old edit
                if (hasOldEdit = nextOldIndex < oldEdits.Count)
                {
                    oldEdit = oldEdits[nextOldIndex];
                    nextOldIndex++;
                    continue;
                }
                break;
            }
        }

        // add remaining new edits
        if (hasNewEdit)
        {
            // add adjusted new edit
            combinedEdits.Add(new RangeEdit(newEdit.Start - oldDelta, newEdit.DeleteLength, newEdit.InsertLength));
        }

        for (; nextNewIndex < newEdits.Count; nextNewIndex++)
        {
            newEdit = newEdits[nextNewIndex];
            // add adjusted new edit
            combinedEdits.Add(new RangeEdit(newEdit.Start - oldDelta, newEdit.DeleteLength, newEdit.InsertLength));
        }

        // add remaining old edits
        if (hasOldEdit)
        {
            combinedEdits.Add(oldEdit);
        }

        for (; nextOldIndex < oldEdits.Count; nextOldIndex++)
        {
            combinedEdits.Add(oldEdits[nextOldIndex]);
        }

        CombineAdjacentEdits(combinedEdits);

        return combinedEdits.ToImmutable();
    }

    private static void CombineAdjacentEdits(ImmutableList<RangeEdit>.Builder edits)
    {
        for (int i = edits.Count - 1; i >= 1; i--)
        {
            var edit = edits[i];
            var prevEdit = edits[i - 1];
            if (prevEdit.DeleteEnd == edit.Start)
            {
                edits[i - 1] = new RangeEdit(prevEdit.Start, prevEdit.DeleteLength + edit.DeleteLength, prevEdit.InsertLength + edit.InsertLength);
                edits.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// An edit of a text range (no text involved).
    /// Used to represent edits affecting the positioning of text.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Start: {Start}, Delete: {DeleteLength}, Insert: {InsertLength}")]
    private struct RangeEdit
    {
        public int Start { get; }
        public int DeleteLength { get; }
        public int InsertLength { get; }

        public int DeleteEnd => Start + DeleteLength;
        public int InsertEnd => Start + InsertLength;

        public RangeEdit(int start, int deleteLength, int insertLength)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start", "negative start position");
            if (deleteLength < 0)
                throw new ArgumentOutOfRangeException("deleteLength", "negative deletion length");
            if (insertLength < 0)
                throw new ArgumentOutOfRangeException("insertLength", "negative insertion length");

            this.Start = start;
            this.DeleteLength = deleteLength;
            this.InsertLength = insertLength;
        }
    }

    /// <summary>
    /// Gets the position in the current text corresponding to the position in the original text.
    /// If original position corresponds to a region of text that was removed or replaced, it will return the position 
    /// at the start of where the change occurred.
    /// </summary>
    public int GetCurrentPosition(int originalPosition, PositionBias bias = PositionBias.Right)
    {
        int currentPosition = originalPosition;
        int delta = 0;

        for (int i = 0; i < _changes.Count; i++)
        {
            var edit = _changes[i];

            if (currentPosition < edit.Start + delta)
                break;

            currentPosition = Translate(
                currentPosition,
                edit.Start + delta,
                edit.DeleteLength,
                edit.InsertLength,
                bias);

            delta = delta - edit.DeleteLength + edit.InsertLength;
        }

        return currentPosition;
    }

    /// <summary>
    /// Gets the current range start and length for the given original range start and length.
    /// </summary>
    public void GetCurrentRange(int originalStart, int originalLength, out int currentStart, out int currentLength)
    {
        var originalEnd = originalStart + originalLength;
        currentStart = GetCurrentPosition(originalStart, PositionBias.Right);
        var currentEnd = GetCurrentPosition(originalEnd, PositionBias.Left);
        currentLength = currentEnd - currentStart;
    }

    /// <summary>
    /// Gets the position in the original text corresponding to the position in the current text.
    /// If current position corresponds to a region of the text that was inserted, it will return the position
    /// at the start of where change occurred.
    /// </summary>
    public int GetOriginalPosition(int currentPosition, PositionBias bias = PositionBias.Right)
    {
        int originalPosition = currentPosition;
        int delta = 0;

        for (int i = 0; i < _changes.Count; i++)
        {
            var edit = _changes[i];

            if (originalPosition < edit.Start + delta)
                break;

            // since we are doing the reverse operation
            // deletes are inserts and inserts are deletes
            originalPosition = Translate(
                position: originalPosition,
                start: edit.Start + delta,
                deleteLength: edit.InsertLength,
                insertLength: edit.DeleteLength,
                bias: bias);

            //delta = delta - edit.InsertLength + edit.DeleteLength;
        }

        return originalPosition;
    }

    /// <summary>
    /// Translate a position across an edit.
    /// </summary>
    private static int Translate(int position, int start, int deleteLength, int insertLength, PositionBias bias)
    {
        if (deleteLength > 0 && position > start)
        {
            if (position > start + deleteLength)
            {
                // after deleted range, adjust downward
                position -= deleteLength;
            }
            else
            {
                // inside deleted range, adjust to start
                position = start;
            }
        }

        if (insertLength > 0)
        {
            // if after the insert position, adjust upward
            // or at the insert position and bias is toward right, also adjust upward
            if (position > start || (bias == PositionBias.Right && position == start))
                position += insertLength;
        }

        return position;
    }

    /// <summary>
    /// Gets the smallest range within the current text encompassing all of the changes.
    /// </summary>
    public TextRange GetChangeRange()
    {
        if (_changes.Count > 0)
        {
            var firstStart = GetCurrentPosition(_changes[0].Start, PositionBias.Left);
            var lastEnd = GetCurrentPosition(_changes[_changes.Count - 1].Start, PositionBias.Right);
            return TextRange.FromBounds(firstStart, lastEnd);
        }
        else
        {
            return TextRange.Empty;
        }
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with the blank lines removed.
    /// </summary>
    public EditString RemoveBlankLines()
    {
        var pos = 0;
        var text = this.CurrentText;

        ImmutableList<TextEdit>.Builder? edits = null;

        while (pos < text.Length)
        {
            var len = TextFacts.GetLineLength(text, pos, includeLineBreak: true);

            if (TextFacts.IsBlankLine(text, pos))
            {
                if (edits == null)
                    edits = ImmutableList<TextEdit>.Empty.ToBuilder();
                edits.Add(TextEdit.Deletion(pos, len));
            }

            pos += len;
        }

        return (edits != null)
            ? this.Apply(new ParallelEdits(edits))
            : this;
    }

    /// <summary>
    /// Returns a new <see cref="EditString"/> with all the line breaks replaced with the specied value.
    /// </summary>
    public EditString ReplaceLineBreaks(string newValue)
    {
        if (newValue == null)
            throw new ArgumentNullException(nameof(newValue));

        var text = this.CurrentText;
        ImmutableList<TextEdit>.Builder? edits = null;

        for (int i = 0; i < text.Length; i++)
        {
            if (TextFacts.IsLineBreakStart(text[i]))
            {
                var len = TextFacts.GetLineBreakLength(text, i);

                // only make the edit if the newValue is different than the existing line break
                if (len != newValue.Length || string.Compare(text, i, newValue, 0, len) != 0)
                {
                    if (edits == null)
                        edits = ImmutableList<TextEdit>.Empty.ToBuilder();
                    edits.Add(TextEdit.Replacement(i, len, newValue));
                }

                // add one less because for-loop will add one back
                i += len - 1;
            }
        }

        if (edits != null)
            return this.Apply(new ParallelEdits(edits));
        return this;
    }
}

/// <summary>
/// A set of non-overlapping edits all relative to the same original text.
/// </summary>
public struct ParallelEdits
{
    /// <summary>
    /// The parallel edits
    /// </summary>
    public ImmutableList<TextEdit> Edits { get; }

    public ParallelEdits(IEnumerable<TextEdit> edits)
    {
        var imEdits = edits.ToImmutableList();
        var orderedEdits = GetOrderedEdits(imEdits);
        if (HasOverlap(orderedEdits))
            throw new InvalidOperationException("Invalid: at least two edits overlap each other");
        this.Edits = orderedEdits;
    }

    /// <summary>
    /// Convert to sequential edits.
    /// </summary>
    public SequentialEdits ToSequential()
    {
        var seqentialEdits = new List<TextEdit>();

        var delta = 0;
        foreach (var edit in this.Edits)
        {
            seqentialEdits.Add(TextEdit.Replacement(edit.Start + delta, edit.DeleteLength, edit.InsertText));
            delta = delta - edit.DeleteLength + edit.InsertText.Length;
        }

        return new SequentialEdits(seqentialEdits);
    }

    /// <summary>
    /// Returns true if at least two edits overlap each other.
    /// </summary>
    public static bool HasOverlap(ImmutableList<TextEdit> edits)
    {
        return HasOverlap_OrderedEdits(GetOrderedEdits(edits));
    }

    /// <summary>
    /// Returns true if at least two edits overlap each other.
    /// </summary>
    private static bool HasOverlap_OrderedEdits(ImmutableList<TextEdit> edits)
    {
        // check for overlapping or out of bounds
        var priorEnd = 0;

        foreach (var edit in edits)
        {
            if (edit.Start < priorEnd)
            {
                return true;
            }

            priorEnd = edit.Start + edit.DeleteLength;
        }

        return false;
    }

    /// <summary>
    /// Returns the list of edits in order of start position.
    /// </summary>
    private static ImmutableList<TextEdit> GetOrderedEdits(ImmutableList<TextEdit> edits)
    {
        if (!IsInOrder(edits))
        {
            // OrderBy is a stable sort
            edits = edits.OrderBy(e => e.Start).ToImmutableList();
        }

        return edits;
    }

    /// <summary>
    /// Returns true if the list of edits is already in order.
    /// </summary>
    private static bool IsInOrder(IReadOnlyList<TextEdit> edits)
    {
        var lastStart = 0;

        foreach (var edit in edits)
        {
            if (edit.Start < lastStart)
                return false;

            lastStart = edit.Start;
        }

        return true;
    }

    /// <summary>
    /// Apply the edits to the text.
    /// </summary>
    public string ApplyTo(string text)
    {
        if (!CanApplyTo(text))
            throw new InvalidOperationException("At least one edit is out of bounds of the text.");

        var builder = new StringBuilder();

        // the end position of the last edit in the newest text. 
        var priorEnd = 0;

        // construct the new current text and the new list of edits
        foreach (var edit in this.Edits)
        {
            if (edit.Start > priorEnd)
            {
                // append anything between this point and the edit start
                builder.Append(text, priorEnd, edit.Start - priorEnd);
            }

            priorEnd = edit.Start + edit.DeleteLength;

            if (edit.InsertText.Length > 0)
            {
                builder.Append(edit.InsertText);
            }
        }

        // add any remaining text
        if (priorEnd < text.Length)
        {
            builder.Append(text, priorEnd, text.Length - priorEnd);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Return true if the list of edits can be applied via ApplyAll.
    /// Returns false if the edits are overlapping or out of bounds of the current text.
    /// </summary>
    public bool CanApplyTo(string text)
    {
        // check for overlapping or out of bounds
        var priorEnd = 0;

        foreach (var edit in this.Edits)
        {
            if (edit.Start > text.Length || edit.Start + edit.DeleteLength > text.Length)
            {
                return false;
            }

            priorEnd = edit.Start + edit.DeleteLength;
        }

        return true;
    }
}

/// <summary>
/// A sequence of edits, each relative to the text produced by applying all the prior edits.
/// </summary>
public struct SequentialEdits
{
    /// <summary>
    /// The sequential edits
    /// </summary>
    public ImmutableList<TextEdit> Edits { get; }

    public SequentialEdits(IEnumerable<TextEdit> edits)
    {       
        this.Edits = edits.ToImmutableList();
    }

    public string ApplyTo(string text)
    {
        var es = new EditString(text);
        foreach (var edit in this.Edits)
        {
            es = es.Apply(edit);
        }
        return es.CurrentText;
    }

    public static implicit operator SequentialEdits(TextEdit edit) => 
        new SequentialEdits([edit]);
}