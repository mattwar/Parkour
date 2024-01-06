using Parkour;
using Parkour.Semantics;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class SimpleBindingTests
{
    private readonly SymbolModel _symbols;
    private readonly Intrinsics _intrinsics;


    public SimpleBindingScope DefaultTestScope { get; }

    public SimpleBindingTests()
    {
        _symbols = new RuntimeSymbolModel();
        _intrinsics = new Intrinsics(_symbols);

        this.DefaultTestScope = SimpleBindingScope.Default.AddAmbientSymbols(
            ImmutableList.Create<Symbol>(
                _symbols.Int32, 
                _symbols.Int64, 
                _symbols.Single, 
                _symbols.Double, 
                _symbols.String)
            );
    }

    [TestMethod]
    public void TestConstant()
    {
        TestBinding(Constant(1), _symbols.Int32);
    }

    [TestMethod]
    public void TestOperators()
    {
        // Int32
        TestBinding(Add(Constant(1), Constant(2)), _symbols.Int32);

        TestBinding(Subtract(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Multiply(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Divide(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Remainder(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(Negate(Constant(1)), _symbols.Int32);

        TestBinding(Equal(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(NotEqual(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(LessThan(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(LessThanOrEqual(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(GreaterThan(Constant(1), Constant(2)), _symbols.Boolean);
        TestBinding(GreaterThanOrEqual(Constant(1), Constant(2)), _symbols.Boolean);

        TestBinding(BitwiseAnd(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseOr(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseXor(Constant(1), Constant(2)), _symbols.Int32);
        TestBinding(BitwiseNot(Constant(1)), _symbols.Int32);

        // boolean / logical
        TestBinding(And(Constant(true), Constant(true)), _symbols.Boolean);
        TestBinding(Or(Constant(true), Constant(true)), _symbols.Boolean);
        TestBinding(Not(Constant(true)), _symbols.Boolean);

        // string
        TestBinding(Add(Constant("one"), Constant("two")), _symbols.String);
        TestBinding(Equal(Constant("one"), Constant("two")), _symbols.Boolean);
    }

    [TestMethod]
    public void TestReference()
    {
        TestBinding(Reference("Int32"), _symbols.Type, _symbols.Int32);
    }

    [TestMethod]
    public void TestPath()
    {
        TestBinding(Path(Reference("Int32"), Reference("MaxValue")), _symbols.Int32);
    }

    [TestMethod]
    public void TestCall()
    {
        TestBinding(Call(Path(Constant(1), Reference("ToString"))), _symbols.String);
    }

    private void TestBinding(Semantic expression, Symbol.Type? expectedResultType = null, Symbol? expectedReferencedSymbol = null, SimpleBindingScope? scope = null)
    {
        var binder = new SemanticBinder<SimpleBindingScope>(_symbols, _intrinsics);
        var bound = binder.Bind(expression, scope ?? DefaultTestScope);

        Assert.IsFalse(bound.ContainsUnknowns, "expression contains unknowns after binding");

        if (expectedResultType != null)
        {
            var actualResultType = bound.ResultType;
            Assert.IsTrue(TypeEqualityComparer.Instance.Equals(expectedResultType, actualResultType), $"result type expected: {expectedResultType.Name} actual: {actualResultType.Name}");
        }

        if (expectedReferencedSymbol != null)
        {
            var actualReferencedSymbol = bound.ReferencedSymbol;
            Assert.IsTrue(SymbolEqualityComparer.Instance.Equals(expectedReferencedSymbol, actualReferencedSymbol), $"referenced symbol expected: {expectedReferencedSymbol?.Name ?? "null"} actual: {actualReferencedSymbol?.Name ?? "null"}");
        }
    }
}
