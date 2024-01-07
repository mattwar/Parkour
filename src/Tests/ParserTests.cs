using Parkour;
using Parkour.Parsing;
using Parkour.Syntax;

namespace Tests
{
    using static TestHelpers;
    using static CharParserFactory;
    using static ParserFactory<char>;

    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TestDigit()
        {
            Test(Digit, "A", succeeds: false);
            Test(Digit, "1");
            Test(Digit, ".", succeeds: false);
            Test(Digit, "", succeeds: false);
        }

        [TestMethod]
        public void TestLetter()
        {
            Test(Letter, "A");
            Test(Letter, "1", succeeds: false);
            Test(Letter, ".", succeeds: false);
            Test(Letter, "", succeeds: false);
        }

        [TestMethod]
        public void TestLetterOrDigit()
        {
            Test(LetterOrDigit, "A");
            Test(LetterOrDigit, "1");
            Test(LetterOrDigit, ".", succeeds: false);
            Test(LetterOrDigit, "", succeeds: false);
        }

        [TestMethod]
        public void TestWhitespace()
        {
            Test(Whitespace, " ");
            Test(Whitespace, "  ", " ");
            Test(Whitespace, "\t");
            Test(Whitespace, "\r");
            Test(Whitespace, "\n");
            Test(Whitespace, ".", succeeds: false);
            Test(Whitespace, "", succeeds: false);
        }

        [TestMethod]
        public void TestEndOfLine()
        {
            Test(EndOfLine, "\r");
            Test(EndOfLine, "\n");
            Test(EndOfLine, "\r\n");
            Test(EndOfLine, ".", succeeds: false);
            Test(EndOfLine, "", succeeds: false);
        }

        [TestMethod]
        public void TestMatch()
        {
            // parser
            Test(Match('A'), "A");
            Test(Match('A'), "AA", "A");
            Test(Match('A'), "B", succeeds: false);

            // multi-parser
            Test(Match("A"), "A");
            Test(Match("A"), "AA", "A");
            Test(Match("A"), "B", succeeds: false);
        }

        [TestMethod]
        public void TestZeroOrMore()
        {
            Test(Match("A").ZeroOrMore(), "");
            Test(Match("A").ZeroOrMore(), "A");
            Test(Match("A").ZeroOrMore(), "AAA");
            Test(Match("A").ZeroOrMore(), "AAAB", "AAA");
            Test(Match("A").ZeroOrMore(), "B", "");
        }

        [TestMethod]
        public void TestOneOrMore()
        {
            Test(Match("A").OneOrMore(), "", succeeds: false);
            Test(Match("A").OneOrMore(), "A");
            Test(Match("A").OneOrMore(), "AAA");
            Test(Match("A").OneOrMore(), "AAAB", "AAA");
            Test(Match("A").OneOrMore(), "B", succeeds: false);
        }

        [TestMethod]
        public void TestRepeat()
        {
            Test(Match("A").Repeat(2), "", succeeds: false);
            Test(Match("A").Repeat(2), "A", succeeds: false);
            Test(Match("A").Repeat(2), "AA");
            Test(Match("A").Repeat(2), "AAA", "AAA");

            Test(Match("A").Repeat(2, 4), "AA");
            Test(Match("A").Repeat(2, 4), "AAA");
            Test(Match("A").Repeat(2, 4), "AAAA");
            Test(Match("A").Repeat(2, 4), "AAAAA", "AAAA");
        }

        [TestMethod]
        public void TestOptional()
        {
            // parser version
            Test(Match('A').Optional(), "", "\0", "");
            Test(Match('A').Optional(), "A", "A");
            Test(Match('A').Optional(), "B", "\0", "B");
        }

        [TestMethod]
        public void TestAnd()
        {
            //Test(Match("A").And(Match("B")), "", succeeds: false);
            //Test(Match("A").And(Match("B")), "A", succeeds: false);
            //Test(Match("A").And(Match("B")), "B", succeeds: false);
            Test(Match("A").And(Match("B")), "AB");
        }

        [TestMethod]
        public void TestBest()
        {
            Test(Best(Match("A"), Match("B")), "", succeeds: false);
            Test(Best(Match("A"), Match("B")), "A");
            Test(Best(Match("A"), Match("B")), "B");
            Test(Best(Match("A"), Match("B")), "AB", "A");
            Test(Best(Match("A"), Match("B")), "BA", "B");

            // when multiple succeed, the one that consumes most input wins
            Test(Best(Match("A"), Match("A").ZeroOrMore()), "A");
            Test(Best(Match("A"), Match("A").ZeroOrMore()), "AAA");
        }

