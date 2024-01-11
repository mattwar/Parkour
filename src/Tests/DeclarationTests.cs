using Parkour;
using Parkour.Expressions;
using Parkour.Analysis;
using Parkour.Symbols;
using static Parkour.Expressions.SemanticFactory;

namespace Tests;

[TestClass]
public class DeclarationTests
{
    private readonly NamespaceSymbol _runtimeGlobalNamespace;

    public DeclarationTests()
    {
        _runtimeGlobalNamespace = RuntimeSymbols.GetOrCreateGlobalNamespace();
    }   

    [TestMethod]
    public void TestBindClass()
    {
        TestBind(
            [Class("C")],
            ["C"]);
    }

    [TestMethod]
    public void TestBindClassInNamespace()
    {
        TestBind(
            [Namespace("N", Class("C"))],
            ["N", "N.C"]);
    }

    [TestMethod]
    public void TestBindMethodInClass()
    {
        TestBind(
            [Class("C",
                [Method("M", [], Path(Reference("System"), "Int32"), Void())])],
            ["C", "C.M"]);
    }

    private void TestBind(Declaration[] declarations, string[] expectedSymbols)
    {
        var binding = DeclarationBinding.Create(
            declarations, 
            new[] { _runtimeGlobalNamespace });

        Assert.AreEqual(declarations.Length, binding.Bound.Count, "bound declarations count");

        foreach (var path in expectedSymbols)
        {
            var symbol = binding.GlobalNamespace.GetFirstSymbolFromPath(path);
            Assert.IsNotNull(symbol, $"symbol '{path}' not found");
        }
    }
}