using Parkour;
using Parkour.Text;

namespace Tests;
using static TestHelpers;

[TestClass]
public class TextTests
{
	[TestMethod]
	public void Constructor_InitializesTextWithoutChanges()
	{
		var text = new EditString("hello");

		Assert.AreEqual("hello", text.OriginalText);
		Assert.AreEqual("hello", text.CurrentText);
		Assert.AreEqual(5, text.Length);
		Assert.AreEqual('e', text[1]);
		Assert.AreEqual(1, text.IndexOf("e"));
		Assert.AreEqual("hello", text.ToString());
		Assert.AreEqual(TextRange.Empty, text.GetChangeRange());
	}

	[TestMethod]
	public void EmptyAndImplicitConversion_AreSupported()
	{
		EditString converted = "hello";

		Assert.AreEqual("", EditString.Empty.CurrentText);
		Assert.AreEqual("hello", converted.CurrentText);
	}

	[TestMethod]
	public void Insert_HandlesBeginningMiddleAndEnd()
	{
		var text = new EditString("abcd");

		Assert.AreEqual("Xabcd", text.Insert(0, "X").CurrentText);
		Assert.AreEqual("abXYcd", text.Insert(2, "XY").CurrentText);
		Assert.AreEqual("abcd!", text.Insert(text.Length, "!").CurrentText);
	}

	[TestMethod]
	public void AppendPrependAndReplaceAt_ChangeCurrentText()
	{
		var text = new EditString("hello");

		Assert.AreEqual("hello world", text.Append(" world").CurrentText);
		Assert.AreEqual("say hello", text.Prepend("say ").CurrentText);
		Assert.AreEqual("heLLo", text.ReplaceAt(2, 2, "LL").CurrentText);
	}

	[TestMethod]
	public void RemoveAndSubstring_ReturnExpectedText()
	{
		var text = new EditString("hello");

		Assert.AreEqual("ho", text.Remove(1, 3).CurrentText);
		Assert.AreEqual("ell", text.Substring(1, 3).CurrentText);
		Assert.AreEqual("llo", text.Substring(2).CurrentText);
		Assert.AreEqual("", text.Remove(0, text.Length).CurrentText);
	}

	[TestMethod]
	public void EditOperations_DoNotMutateOriginal()
	{
		var original = new EditString("abcd");

		var changed = original.Insert(2, "XY");

		Assert.AreEqual("abcd", original.CurrentText);
		Assert.AreEqual("abcd", changed.OriginalText);
		Assert.AreEqual("abXYcd", changed.CurrentText);
	}

	[TestMethod]
	public void Replace_ReplacesAllMatchesAndLeavesNoMatchUnchanged()
	{
		var text = new EditString("abracadabra");

		Assert.AreEqual("xbrxcxdxbrx", text.Replace("a", "x").CurrentText);
		Assert.AreEqual("abracadabra", text.Replace("missing", "x").CurrentText);
	}

	[TestMethod]
	public void Trim_RemovesWhitespaceFromExpectedSides()
	{
		var text = new EditString("  hello  ");

		Assert.AreEqual("hello", text.Trim().CurrentText);
		Assert.AreEqual("hello  ", text.TrimLeft().CurrentText);
		Assert.AreEqual("  hello", text.TrimRight().CurrentText);
	}

	[TestMethod]
	public void ReplaceLineBreaks_NormalizesLineEndings()
	{
		var text = new EditString("a\r\nb\nc\rd");

		Assert.AreEqual("a\nb\nc\nd", text.ReplaceLineBreaks("\n").CurrentText);
	}

	[TestMethod]
	public void RemoveBlankLines_RemovesBlankLines()
	{
		var text = new EditString("a\n\nb\n\n\nc");

		Assert.AreEqual("a\nb\nc", text.RemoveBlankLines().CurrentText);
	}