        [TestMethod]
        public void TestFirst()
        {
            Test(First(Match("A"), Match("B")), "", succeeds: false);
            Test(First(Match("A"), Match("B")), "A");
            Test(First(Match("A"), Match("B")), "B");
            Test(First(Match("A"), Match("B")), "AB", "A");
            Test(First(Match("A"), Match("B")), "BA", "B");

            // when multiple succeed the first one wins
            Test(First(Match("A"), Match("A").ZeroOrMore()), "A");
            Test(First(Match("A"), Match("A").ZeroOrMore()), "A");
            Test(First(Match("A").ZeroOrMore(), Match("A")), "AAA");
        }

        [TestMethod]
        public void TestNot()
        {
            Test(Not(Match("A")), "A", succeeds: false);
            Test(Not(Match("A")), "B");
            Test(Not(Match("AB")), "AB", succeeds: false);
            Test(Not(Match("AB")), "A", "A");
        }

        [TestMethod]
        public void TestIf()
        {
            // parser
            var p2 = If(Match('A'), Map(Match('A'), Match('B'), (a, b) => 'C'));
            Test(p2, "A", succeeds: false);
            Test(p2, "AA", succeeds: false);
            Test(p2, "AB", "C", "");

            //// multiparser
            var p = If(Letter.OneOrMore().And(Match("=")),
                Map(Letter.OneOrMore(), Match("="), Letter.OneOrMore(), (left, op, right) => Concat(left, op, right)));
            Test(p, "A", succeeds: false);
            Test(p, "A=", succeeds: false);
            Test(p, "A=B", "A=B");
        }

        [TestMethod]
        public void TestLimit()
        {
            var anyUntilDot = Any.ZeroOrMore().Limit(ZeroOrMore(Not(Match('.'))));
            Test(anyUntilDot, "A", "A", "");
            Test(anyUntilDot, "A.", "A", ".");
            Test(anyUntilDot, "ABCD.", "ABCD", ".");
        }

        [TestMethod]
        public void TestUnless()
        {
            // parser version
            Test(Match('A').Unless(Match('B')), "", succeeds: false);
            Test(Match('A').Unless(Match("B")), "A");
            Test(Match('A').Unless(Match('B')), "AA", "A");
            Test(Match('A').Unless(Match('B')), "AB", succeeds: false);

            // multi-parser version
            Test(Match("A").Unless(Match("B")), "", succeeds: false);
            Test(Match("A").Unless(Match("B")), "A");
            Test(Match("A").Unless(Match("B")), "AA", "A");
            Test(Match("A").Unless(Match("B")), "AB", succeeds: false);
        }

        [TestMethod]
        public void TestApply()
        {
            var parser = Match("AB").Apply(
                fnAB => Match("CD").Select(cd => Concat(fnAB(), cd)));

            Test(parser, "", succeeds: false);
            Test(parser, "A", succeeds: false);
            Test(parser, "AB", succeeds: false);
            Test(parser, "ABC", succeeds: false);
            Test(parser, "ABCD");
        }

        [TestMethod]
        public void TestThen()
        {
            var parser = Match("AB").Then(Match("CD"), (ab, cd) => Concat(ab, cd));

            Test(parser, "", succeeds: false);
            Test(parser, "A", succeeds: false);
            Test(parser, "AB", succeeds: false);
            Test(parser, "ABC", succeeds: false);
            Test(parser, "ABCD");
        }

        [TestMethod]
        public void TestApplyOptional()
        {
            var parser = Match("AB").ApplyOptional(
                fnAB => Match("CD").Select(cd => Concat(fnAB(), cd)));

            Test(parser, "", succeeds: false);
            Test(parser, "A", succeeds: false);
            Test(parser, "AB", "AB", "");
            Test(parser, "ABC", "AB", "C");
            Test(parser, "ABCD", "ABCD", "");
        }

        [TestMethod]
        public void TestApplyRepeat()
        {
            var parser = Match("AB").ApplyRepeat(
                fnAB => Match("CD").Select(cd => Concat(fnAB(), cd)));

            Test(parser, "", succeeds: false);
            Test(parser, "A", succeeds: false);
            Test(parser, "AB", "AB", "");
            Test(parser, "ABC", "AB", "C");
            Test(parser, "ABCD", "ABCD", "");
            Test(parser, "ABCDCD", "ABCDCD", "");
        }

        [TestMethod]
        public void TestOperators()
        {
            var parser = Operators(
                Letter.OneOrMore(),
                context => context
                    .Prefix(Match("!"), (op, right) => Concat(op, right))
                    .Lower()
                    .Infix(Match("*"), (left, op, right) => Concat("(", left, op, right, ")"))
                    .Lower()
                    .Infix(Match("+"), (left, op, right) => Concat("(", left, op, right, ")"))
                    );

            Test(parser, "!A", "!A", "");
            Test(parser, "A+B", "(A+B)", "");
            Test(parser, "A+B+C", "((A+B)+C)", "");
            Test(parser, "A*B", "(A*B)", "");
            Test(parser, "A*B+C", "((A*B)+C)", "");
            Test(parser, "A+B*C", "(A+(B*C))", "");

            Test(parser, "", succeeds: false);
            Test(parser, ".", succeeds: false);
            Test(parser, "!", succeeds: false);
            Test(parser, "A+", succeeds: false);
            Test(parser, "A+B+", succeeds: false);

            var parserWithReq = Operators(
                Letter.OneOrMore(),
                Letter.OneOrMore().Required(() => "?".ToArray()),
                context => context
                    .Prefix(Match("!"), (op, right) => Concat(op, right))
                    .Lower()
                    .Infix(Match("*"), (left, op, right) => Concat("(", left, op, right, ")"))
                    .Lower()
                    .Infix(Match("+"), (left, op, right) => Concat("(", left, op, right, ")"))
                    );
        }

