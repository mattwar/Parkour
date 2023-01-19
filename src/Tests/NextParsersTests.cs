using Parkour;

namespace Tests;

using static TestHelpers;
using static CharParserFactory;
using static ParserFactory<char>;
using static System.Net.Mime.MediaTypeNames;

[TestClass]
public class NextParsersTests
{
    [TestMethod]
    public void TestMatch()
    {
        Test(Match("A"), "$", "'A'");
        Test(Match("A"), "A$", "");

        Test(Match('A'), "$", "'A'");
        Test(Match('A'), "A$", "");
    }

    [TestMethod]
    public void TestZeroOrMore()
    {
        Test(Match("A").ZeroOrMore(), "$", "'A'");
        Test(Match("A").ZeroOrMore(), "$A", "'A'");
        Test(Match("A").ZeroOrMore(), "$B", "'A'");
        Test(Match("A").ZeroOrMore(), "A$", "'A'");
        Test(Match("A").ZeroOrMore(), "AA$", "'A'");
        Test(Match("A").ZeroOrMore(), "AA$B", "'A'");

        Test(Match('A').ZeroOrMore(), "$", "'A'");
        Test(Match('A').ZeroOrMore(), "$A", "'A'");
        Test(Match('A').ZeroOrMore(), "$B", "'A'");
        Test(Match('A').ZeroOrMore(), "A$", "'A'");
        Test(Match('A').ZeroOrMore(), "AA$", "'A'");
        Test(Match('A').ZeroOrMore(), "AA$B", "'A'");
    }

    [TestMethod]
    public void TestOneOrMore()
    {
        Test(Match("A").OneOrMore(), "$", "'A'");
        Test(Match("A").OneOrMore(), "$A", "'A'");
        Test(Match("A").OneOrMore(), "$B", "'A'");
        Test(Match("A").OneOrMore(), "A$", "'A'");
        Test(Match("A").OneOrMore(), "AA$", "'A'");
        Test(Match("A").OneOrMore(), "AA$B", "'A'");

        Test(Match('A').OneOrMore(), "$", "'A'");
        Test(Match('A').OneOrMore(), "$A", "'A'");
        Test(Match('A').OneOrMore(), "$B", "'A'");
        Test(Match('A').OneOrMore(), "A$", "'A'");
        Test(Match('A').OneOrMore(), "AA$", "'A'");
        Test(Match('A').OneOrMore(), "AA$B", "'A'");
    }

    [TestMethod]
    public void TestElse()
    {
        Test(Match("A").Else(Match("B")), "$", "'A', 'B'");
        Test(Match('A').Else(Match('B')), "$", "'A', 'B'");
    }

    [TestMethod]
    public void TestOr()
    {
        Test(Match("A").Or(Match("B")), "$", "'A', 'B'");
        Test(Match('A').Or(Match('B')), "$", "'A', 'B'");
    }

    [TestMethod]
    public void TestAnd()
    {
        var p = Match("A").And(Match("B")).And(Match("C"));
        Test(p, "$", "'A'");
        Test(p, "A$", "'B'");
        Test(p, "AB$", "'C'");
        Test(p, "ABC$", "");
    }

    [TestMethod]
    public void TestOptional()
    {
        Test(Match("A").Optional(), "$", "'A'");
        Test(Match('A').Optional(), "$", "'A'");
    }

    [TestMethod]
    public void TestRequired()
    {
        var p = Required(Match("A"));
        Test(p, "$", "'A'");
    }

    [TestMethod]
    public void TestNot()
    {
        var p = Not(Match("A"));
        Test(p, "$", "");
    }

    [TestMethod]
    public void TestLeftReduce()
    {
        var p = LeftReduce(Match("A"), fnLeft => Map(Match("+"), Match("A"), (op, right) => Concat(fnLeft(), op, right)));
        Test(p, "$", "'A'");
        Test(p, "A$", "'+'");
        Test(p, "A+$", "'A'");
        Test(p, "A+A$", "'+'");
        Test(p, "A+A+$", "'A'");
    }

    [TestMethod]
    public void TestRightReduce()
    {
        var p = RightReduce(Match("+"), Match("A"), (left, right) => Concat(left, right));
        Test(p, "$", "'+', 'A'");
        Test(p, "+$", "'+', 'A'");
        Test(p, "++$", "'+', 'A'");
        Test(p, "+A$", "");
    }

    [TestMethod]
    public void TestMissing_AfterAndElement()
    {
        var abcd = Match("A").And(Required(Match("B")).And(Match("C")).And(Match("D")));
        Test(abcd, "$", "'A'");
        Test(abcd, "A$", "'B'");
        Test(abcd, "AB$", "'C'");
        Test(abcd, "ABC$", "'D'");
        Test(abcd, "AC$", "'D'");
        Test(abcd, "AX$", "");
    }

    [TestMethod]
    public void TestMissing_AfterMapElement()
    {
        var abcd = Map(Match("A"), Required(Match("B")), Match("C"), Match("D"), (a, b, c, d) => Concat(a, b, c, d));
        Test(abcd, "$", "'A'");
        Test(abcd, "A$", "'B'");
        Test(abcd, "AB$", "'C'");
        Test(abcd, "ABC$", "'D'");
        Test(abcd, "AC$", "'D'");
        Test(abcd, "AX$", "");
    }

    [TestMethod]
    public void TestMissing_AfterNestedRequired()
    {
        var ab = Map(Match("A"), Required(Match("B")), (a, b) => Concat(a,b));
        var abcd = Map(ab, Match("C"), Match("D"), (ab, c, d) => Concat(ab, c, d));

        Test(abcd, "$", "'A'");
        Test(abcd, "A$", "'B'");
        Test(abcd, "AB$", "'C'");
        Test(abcd, "ABC$", "'D'");
        Test(abcd, "AC$", "'D'");
    }

    [TestMethod]
    public void TestMissing_AfterLeftReduceApply()
    {
        var p = Match("A").LeftReduce(fnLeft => Map(Match("+"), Required(Match("A")), (op, right) => Concat(fnLeft(), op, right))).And(Match("B"));
        Test(p, "$", "'A'");
        Test(p, "A$", "'+', 'B'");
        Test(p, "A+$", "'A'");
        Test(p, "A+A$", "'+', 'B'");
        Test(p, "A+A+$", "'A'");
        Test(p, "A+AB$", "");
    }

    [TestMethod]
    public void TestMissing_AfterRequiredAtEndOfElseBranch()
    {
        // bogus scenario.. should not have identical start in first/else if remaining in earlier path is required
        var abc = Match("A").And(Required(Match("B")))
                    .Else(Match("A").And(Match("D")))
                 .And(Match("C"));

        Test(abc, "$", "'A'");
        Test(abc, "A$", "'B', 'D'");
    }

    private static Parser<char, IReadOnlyList<char>> Required(Parser<char, IReadOnlyList<char>> parser) =>
        parser.Required(() => Concat("?"));

    private void Test(Parser<char> parser, string textWithMarker, string expectedNextTerms)
    {
        var (textWithoutMarker, position) = StripMarker(textWithMarker);
        var parsers = parser.GetNextParsers(textWithoutMarker.AsSpan(), position, (p, afterMissing) => p.Term != null && !afterMissing);
        var nextTerms = parsers.Select(p => p.Term ?? "").ToHashSet();
        var actualNextTerms = string.Join(", ", nextTerms.Select(t => $"'{t}'"));
        Assert.AreEqual(expectedNextTerms, actualNextTerms, "next terms");
    }
}