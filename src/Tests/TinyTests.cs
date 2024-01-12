using Parkour;
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
            new LexicalToken(TokenKinds.EndOfTextToken, "", ""));

        TestLexer("  ",
            new LexicalToken(TokenKinds.EndOfTextToken, "  ", ""));

        TestLexer("ABC",
            new LexicalToken(TokenKinds.IdentifierToken, "", "ABC"),
            new LexicalToken(TokenKinds.EndOfTextToken, "", ""));

        TestLexer("  ABC ",
            new LexicalToken(TokenKinds.IdentifierToken, "  ", "ABC"),
            new LexicalToken(TokenKinds.EndOfTextToken, " ", ""));

        TestLexer("ABC + 123",
            new LexicalToken(TokenKinds.IdentifierToken, "", "ABC"),
            new LexicalToken(TokenKinds.PlusToken, " ", "+"),
            new LexicalToken(TokenKinds.NumberToken, " ", "123"),
            new LexicalToken(TokenKinds.EndOfTextToken, "", ""));
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
        TestSyntax("ABC", Root(IdentifierToken("ABC")));
        TestSyntax("A + B", Root(Add(IdentifierToken("A"), PlusToken(), IdentifierToken("B"))));
        TestSyntax("A * B + C", Root(Add(Multiply(IdentifierToken("A"), AsteriskToken(), IdentifierToken("B")), PlusToken(), IdentifierToken("C"))));
    }

    public void TestSyntax(string text, SyntaxElement expected, bool trivia = false)
    {
        var syntax = new TinyParser().Parse("test", text);
        var root = syntax.Root;
        var kind = root.Kind;

        AssertEquals(expected, syntax.Root);
    }
}