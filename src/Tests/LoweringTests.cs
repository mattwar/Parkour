using Parkour;
using Parkour.Semantics;
using Parkour.Reflection;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;
using static TestHelpers;

[TestClass]
public class LoweringTests
{
    [TestMethod]
    public void TestConstantFolding()
    {
        // int operator folds
        TestFoldOperator(OperatorKind.Add, [1, 2], 3);
        TestFoldOperator(OperatorKind.Subtract, [1, 2], -1);
        TestFoldOperator(OperatorKind.Multiply, [2, 3], 6);
        TestFoldOperator(OperatorKind.Divide, [6, 3], 2);
        TestFoldOperator(OperatorKind.Remainder, [5, 3], 2);
        TestFoldOperator(OperatorKind.Negate, [2], -2);
        TestFoldOperator(OperatorKind.Increment, [1], 2);
        TestFoldOperator(OperatorKind.Decrement, [1], 0);
        TestFoldOperator(OperatorKind.BitwiseAnd, [1, 2], 0);
        TestFoldOperator(OperatorKind.BitwiseOr, [1, 2], 3);
        TestFoldOperator(OperatorKind.BitwiseXor, [1, 0], 1);
        TestFoldOperator(OperatorKind.BitwiseNot, [1], ~1);
        TestFoldOperator(OperatorKind.ShiftLeft, [1, 1], 2);
        TestFoldOperator(OperatorKind.ShiftRight, [4, 1], 2);
        TestFoldOperator(OperatorKind.Equal, [1, 1], true);
        TestFoldOperator(OperatorKind.NotEqual, [1, 0], true);
        TestFoldOperator(OperatorKind.LessThan, [1, 2], true);
        TestFoldOperator(OperatorKind.LessThanOrEqual, [2, 2], true);
        TestFoldOperator(OperatorKind.GreaterThan, [2, 1], true);
        TestFoldOperator(OperatorKind.GreaterThanOrEqual, [2, 2], true);

        // bool operator folds
        TestFoldOperator(OperatorKind.LogicalAnd, [true, true], true);
        TestFoldOperator(OperatorKind.LogicalOr, [true, false], true);
        TestFoldOperator(OperatorKind.LogicalNot, [true], false);
    }

    private void TestFoldOperator(string op, object[] args, object expectedResult)
    {
        TestLower(
            Operator(op, args.Select(a => (Expression)Constant(a)).ToImmutableList()),
            lowerers: [ConstantFoldingLowerer.Instance],
            fnValidate: elements =>
            {
                Assert.AreEqual(1, elements.Count);
                var constValue = elements[0] as ConstantExpression;
                Assert.IsNotNull(constValue);
                Assert.AreEqual(expectedResult, constValue.Value);
            });
    }

    private void TestLower(
        SemanticElement element,
        string[]? expectedSymbols = null,
        string[]? unexpectedSymbols = null,
        string? expectedResultType = null,
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false,
        Action<ImmutableList<SemanticElement>>? fnValidate = null,
        ImmutableList<PartialLowerer>? lowerers = null
        )
        =>
        TestLower(
            [element],
            expectedSymbols,
            unexpectedSymbols,
            expectedResultType,
            expectedReferencedSymbol,
            containsDiagnostics,
            fnValidate,
            lowerers);


    private void TestLower(
        ImmutableList<SemanticElement> elements,
        string[]? expectedSymbols = null,
        string[]? unexpectedSymbols = null,
        string? expectedResultType = null,
        string? expectedReferencedSymbol = null,
        bool containsDiagnostics = false,
        Action<ImmutableList<SemanticElement>>? fnValidate = null,
        ImmutableList<PartialLowerer>? lowerers = null
        )
    {
        var binder = new StandardBinder();
        var binding = binder.Bind(elements, ReflectionSymbols.CurrentMscorlib);

        var bindingDiagnostics = binding.Elements.GetContainedDiagnostics();
        if (bindingDiagnostics.Count > 0)
        {
            Assert.Fail($"Unexpected binding diagnostics:\n{bindingDiagnostics[0]}");
        }

        var allBound = binding.Elements.All(e => !e.IsUnbound);
        if (!allBound)
        {
            Assert.Fail("Elements still unbound after binding");
        }

        var lowerer = new StandardLowerer(
            new StandardBinder(),
            lowerers,
            includeElementLowerers: lowerers == null
            );

        var lowering = lowerer.Lower(binding);

        var loweringDiagnostics = lowering.Elements.GetContainedDiagnostics();
        if (loweringDiagnostics.Count > 0)
        if (loweringDiagnostics.Count > 0 && !containsDiagnostics)
        {
            Assert.Fail($"Unexpected lowering diagnostics:\n{loweringDiagnostics[0]}");
        }
        else if (containsDiagnostics && loweringDiagnostics.Count == 0)
        {
            Assert.Fail($"Unexpected missing lowering diagnostics");
        }

        if (expectedSymbols != null)
        {
            foreach (var path in expectedSymbols)
            {
                var symbol = lowering.CombinedSymbols.GetSymbol(path);
                Assert.IsNotNull(symbol, $"symbol '{path}' not found");
            }
        }

        if (unexpectedSymbols != null)
        {
            foreach (var path in unexpectedSymbols)
            {
                var found = lowering.CombinedSymbols.TryGetSymbol(path, out _);
                if (found)
                {
                    Assert.Fail($"Unexpected symbol found: '{path}'");
                }
            }
        }

        if (lowering.Elements.OfType<Expression>().LastOrDefault() is Expression expr)
        {
            if (expectedResultType != null)
                Assert.AreEqual(expectedResultType, expr.ResultType?.FullName, "result type");

            if (expectedReferencedSymbol != null)
                Assert.AreEqual(expectedReferencedSymbol, expr.ReferencedSymbol?.FullName, "referenced symbol");
        }

        if (fnValidate != null)
            fnValidate(lowering.Elements);
    }
}