	[TestMethod]
	public void PositionMapping_TracksInsertionWithBias()
	{
		var text = new EditString("abcd").Insert(2, "XY");

		Assert.AreEqual(4, text.GetCurrentPosition(2, PositionBias.Right));
		Assert.AreEqual(2, text.GetCurrentPosition(2, PositionBias.Left));
		Assert.AreEqual(2, text.GetOriginalPosition(3));
	}

	[TestMethod]
	public void PositionMapping_TracksDeletionAndRanges()
	{
		var text = new EditString("abcdef").Remove(2, 2);

		Assert.AreEqual(2, text.GetCurrentPosition(3));
		Assert.AreEqual(2, text.GetCurrentPosition(4));

		text.GetCurrentRange(1, 4, out var currentStart, out var currentLength);
		Assert.AreEqual(1, currentStart);
		Assert.AreEqual(2, currentLength);
	}

	[TestMethod]
	public void GetChangeRange_ReturnsChangedCurrentRange()
	{
		var text = new EditString("abcdef").ReplaceAt(2, 2, "XYZ");

		Assert.AreEqual(new TextRange(2, 3), text.GetChangeRange());
	}

	[TestMethod]
	public void Apply_SupportsInsertionDeletionAndReplacement()
	{
		var text = new EditString("abcd");

		Assert.AreEqual("aXbcd", text.Apply(TextEdit.Insertion(1, "X")).CurrentText);
		Assert.AreEqual("acd", text.Apply(TextEdit.Deletion(1, 1)).CurrentText);
		Assert.AreEqual("aXYd", text.Apply(TextEdit.Replacement(1, 2, "XY")).CurrentText);
	}

	[TestMethod]
	public void ApplyParallel_AppliesNonOverlappingEditsInOrder()
	{
		var text = new EditString("abcdef");
		var result = text.ApplyParallel([
			TextEdit.Deletion(4, 1),
			TextEdit.Replacement(0, 1, "A")
		]);

		Assert.AreEqual("Abcdf", result.CurrentText);
	}

	[TestMethod]
	public void ApplyParallel_AppliesMultipleUnorderedNonOverlappingEdits()
	{
		var text = new EditString("0123456789");
		var result = text.ApplyParallel([
			TextEdit.Replacement(8, 1, "I"),
			TextEdit.Insertion(5, "X"),
			TextEdit.Deletion(2, 2),
			TextEdit.Replacement(0, 1, "A")
		]);

		Assert.AreEqual("A14X567I9", result.CurrentText);
	}

	[TestMethod]
	public void ApplySequential_AppliesEachEditToThePreviousResult()
	{
		var text = new EditString("cd");
		var result = text.ApplySequential([
			TextEdit.Insertion(0, "A"),
			TextEdit.Insertion(1, "B")
		]);

		Assert.AreEqual("ABcd", result.CurrentText);
	}

	[TestMethod]
	public void GetChanges_RecreatesCurrentText()
	{
		var text = new EditString("hello world")
			.Insert(5, ",")
			.Replace("world", "Parkour");

		Assert.AreEqual(text.CurrentText, text.GetChanges().ApplyTo(text.OriginalText));
	}

	[TestMethod]
	public void InvalidSingleEdits_Throw()
	{
		var text = new EditString("abc");

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => text.Apply(TextEdit.Deletion(4, 1)));
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => text.Apply(TextEdit.Deletion(1, 5)));
	}

	[TestMethod]
	public void InvalidParallelEdits_Throw()
	{
		var text = new EditString("abc");

		Assert.ThrowsException<InvalidOperationException>(() => text.ApplyParallel([
			TextEdit.Deletion(0, 2),
			TextEdit.Deletion(1, 2)
		]));
		Assert.ThrowsException<InvalidOperationException>(() => text.ApplyParallel([
			TextEdit.Deletion(4, 1)
		]));
		Assert.ThrowsException<InvalidOperationException>(() => text.ApplyParallel([
			TextEdit.Deletion(2, 2)
		]));

		Assert.AreEqual("abc!", text.ApplyParallel([
			TextEdit.Insertion(text.Length, "!")
		]).CurrentText);
	}
}
