using Parkour;
using Parkour.Semantics;
using Parkour.Binding;
using Parkour.Reflection;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class BindingTests
{
    private readonly ReflectionSymbols _reflectionSymbols;

    public BindingTests()
    {
        _reflectionSymbols = ReflectionSymbols.CurrentMscorlib;
    }

    private static SymbolExpression VoidType = Symbol("System.Void");
    private static SymbolExpression Int16Type = Symbol("System.Int16");
    private static SymbolExpression Int32Type = Symbol("System.Int32");
    private static SymbolExpression Int64Type = Symbol("System.Int64");
    private static SymbolExpression SingleType = Symbol("System.Single");
    private static SymbolExpression DoubleType = Symbol("System.Double");
    private static SymbolExpression StringType = Symbol("System.String");
    private static Expression Int32ArrayType = Int32Type.Array();
    private static Expression StringArrayType = StringType.Array();
    private static SymbolExpression ListTType = Symbol("System.Collections.Generic.List`1");
    private static Expression ListInt32Type = ListTType.Construct([Int32Type]);
    private static Expression ListStringType = ListTType.Construct([StringType]);

    [TestMethod]
    public void TestBindClass()
    {
        // bind it and find it
        TestBind(
            [Class("C", [])],
            ["C"]);

        // bint in namespace
        TestBind(
            [Namespace("N", [Class("C", [])])],
            ["N", "N.C"]);
    }

    [TestMethod]
    public void TestBindClassConstructor()
    {
        // bind it and default construct it.
        TestBind(
            [Class("C", [])],
            ["C", "C.[.ctor]"],
            New(Name("C")),
            expectedResultType: "C");

        // bind it with explicit constructor
        TestBind(
            [Class("C", [Constructor()])],
            ["C", "C.[.ctor]"],
            New(Name("C")),
            expectedResultType: "C");

        // constructor has parameter
        TestBind([
            Class("C", [
                Constructor(
                    [Parameter("x", Int32Type)], 
                    Block())
                ])
            ],
            ["C", "C.[.ctor]"],
            New(Name("C"), [Constant(1)]),
            expectedResultType: "C");
    }

    [TestMethod]
    public void TestBindClassField()
    {
        // instance field
        TestBind(
            [Class("C", [Field("F", Int32Type)])],
            ["C.F"],
            New(Name("C")).Member("F"),
            expectedResultType: "System.Int32");

        // static field
        TestBind(
            [Class("C", [Field("F", Int32Type).WithModifiers(SymbolModifier.Static)])],
            ["C.F"],
            Name("C").Member("F"),
            expectedResultType: "System.Int32");
    }

    [TestMethod]
    public void TestBindClassMethod()
    {
        // instance void method
        TestBind(
            [Class("C",
                [Method("M", [], VoidType, Block())])],
            ["C.M"],
            New(Name("C")).Member("M").Call(),
            expectedResultType: "System.Void"
            );

        // instance int returning method
        TestBind(
            [Class("C",
                [Method("M", [], Int32Type, Constant(1))])],
            ["C.M"],
            New(Name("C")).Member("M").Call(),
            expectedResultType: "System.Int32"
            );

        // instance int returning method with parameters
        TestBind(
            [Class("C",
                [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1))])],
            ["C.M"],
            New(Name("C")).Member("M").Call([Constant(1)]),
            expectedResultType: "System.Int32"
            );

        // static void method
        TestBind(
            [Class("C",
                [Method("M", [], VoidType, Block()).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(),
            expectedResultType: "System.Void"
            );

        // static int returning method
        TestBind(
            [Class("C",
                [Method("M", [], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(),
            expectedResultType: "System.Int32"
            );

        // static int returning method with parameter
        TestBind(
            [Class("C",
                [Method("M", [Parameter("p", Int32Type)], Int32Type, Constant(1)).WithModifiers(SymbolModifier.Static)]
                )],
            ["C.M"],
            Name("C").Member("M").Call(Constant(1)),
            expectedResultType: "System.Int32"
            );
    }

    [TestMethod]
    public void TestBindClassProperty()
    {
        // instance property
        TestBind(
            [Class("C", [
                Property("P", Int32Type)])
                ],
            ["C.P"],
            New(Name("C")).Member("P"),
            expectedResultType: "System.Int32");

        // static property
        TestBind(
            [Class("C", [
                Property("P", SymbolAccess.Public, SymbolModifier.Static, Int32Type)])
                ],
            ["C.P"],
            Name("C").Member("P"),
            expectedResultType: "System.Int32");
    }

    [TestMethod]
    public void TestBindClassIndexer()
    {
        TestBind(
            [Class("C",
                [
                    Indexer(
                        Symbol("System.Int32"),
                        [Parameter("index", Symbol("System.Int32"))],
                        Name("index"),
                        null)
                ]
                )],
            ["C", "C.Item"]);
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
            [Class("C", []).WithTypeParameters([TypeParameter("T")])],
            ["C`1"],
            New(Name("C").Construct([Int32Type])),
            expectedResultType: "C[System.Int32]"
            );
    }

    [TestMethod]
    public void TestBindStruct()
    {
        // bind it and find it
        TestBind(
            [Struct("S", [])],
            ["S"]);

        // bind it and construct it
        TestBind(
            [Struct("S", [])],
            ["S", "S.[.ctor]"],
            New(Name("S")),
            expectedResultType: "S");
    }

    [TestMethod]
    public void TestBindInterface()
    {
        TestBind(
            [Interface("I", [])],
            ["I"]);
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

    [TestMethod]
    public void TestIsType()
    {
        TestBind(
            IsType(Name("System").Member("Int32"), Symbol("System.Int32")),
            "System.Boolean"
            );
    }

    [TestMethod]
    public void TestTypeOf()
    {
        TestBind(
            TypeOf(Symbol("System.Int32")),
            "System.Type"
            );
    }

    [TestMethod]
    public void TestExpressionAdd()
    {
        // Int32
        TestBind(
            Add(Constant(1), Constant(2)),
            expectedResultType: Int32Type.FullName);

        // Int64
        TestBind(
            Add(Constant(1L), Constant(2L)),
            expectedResultType: Int64Type.FullName);

        // promote Int32 to Int64
        TestBind(
            Add(Constant(1), Constant(2L)),
            expectedResultType: Int64Type.FullName);

        TestBind(
            Add(Constant(1L), Constant(2)),
            expectedResultType: Int64Type.FullName);
    }

    private void TestBind(
        Declaration declaration, 
        string[] expectedSymbols)
    {
        TestBind([declaration], expectedSymbols);
    }

    private void TestBind(
        Declaration[] declarations, 
        string[] expectedSymbols)
    {
        TestBind(declarations, expectedSymbols, null, null, null);
    }

    private void TestBind(
        Expression expression, 
        string? expectedResultType = null, 
        string? expectedReferencedSymbol = null)
    {
        TestBind(null, null, expression, expectedResultType, expectedReferencedSymbol);
    }

    private void TestBind(
        Declaration[] declarations,
        Expression expression,
        string? expectedResultType = null,
        string? expectedReferencedSymbol = null)
    {
        TestBind(declarations, null, expression, expectedResultType, expectedReferencedSymbol);
    }

    private void TestBind(
        Declaration[]? declarations,
        string[]? expectedSymbols,
        Expression? expression,
        string? expectedResultType = null, 
        string? expectedReferencedSymbol = null)
    {
        var binder = new StandardDeclarationBinder();

        SymbolTable bindSymbols = _reflectionSymbols;

        if (declarations != null)
        {
            var declBinding = binder.BindDeclarations(declarations.ToImmutableList(), _reflectionSymbols);

            if (declBinding.Diagnostics.Count > 0)
            {
                Assert.Fail($"Unexpected declaration diagnostics:\n{declBinding.Diagnostics[0]}");
            }

            if (expectedSymbols != null)
            {
                foreach (var path in expectedSymbols)
                {
                    var symbol = declBinding.Symbols.GetSymbol(path);
                    Assert.IsNotNull(symbol, $"symbol '{path}' not found");
                }
            }

            bindSymbols = declBinding.Symbols;
        }

        if (expression != null)
        {
            var binding = binder.BindExpression(expression, bindSymbols);

            if (binding.Diagnostics.Count > 0)
            {
                Assert.Fail($"Unexpected expression diagnostics:\n{binding.Diagnostics[0]}");
            }

            if (expectedResultType != null)
            {
                Assert.IsNotNull(binding.Expression.ResultType);
                Assert.AreEqual(expectedResultType, binding.Expression.ResultType.FullName);
            }

            if (expectedReferencedSymbol != null)
            {
                Assert.IsNotNull(binding.Expression.ReferencedSymbol);
                Assert.AreEqual(expectedReferencedSymbol, binding.Expression.ReferencedSymbol.FullName);
            }
        }
    }
}