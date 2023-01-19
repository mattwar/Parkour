using Parkour;
using System.Diagnostics.Metrics;

namespace Tests;

[TestClass]
public class TinyTests
{
    [TestMethod]
    public void TestIdentifier()
    {
        var parser = CharParserFactory.Letter.And(CharParserFactory.LetterOrDigit.ZeroOrMore()).Text();

        var success = parser.Parse("ABC", out var output, out var remainingInput);
    }

    [TestMethod]
    public void TestLexer()
    {
        //TestLexer("",
        //    new LexicalToken(TokenKinds.EndOfText, "", ""));

        //TestLexer("  ",
        //    new LexicalToken(TokenKinds.EndOfText, "  ", ""));

        TestLexer("ABC",
            new LexicalToken(TokenKinds.Identifier, "", "ABC"),
            new LexicalToken(TokenKinds.EndOfText, "", ""));

        //TestLexer("  ABC ",
        //    new LexicalToken(TokenKinds.Identifier, "  ", "ABC"),
        //    new LexicalToken(TokenKinds.EndOfText, " ", ""));

        //TestLexer("ABC + 123",
        //    new LexicalToken(TokenKinds.Identifier, "", "ABC"),
        //    new LexicalToken(TokenKinds.Plus, " ", "+"),
        //    new LexicalToken(TokenKinds.Number, " ", "123"),
        //    new LexicalToken(TokenKinds.EndOfText, "", ""));
    }

    public void TestLexer(string text, params LexicalToken[] expectedTokens)
    {
        var actualTokens = new TinyLexer().Parse(text);

        Assert.AreEqual(expectedTokens.Length, actualTokens.Length, "Tokens.Length");

        for (int i = 0; i < expectedTokens.Length; i++)
        {
            Assert.AreEqual(expectedTokens[i].Kind, actualTokens[i].Kind, $"Tokens[{i}].Kind");
            Assert.AreEqual(expectedTokens[i].Trivia, actualTokens[i].Trivia, $"Tokens[{i}].Trivia");
            Assert.AreEqual(expectedTokens[i].Text, actualTokens[i].Text, $"Tokens[{i}].Text");
        }
    }
}