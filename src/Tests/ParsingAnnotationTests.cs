using Parkour;
using Parkour.Parsing;

using Tiny;

namespace Tests;
using static TestHelpers;

[TestClass]
public class ParsingAnnotationTests
{
    private static string AtStart = "<Expression>, 'not', '-', <IdentifierToken>, <NumberToken>, <StringToken>, '('";
    private static string AfterPrimary = "'*', '/', '+', '-', '>', '>=', '<', '<=', '==', '!=', 'and', 'or', <any>";
    private static string AfterBinaryOp = "'-', <IdentifierToken>, <NumberToken>, <StringToken>, '('";
    private static string AfterParenthesizedPrimary = "'*', '/', '+', '-', '>', '>=', '<', '<=', '==', '!=', 'and', 'or', ')'";

    [TestMethod]
    public void TestAnnotations_AtStart()
    {
        TestAnnotations("$", AtStart);
        TestAnnotations("$X", AtStart);
    }

    [TestMethod]
    public void TestAnnotations_AfterPrimary()
    {
        TestAnnotations("X$", AfterPrimary);
        TestAnnotations("X $", AfterPrimary);
        TestAnnotations("X $ Y", AfterPrimary);
    }

    [TestMethod]
    public void TestAnnotations_AfterMultiplicative()
    {
        TestAnnotations("X * $", AfterBinaryOp);
        TestAnnotations("X * X $", AfterPrimary);
    }

    [TestMethod]
    public void TestAnnotations_AfterAdditive()
    {
        TestAnnotations("X + $", AfterBinaryOp);
        TestAnnotations("X + X $", AfterPrimary);
    }

    [TestMethod]
    public void TestAnnotations_AfterParenthesizedPrimary()
    {
        TestAnnotations("(X$", AfterParenthesizedPrimary);
    }

    private void TestAnnotations(string textWithMarker, params string[] expectedTerms)
    {
        var (textWithoutMarker, markerPosition) = StripMarker(textWithMarker);
        var parser = new TinyParser();
        var tree = parser.Parse("test", textWithoutMarker);

        var actualTerms = new List<string>();
        tree.Annotations.GetAnnotations(markerPosition, null, actualTerms);

        var expectedList = string.Join(", ", expectedTerms);
        var actualList = string.Join(", ", actualTerms);

        Assert.AreEqual(expectedList, actualList, "next terms");
    }
}