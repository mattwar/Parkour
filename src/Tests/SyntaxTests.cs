using Parkour;
using Tiny;

namespace Tests;
using static TestHelpers;

[TestClass]
public class SyntaxTests
{
    private static string AtStart = "<Expression>, 'not', '-', <IdentifierToken>, <NumberToken>, <StringToken>, '('";
    private static string AfterPrimary = "'*', '/', '+', '-', '>', '>=', '<', '<=', '==', '!=', 'and', 'or', <any>";
    private static string AfterBinaryOp = "'-', <IdentifierToken>, <NumberToken>, <StringToken>, '('";
    private static string AfterParenthesizedPrimary = "'*', '/', '+', '-', '>', '>=', '<', '<=', '==', '!=', 'and', 'or', ')'";

    [TestMethod]
    public void TestNextTerms_AtStart()
    {
        TestNextTerms("$", AtStart);
        TestNextTerms("$X", AtStart);
    }

    [TestMethod]
    public void TestNextTerms_AfterPrimary()
    {
        TestNextTerms("X$", AfterPrimary);
        TestNextTerms("X $", AfterPrimary);
        TestNextTerms("X $ Y", AfterPrimary);
    }

    [TestMethod]
    public void TestNextTerms_AfterMultiplicative()
    {
        TestNextTerms("X * $", AfterBinaryOp);
        TestNextTerms("X * X $", AfterPrimary);
    }

    [TestMethod]
    public void TestNextTerms_AfterAdditive()
    {
        TestNextTerms("X + $", AfterBinaryOp);
        TestNextTerms("X + X $", AfterPrimary);
    }

    [TestMethod]
    public void TestNextTerms_AfterParenthesizedPrimary()
    {
        TestNextTerms("(X$", AfterParenthesizedPrimary);
    }

    private void TestNextTerms(string textWithMarker, params string[] expectedTerms)
    {
        var (textWithoutMarker, markerPosition) = StripMarker(textWithMarker);
        var parser = new TinyParser();
        var syntax = parser.Parse("test", textWithoutMarker);
        var actualTerms = syntax.GetNextTermsAt(markerPosition);

        var expectedList = string.Join(", ", expectedTerms);
        var actualList = string.Join(", ", actualTerms);

        Assert.AreEqual(expectedList, actualList, "next terms");
    }
}