        [TestMethod]
        public void TestOperators_Required()
        {
            var parser = Operators(
                Letter.OneOrMore(),
                Letter.OneOrMore().Required(() => "?".ToArray()),
                context => context
                    .Prefix(Match("!"), (op, right) => Concat(op, right))
                    .Lower()
                    .Infix(Match("*"), (left, op, right) => Concat("(", left, op, right, ")"))
                    .Lower()
                    .Infix(Match("+"), (left, op, right) => Concat("(", left, op, right, ")"))
                    );

            Test(parser, "!A", "!A", "");
            Test(parser, "A+B", "(A+B)", "");
            Test(parser, "A+B+C", "((A+B)+C)", "");
            Test(parser, "A*B", "(A*B)", "");
            Test(parser, "A*B+C", "((A*B)+C)", "");
            Test(parser, "A+B*C", "(A+(B*C))", "");

            Test(parser, "", succeeds: false);
            Test(parser, ".", succeeds: false);
            Test(parser, "!", "!?", "");
            Test(parser, "A+", "(A+?)", "");
            Test(parser, "A+B+", "((A+B)+?)", "");
        }

        [TestMethod]
        public void TestSwitch()
        {
            var p = Switch(context => context
                .Case("AB", Match("ABC"))
                .Case("12", Match("123"))
                .Case("12345", Match("12345")));

            Test(p, "ABC", "ABC");
            Test(p, "AB", succeeds: false);
            Test(p, "123", "123", "");
            Test(p, "1234", "123", "4");
            Test(p, "12345", "12345", "");
            Test(p, "123456", "12345", "6");
        }

        private void Test(Parser<char, IReadOnlyList<char>> parser, string input, string? expectedOutput = null, string? expectedRemaining = null, bool succeeds = true)
        {
            expectedOutput ??= input;

            TestParse(parser, input, expectedOutput, expectedRemaining, succeeds);
            TestScan(parser, input, expectedRemaining != null ? input.Length - expectedRemaining.Length : null, succeeds);
            TestSearch(parser, input, expectedRemaining != null ? input.Length - expectedRemaining.Length : null, succeeds);
        }

        private void Test(Parser<char, char> parser, string input, string? expectedOutput = null, string? expectedRemaining = null, bool succeeds = true)
        {
            Test(parser.ToList(), input, expectedOutput, expectedRemaining, succeeds);
        }

        private void TestParse(Parser<char, IReadOnlyList<char>> parser, string input, string expectedOutput, string? expectedRemaining = null, bool succeeds = true)
        {
            var result = parser.Parse(input);
            Assert.AreEqual(succeeds, result.Success, "parse success");
            if (succeeds)
            {
                var actualOutput = result.Output != null ? new string(result.Output.ToArray()) : "";
                Assert.AreEqual(expectedOutput, actualOutput, "parsed output");

                if (expectedRemaining != null)
                {
                    var actualRemaining = input.Substring(result.Length);
                    Assert.AreEqual(expectedRemaining, actualRemaining, "remaining input");
                }
            }
        }

        private void TestScan(Parser<char, IReadOnlyList<char>> parser, string input, int? expectedConsumedLength = null, bool succeeds = true)
        {
            var result = parser.Scan(input);
            Assert.AreEqual(succeeds, result.Success, "scan success");
            if (succeeds)
            {
                if (expectedConsumedLength != null)
                    Assert.AreEqual(expectedConsumedLength, result.Length, "consumed input length");
            }
        }

        private void TestSearch(Parser<char, IReadOnlyList<char>> parser, string input, int? expectedConsumedLength = null, bool succeeds = true)
        {
            var callbackInvoked = false;
            var rootAfterMissing = false;

            var result = parser.Search(input, 
                rootAfterMissing,                
                (p, _, _) => { if (p == parser) { callbackInvoked = true; } });

            Assert.AreEqual(succeeds, result.Success, "search success");
            if (succeeds)
            {
                if (expectedConsumedLength != null)
                    Assert.AreEqual(expectedConsumedLength, result.Length, "consumed input length");
            }

            Assert.IsTrue(callbackInvoked, "callback invoked");
        }
    }
}
