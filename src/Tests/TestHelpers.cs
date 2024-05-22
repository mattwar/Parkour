using System.Reflection;
using Parkour;
using Parkour.Semantics;
using Parkour.Syntax;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

public static class TestHelpers
{
    public static Expression Int32ArrayType = Int32Type.Array();
    public static Expression StringArrayType = StringType.Array();
    public static SymbolExpression ListTType = Symbol("System.Collections.Generic.List`1");
    public static Expression ListInt32Type = ListTType.Construct([Int32Type]);
    public static Expression ListStringType = ListTType.Construct([StringType]);
    public static SymbolExpression SystemCollectionsGenericNamespace = Symbol("System.Collections.Generic");

    /// <summary>
    /// Strips the marker from the text,
    /// returns the text without the marker and the position of the marker.
    /// </summary>
    public static (string textWithoutMarker, int markerPosition) 
        StripMarker(string textWithMarker, string marker = "$")
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
    public static void AssertSyntaxEquals(SyntaxElement? expected, SyntaxElement? actual, bool trivia = false)
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
                    AssertSyntaxEquals(enode.GetChild(i), anode.GetChild(i));
                }
                break;

            default:
                Assert.Fail($"expected type: {expected.GetType().Name} actual: {actual.GetType().Name}");
                break;
        }
    }

    public static void AssertAreEquivalent(object? expected, object? actual, string path = "")
    {
        if (expected == actual)
            return;

        if (expected == null && actual != null)
        {
            Assert.Fail($"{path}: expected: {expected} actual: {actual}");
        }
        else if (expected != null && actual == null)
        {
            Assert.Fail($"{path}: expected: {expected} actual: {actual}");
        }

        var expectedType = expected!.GetType();
        var actualType = actual!.GetType();

        if (expectedType != actualType)
        {
            Assert.Fail($"{path}: Type expected: {expectedType.Name} actual: {actualType.Name}");
        }

        if (expectedType.IsAssignableTo(typeof(IEquatable<>).MakeGenericType(expectedType)))
        {
            if (!object.Equals(expected, actual))
            {
                Assert.Fail($"{path}: expected: {expected} actual: {actual}");
            }
        }
        else if (expectedType.IsAssignableTo(typeof(System.Collections.IEnumerable)))
        {
            var expectedList = ((System.Collections.IEnumerable)expected).OfType<object>().ToList();
            var actualList = ((System.Collections.IEnumerable)actual).OfType<object>().ToList();

            if (actualList.Count != expectedList.Count)
            {
                Assert.Fail($"{path}: count expected: {expectedList.Count} actual: {actualList.Count}");
            }

            for (int i = 0; i < expectedList.Count; i++)
            {
                var expectedItem = expectedList[i];
                var actualItem = actualList[i];
                AssertAreEquivalent(expectedItem, actualItem, $"{path}[{i}]");
            }
        }
        else
        {
            var props = expectedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length == 0)
                {
                    var expectedPropValue = prop.GetValue(expected);
                    var actualPropValue = prop.GetValue(actual);
                    AssertAreEquivalent(expectedPropValue, actualPropValue, $"{path}.{prop.Name}");
                }
            }
        }
    }

}