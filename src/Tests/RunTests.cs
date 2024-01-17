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
    public void TestConstant()
    {
        TestRun(
            Constant(1), 
            1);
        TestRun(
            Constant("one"), 
            "one");
    }

    [TestMethod]
    public void TestAdd()
    {
        TestRun(
            Add(Constant(1), Constant(2)), 
            3);
    }

    [TestMethod]
    public void TestParameters()
    {
        TestRun(
            Lambda([Parameter("x", TypeReference(_symbols.Int32))], Reference("x")), 
            [1], 
            1);
        
        TestRun(
            Lambda([Parameter("x", TypeReference(_symbols.Int32))], Add(Reference("x"), Constant(1))), 
            [2], 
            3);
    }

    [TestMethod]
    public void TestPath()
    {
        TestRun(
            Path(TypeReference(_symbols.Int32), Reference("MaxValue")), 
            Int32.MaxValue);
    }

    [TestMethod]
    public void TestCall()
    {
        TestRun(
            Call(Path(Constant(1), "ToString")),
            "1");
    }

    private void TestRun(Expression expression, object expectedResult, BindingScope? scope = null) =>
        TestRun(expression, [], expectedResult, scope);

    private void TestRun(Expression expression, object[] args, object expectedResult, BindingScope? scope = null)
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