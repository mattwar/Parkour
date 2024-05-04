using Parkour;
using Parkour.Semantics;
using Parkour.Binding;
using Parkour.Reflection;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class DeclarationTests
{
    private readonly ReflectionSymbols _reflectionSymbols;

    public DeclarationTests()
    {
        _reflectionSymbols = ReflectionSymbols.CurrentMscorlib;
    }   

    [TestMethod]
    public void TestBindClass()
    {
        TestBind(
            [Class("C", [])],
            ["C"]);
    }

    [TestMethod]
    public void TestBindClassWithReferencesToSelf()
    {
        //// base types (illegal, but should find reference)
        //TestBind(
        //    [Class("C", [Name("C")], [])],
        //    ["C"]);

        // field that refers to self
        TestBind(
            Class("C", [Field("F", Name("C"))]),
            ["C.F"]);

        TestBind(
            Namespace("N", 
                [Class("C", [Field("F", Name("C"))])]),
            ["N.C.F"]);
    }

    [TestMethod]
    public void TestBindClassWithTypeParameters()
    {
        TestBind(
            [Class("C",
                [TypeParameter("T")],
                [],
                [])
                ],
            ["C`1"]);
    }

    [TestMethod]
    public void TestBindClassInNamespace()
    {
        TestBind(
            [Namespace("N", [Class("C", [])])],
            ["N", "N.C"]);
    }

    [TestMethod]
    public void TestBindMethodInClass()
    {
        TestBind(
            [Class("C",
                [Method("M", [], Name("System.Int32"), Void())])],
            ["C", "C.M"]);
    }

    [TestMethod]
    public void TestBindFieldInClass()
    {
        TestBind([
            Class("C", [
                Method("M", [], Name("System.Int32"), Void())
                ])
            ],
            ["C", "C.M"]);
    }

    [TestMethod]
    public void TestBindPropertyInClass()
    {
        TestBind(
            [Class("C",
                [Property("P", Name("System.Int32"))])
                ],
            ["C", "C.P"]);
    }

    [TestMethod]
    public void TestBindUsing()
    {
        TestBind([
            Using(Symbol("System")),

            Class("C", [
                Property("P", Name("Int32"))
                ])
            ],
            ["C.P"]
            );

        TestBind([
            Using("X", Symbol("System")),

            Class("C", [
                Property("P", Name("X").Member("Int32"))
                ])
            ],
            ["C.P"]
            );
    }

    private void TestBind(Declaration declaration, string[] expectedSymbols)
    {
        TestBind([declaration], expectedSymbols);
    }

    private void TestBind(Declaration[] declarations, string[] expectedSymbols)
    {
        var binding = new StandardBinder().BindDeclarations(declarations.ToImmutableList(), _reflectionSymbols);

        Assert.AreEqual(declarations.Length, binding.BoundDeclarations.Count, "bound declarations count");

        var combinedSymbols = CombinedSymbols.Create([_reflectionSymbols.GlobalNamespace, binding.DeclaredSymbols]);

        foreach (var path in expectedSymbols)
        {
            var symbol = combinedSymbols.GetFirstSymbolFromPath(path);
            Assert.IsNotNull(symbol, $"symbol '{path}' not found");
        }
    }
}