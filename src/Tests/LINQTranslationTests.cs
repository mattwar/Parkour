using System.Collections;
using System.Reflection;
using Parkour;
using Parkour.Binding;
using Parkour.Semantics;
using Parkour.Symbols;
using Parkour.Emitting;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class LINQTranslationTests
{
    private readonly RuntimeSymbols _runtimeSymbols;
    private readonly SymbolCache _symbols;
    private readonly BindingScope _defaultTestScope;

    public LINQTranslationTests()
    {
        _runtimeSymbols = RuntimeSymbols.CurrentMscorlib;
        _symbols = _runtimeSymbols.Symbols;
        _defaultTestScope = ExpressionTests.CreateBindingScope(_symbols);
    }

    [TestMethod]
    public void TestAssign()
    {
        TestRun(
            Block(
                Variable(Symbol(_symbols.Int32), "x"),
                Assign(Name("x"), Constant(2))),
            2);
    }

    [TestMethod]
    public void TestBlock()
    {
        TestRun(Block(Constant(1)), 1);
        TestRun(Block(Return(Constant(2)), Constant(1)), 2);
        TestRun(Block(If(Constant(true), Return(Constant(1))), Constant(2)), 1);
        TestRun(Block(If(Constant(false), Return(Constant(1))), Constant(2)), 2);
    }

    [TestMethod]
    public void TestBranch()
    {
        // branch forward
        TestRun(
            Block(
                Goto("x"),
                Label("x"),
                Constant(1)),
            1);

        TestRun(
            Block(
                Goto("x"),
                Label("x")),
            null);

        TestRun(
            Block(
                Goto("x", Constant(1)),
                Label("x", Symbol(_symbols.Int32))),
            1);
    }

    [TestMethod]
    public void TestCall()
    {
        TestRun(Call(Constant(1).Member("ToString")), "1");
    }

    [TestMethod]
    public void TestCondition()
    {
        TestRun(Condition(Constant(true), Constant(1), Constant(2)), 1);
        TestRun(Condition(Constant(false), Constant(1), Constant(2)), 2);
        TestRun(Condition(Constant(true), Constant(1)), 1);
        TestRun(Condition(Constant(false), Constant(1)), 0);
    }

    [TestMethod]
    public void TestConstant()
    {
        TestRun(Constant(1), 1);
        TestRun(Constant("one"), "one");
    }

    [TestMethod]
    public void TestTypeArgs()
    {
        TestRun(
            Symbol("System.Collections.Generic.List`1").WithTypeArguments([Symbol(_symbols.Int32)]),
            typeof(List<int>));
    }

    [TestMethod]
    public void TestDefault()
    {
        TestRun(Default(Symbol(_symbols.Int32)), 0);
    }

    [TestMethod]
    public void TestLambda()
    {
        // test w/o parameter
        TestRun(
            Lambda(Constant(1)),
            1);

        // test w/ parameter
        TestRun(
            Lambda(
                [Parameter("x", Symbol(_symbols.Int32))], 
                Name("x")),
            [2],
            2);

        // test w/ 2 parameters
        TestRun(
            Lambda(
                [
                    Parameter("x", Symbol(_symbols.Int32)), 
                    Parameter("y", Symbol(_symbols.Int32))
                ],
                Add(Name("x"), Name("y"))),
            [2, 3],
            5);

        // invoke lambda
        TestRun(
            Call(
                Lambda([Parameter("x", Symbol(_symbols.Int32))], Name("x")),
                [Constant(2)]),
            2);
    }

    [TestMethod]
    public void TestLoop()
    {
        TestRun(
            Loop(
                Break()),
            null);

        TestRun(
            Loop(
                Break(Constant(1))),
            1);

        TestRun(
            Loop(
                Block(
                    Break(Constant(1)),
                    Break(Constant(2L)))),
            1L);
    }

    [TestMethod]
    public void TestNew()
    {
        TestRun(
            New(Symbol("System.Object")),
            expectedResult: new object()
            );

        TestRun(
            New(Symbol("System.Collections.Generic.List`1").WithTypeArguments([Symbol("System.Int32")])),
            expectedResult: new List<int>()
            );
    }

    [TestMethod]
    public void TestNewArraySize()
    {
        TestRun(
            NewArray(Symbol("System.Int32"), Constant(3)),
            expectedResult: new int[3]);
    }

    [TestMethod]
    public void TestNewArrayInit()
    {
        TestRun(
            NewArray(Symbol("System.Int32"), [Constant(1), Constant(2), Constant(3)]),
            expectedResult: new int[] {1, 2, 3});

        // infer element type
        TestRun(
            NewArray([Constant(1), Constant(2), Constant(3)]),
            expectedResult: new int[] { 1, 2, 3 });
    }

    [TestMethod]
    public void TestOperators()
    {
        // Int32 operators
        TestRun(Add(Constant(1), Constant(2)), 3);
        TestRun(Subtract(Constant(3), Constant(1)), 2);
        TestRun(Multiply(Constant(2), Constant(3)), 6);
        TestRun(Divide(Constant(4), Constant(2)), 2);
        TestRun(Remainder(Constant(5), Constant(3)), 2);
        TestRun(Negate(Constant(1)), -1);
        TestRun(BitwiseAnd(Constant(3), Constant(2)), 3 & 2);
        TestRun(BitwiseOr(Constant(1), Constant(2)), 1 | 2);
        //TestRun(BitwiseXor(Constant(1), Constant(2)), 1 ^ 2);
        TestRun(BitwiseNot(Constant(1)), ~1);
        //TestRun(ShiftLeft(Constant(1), Constant(2)), 1 << 2);
        //TestRun(ShiftRight(Constant(8), Constant(1)), 8 >> 1);
        TestRun(Equal(Constant(1), Constant(2)), 1 == 2);
        TestRun(NotEqual(Constant(1), Constant(2)), 1 != 2);
        TestRun(LessThan(Constant(1), Constant(2)), 1 < 2);
        TestRun(LessThanOrEqual(Constant(1), Constant(2)), 1 <= 2);
        TestRun(GreaterThan(Constant(1), Constant(2)), 1 > 2);
        TestRun(GreaterThanOrEqual(Constant(1), Constant(2)), 1 >= 2);

        // boolean operators
        TestRun(LogicalAnd(Constant(true), Constant(false)), true & false);
        TestRun(LogicalAndAlso(Constant(true), Constant(false)), true && false);
        TestRun(LogicalOr(Constant(true), Constant(false)), true | false);
        TestRun(LogicalOrElse(Constant(true), Constant(false)), true || false);
        TestRun(LogicalNot(Constant(true)), !true);
    }

    [TestMethod]
    public void TestParameters()
    {
        TestRun(
            Lambda([Parameter("x", Symbol(_symbols.Int32))], Name("x")), 
            [1], 
            1);
        
        TestRun(
            Lambda([Parameter("x", Symbol(_symbols.Int32))], Add(Name("x"), Constant(1))), 
            [2], 
            3);
    }

    [TestMethod]
    public void TestMember()
    {
        TestRun(
            Symbol(_symbols.Int32).Member("MaxValue"), 
            Int32.MaxValue);
    }

    [TestMethod]
    public void TestVariable()
    {
        // variable without block
        TestRun(
            Variable("x", Constant(1)),
            1);

        // variable in block
        TestRun(
            Block(
                Variable("x", Constant(1))),
            1);

        // variable with initializer and reference
        TestRun(
            Block(
                Variable("x", Constant(1)),
                Add(Name("x"), Constant(2))),
            3);

        // variable without initializer and assignment
        TestRun(
            Block(
                Variable(Symbol(_symbols.Int32), "x"),
                Assign(Name("x"), Constant(2))),
            2);
    }

    private void TestRun(Expression expression, object? expectedResult, BindingScope? scope = null) =>
        TestRun(expression, [], expectedResult, scope);

    private void TestRun(Expression expression, object[] args, object? expectedResult, BindingScope? scope = null)
    {
        if (!(expression is LambdaExpression))
            expression = Lambda(expression);

        args ??= System.Array.Empty<object>();

        var binder = new SemanticBinder();
        var bound = (LambdaExpression)binder.BindExpression(expression, _symbols.GlobalNamespace, scope ?? _defaultTestScope);

        if (bound.ContainsDiagnostics)
        {
            var diagnostics = new List<Diagnostic>();
            bound.GetContainedDiagnostics(diagnostics);
            var dx = diagnostics[0];
            Assert.Fail($"The expression contains diagnostics: {dx.Message}");
        }

        Assert.IsFalse(bound.ContainsDiagnostics, "expression contains diagnostics");
        Assert.IsFalse(bound.IsUnbound, "expression still contains unbound elements after binding.");

        var translated = new LinqExpressionTranslator(_runtimeSymbols).TranslateToLambda(bound);

        var compiled = translated.Compile();

        var actualResult = compiled.DynamicInvoke(args);

        AssertAreEquivalent(expectedResult, actualResult);
    }

    private static void AssertAreEquivalent(object? expected, object? actual)
    {
        // they are same instance?
        if (expected == actual)
            return;

        if (expected is null)
        {
            Assert.IsNull(actual, "expected null");
            return;
        }

        Assert.IsNotNull(actual, "expected not null");

        var expectedType = expected.GetType();
        var actualType = actual.GetType();

        if (expectedType != actualType)
        {
            Assert.Fail($"expected type: '{expectedType.Name}' actual type: '{actualType.Name}'");
        }

        if (expected is ICollection eCollection && actual is ICollection aCollection)
        {
            Assert.AreEqual(eCollection.Count, aCollection.Count, "collection count");
        }

        if (expected is IEnumerable eEnumerable && actual is IEnumerable aEnumerable)
        {
            var eEnumerator = eEnumerable.GetEnumerator();
            var aEnumerator = aEnumerable.GetEnumerator();

            while (eEnumerator.MoveNext())
            {
                if (!aEnumerator.MoveNext())
                    Assert.Fail("actual collection contains too few elements");

                AssertAreEquivalent(eEnumerator.Current, aEnumerator.Current);
            }

            if (aEnumerator.MoveNext())
                Assert.Fail("actual collection contains too many elements");
        }
        else if (expectedType.IsPrimitive || expectedType == typeof(Type))
        {
            Assert.AreEqual(expected, actual);
        }
        else
        {
            var typeCode = Type.GetTypeCode(expectedType);
            if (typeCode == TypeCode.Object)
            {
                var fields = expectedType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                foreach (var field in fields)
                {
                    var eValue = field.GetValue(expected);
                    var aValue = field.GetValue(actual);
                    AssertAreEquivalent(eValue, aValue);
                }
            }
            else
            {
                Assert.AreEqual(expected, actual);
            }
        }
    }
}