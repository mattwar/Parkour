using Parkour;
using Parkour.Expressions;
using Parkour.Analysis;
using Parkour.Symbols;
using static Parkour.Expressions.SemanticFactory;

namespace Tests;

[TestClass]
public class DeclarationTests
{
    private readonly CommonSymbols _symbols;
    private readonly BindingScope _defaultTestScope;

    public DeclarationTests()
    {
        _symbols = RuntimeSymbols.GetOrCreateCommonSymbols();
        _defaultTestScope = ExpressionTests.CreateBindingScope(_symbols);
    }

    [TestMethod]
    public void TestBinding()
    {
        TestBind(Class("C", SymbolAccess.Public, SymbolModifier.None));
    }

    private void TestBind(Declaration declaration)
    {
        var binding = DeclarationBinding.Create(
            new[] { declaration }, 
            new[] { _symbols.GlobalNamespace });

        var bound = binding.Bound;
    }
}