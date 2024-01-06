using Parkour;
using Parkour.Syntax;

namespace Tests;

public static class TestHelpers
{
    public static (string textWithoutMarker, int markerPosition) StripMarker(string textWithMarker, string marker = "$")
    {
        var index = textWithMarker.IndexOf(marker);
        if (index >= 0)
        {
            var textWithoutMarker = textWithMarker.Remove(index, marker.Length);
            return (textWithoutMarker, index);
        }
        else
        {
            return (textWithMarker, -1);
        }
    }

    public static IReadOnlyList<T> Concat<T>(params IEnumerable<T>[] inputs) =>
        inputs.SelectMany(x => x).ToArray();


    /// <summary>
    /// Compares syntax elements.
    /// </summary>
    public static void AssertEquals(SyntaxElement? expected, SyntaxElement? actual, bool trivia = false)
    {
        if (expected == null && actual == null)
            return;

        if (expected == null)
            Assert.IsNull(actual);
        if (expected != null)
            Assert.IsNotNull(actual);

        Assert.AreEqual(expected!.Kind, actual!.Kind, "syntax element kind");

        switch (expected)
        {
            case SyntaxToken etoken when actual is SyntaxToken atoken:
                if (trivia)
                {
                    Assert.AreEqual(etoken.Trivia, atoken.Trivia, "token trivia");
                    Assert.AreEqual(etoken.Start, atoken.Start, "token start");
                    Assert.AreEqual(etoken.Length, atoken.Length, "token length");
                }
                Assert.AreEqual(etoken.Text, atoken.Text, "token text");
                break;

            case SyntaxNode enode when actual is SyntaxNode anode:
                Assert.AreEqual(enode.ChildCount, anode.ChildCount, "child count");
                for (int i = 0; i < enode.ChildCount; i++)
                {
                    AssertEquals(enode.GetChild(i), anode.GetChild(i));
                }
                break;

            default:
                Assert.Fail($"expected type: {expected.GetType().Name} actual: {actual.GetType().Name}");
                break;
        }
    }
}