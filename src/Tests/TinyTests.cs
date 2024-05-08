using Parkour;
using Parkour.Parsing;
using Parkour.Reflection;
using Parkour.Services;
using Parkour.Syntax;
using Tiny;
using static Tests.TestHelpers;
using static Tiny.TinyFactory;

namespace Tests;

[TestClass]
public class TinyTests
{
    [TestMethod]
    public void TestLexer()
    {
        TestLexer("",
            new LexicalToken(TinyTokenKinds.EndOfTextToken, "", ""));

        TestLexer("  ",
            new LexicalToken(TinyTokenKinds.EndOfTextToken, "  ", ""));

        TestLexer("ABC",
            new LexicalToken(TinyTokenKinds.IdentifierToken, "", "ABC"),
            new LexicalToken(TinyTokenKinds.EndOfTextToken, "", ""));

        TestLexer("  ABC ",
            new LexicalToken(TinyTokenKinds.IdentifierToken, "  ", "ABC"),
            new LexicalToken(TinyTokenKinds.EndOfTextToken, " ", ""));

        TestLexer("ABC + 123",
            new LexicalToken(TinyTokenKinds.IdentifierToken, "", "ABC"),
            new LexicalToken(TinyTokenKinds.PlusToken, " ", "+"),
            new LexicalToken(TinyTokenKinds.NumberToken, " ", "123"),
            new LexicalToken(TinyTokenKinds.EndOfTextToken, "", ""));
    }

    public void TestLexer(string tiny, params LexicalToken[] expectedTokens)
    {
        var actualTokens = new TinyLexer().Parse(tiny);

        Assert.AreEqual(expectedTokens.Length, actualTokens.Length, "Tokens.Length");

        for (int i = 0; i < expectedTokens.Length; i++)
        {
            Assert.AreEqual(expectedTokens[i].Kind, actualTokens[i].Kind, $"Tokens[{i}].Kind");
            Assert.AreEqual(expectedTokens[i].Trivia, actualTokens[i].Trivia, $"Tokens[{i}].Trivia");
            Assert.AreEqual(expectedTokens[i].Text, actualTokens[i].Text, $"Tokens[{i}].Text");
        }
    }

    [TestMethod]
    public void TestSyntax()
    {
        TestSyntax("ABC", Root(Identifier("ABC")));
        TestSyntax("A + B", Root(Add(Identifier("A"), PlusToken(), Identifier("B"))));
        TestSyntax("A * B + C", Root(Add(Multiply(Identifier("A"), AsteriskToken(), Identifier("B")), PlusToken(), Identifier("C"))));
    }

    public void TestSyntax(string text, SyntaxElement expected, bool trivia = false)
    {
        var tree = new TinyParser().Parse("test", text);
        var root = tree.Root;
        AssertSyntaxEquals(expected, root);
    }

    [TestMethod]
    public void TestClassifications()
    {
        TestClassification("ABC", [
            ClassificationKinds.Name
            ]);

        TestClassification("'ABC'", [
            ClassificationKinds.StringLiteral
            ]);

        TestClassification("123", [
            ClassificationKinds.NumericLiteral
            ]);

        TestClassification("A + B", [
            ClassificationKinds.Name, 
            ClassificationKinds.Punctuation, 
            ClassificationKinds.Name
            ]);
    }

    public void TestClassification(string text, string[] expectedClassifications)
    {
        var tree = new TinyParser().Parse("test", text);
        var compilation = new TinyCompilation(tree, ReflectionSymbols.CurrentMscorlib);
        var services = new TinyServices(compilation, compilation.Documents[0]);

        var classifications = services.GetClassificationsAsync(0, text.Length, default).Result.Classifications;
        Assert.AreEqual(expectedClassifications.Length, classifications.Count, "classification count");

        if (expectedClassifications.Zip(classifications).Any(x => x.First != x.Second.Classification))
        {
            var expected = string.Join(", ", expectedClassifications);
            var actual = string.Join(", ", classifications.Select(c => c.Classification));
            Assert.Fail($"expected classifications:\n{expected}\nactual:\n{actual}");
        }
    }
}