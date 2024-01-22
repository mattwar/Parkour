using Parkour;
using Parkour.Binding;
using Parkour.Semantics;
using Parkour.Execution;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class RunTests
{
    private readonly SymbolCache _symbols;
    private readonly BindingScope _defaultTestScope;

    public RunTests()
    {
        _symbols = RuntimeSymbols.GetOrCreateCommonSymbols();
        _defaultTestScope = ExpressionTests.CreateBindingScope(_symbols);
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
                Label("x", Type(_symbols.Int32))),
            1);
    }

    [TestMethod]
    public void TestCall()
    {
        TestRun(Call(Path(Constant(1), "ToString")), "1");
    }

    [TestMethod]
    public void TestCondition()
    {
        TestRun(Condition(Constant(true), Constant(1), Constant(2)), 1);
        TestRun(Condition(Constant(false), Constant(1), Constant(2)), 2);
        TestRun(Condition(Constant(true), Constant(1)), null);
        TestRun(Condition(Constant(false), Constant(1)), null);
    }

    [TestMethod]
    public void TestConstant()
    {
        TestRun(Constant(1), 1);
        TestRun(Constant("one"), "one");
    }

    [TestMethod]
    public void TestDefault()
    {
        TestRun(Default(Type(_symbols.Int32)), 0);
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
        TestRun(And(Constant(true), Constant(false)), true & false);
        TestRun(AndAlso(Constant(true), Constant(false)), true && false);
        TestRun(Or(Constant(true), Constant(false)), true | false);
        TestRun(OrElse(Constant(true), Constant(false)), true || false);
        TestRun(Not(Constant(true)), !true);
    }

    [TestMethod]
    public void TestParameters()
    {
        TestRun(
            Lambda([Parameter("x", Type(_symbols.Int32))], Name("x")), 
            [1], 
            1);
        
        TestRun(
            Lambda([Parameter("x", Type(_symbols.Int32))], Add(Name("x"), Constant(1))), 
            [2], 
            3);
    }

    [TestMethod]
    public void TestPath()
    {
        TestRun(
            Path(Type(_symbols.Int32), Name("MaxValue")), 
            Int32.MaxValue);
    }


    private void TestRun(Expression expression, object? expectedResult, BindingScope? scope = null) =>
        TestRun(expression, [], expectedResult, scope);

    private void TestRun(Expression expression, object[] args, object? expectedResult, BindingScope? scope = null)
    {
        if (!(expression is LambdaExpression))
            expression = Lambda(expression);

        args ??= Array.Empty<object>();

        var binder = new ExpressionBinder(_symbols.GlobalNamespace);
        var bound = (LambdaExpression)binder.Bind(expression, scope ?? _defaultTestScope);

        if (bound.ContainsDiagnostics)
        {
            var diagnostics = new List<Diagnostic>();
            bound.GetContainedDiagnostics(diagnostics);
            var dx = diagnostics[0];
            Assert.Fail($"The expression contains diagnostics: {dx.Message}");
        }

        Assert.IsFalse(bound.ContainsDiagnostics, "expression contains diagnostics");
        Assert.IsFalse(bound.ContainsUnknowns, "expression contains unknowns");

        var translated = new ExpressionTranslator().TranslateToLambda(bound);
        var compiled = translated.Compile();

        var actualResult = compiled.DynamicInvoke(args);
        Assert.AreEqual(expectedResult, actualResult);
    }
}