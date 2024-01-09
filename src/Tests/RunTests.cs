using Parkour;
using Parkour.Analysis;
using Parkour.Symbols;
using Parkour.Expressions;
using Parkour.Execution;
using static Parkour.Expressions.ExpressionFactory;

namespace Tests;

[TestClass]
public class RunTests
{
    private readonly CommonSymbols _symbols;
    private readonly BindingScope _defaultTestScope;

    public RunTests()
    {
        _symbols = RuntimeSymbols.GetOrCreateCommonSymbols();
        _defaultTestScope = ExpressionBinderTests.CreateBindingScope(_symbols);
    }

    [TestMethod]
    public void TestConstant()
    {
        TestRun(Constant(1), 1);
        TestRun(Constant("one"), "one");
    }

    [TestMethod]
    public void TestAdd()
    {
        TestRun(Add(Constant(1), Constant(2)), 3);
    }

    [TestMethod]
    public void TestParameters()
    {
        TestRun(Function(new[] { Parameter("x", Reference("Int32")) }, Reference("x")), 1, 1);
        //TestRun(Function(new[] { Parameter("x", Reference("Int32")) }, Add(Reference("x"), Constant(1))), 2, 1);
    }

    [TestMethod]
    public void TestPath()
    {
        TestRun(
            Path(Reference("Int32"), Reference("MaxValue")), 
            Int32.MaxValue);
    }

    [TestMethod]
    public void TestCall()
    {
        TestRun(
            Call(Path(Constant(1), "ToString")),
            "1");
    }

    private void TestRun(Expression expression, object expectedResult, BindingScope? scope, object[] args)
    {
        if (!(expression is FunctionExpression))
            expression = Function(expression);

        var binder = new ExpressionBinder(_symbols.GlobalNamespace, scope ?? _defaultTestScope);
        var bound = (FunctionExpression)binder.Bind(expression);

        if (bound.ContainsDiagnostics)
        {
            var dx = bound.GetContainedDiagnostics()[0];
            Assert.Fail($"The expression contains diagnostics: {dx.Message}");
        }

        Assert.IsFalse(bound.ContainsDiagnostics, "expression contains diagnostics");
        Assert.IsFalse(bound.ContainsUnknowns, "expression contains unknowns");

        var translated = new ExpressionTranslator().TranslateToLambda(bound);
        var compiled = translated.Compile();

        var actualResult = compiled.DynamicInvoke(args);
        Assert.AreEqual(expectedResult, actualResult);
    }

    private void TestRun(Expression expression, object expectedResult, params object[] args) =>
        TestRun(expression, expectedResult, default, args);
}