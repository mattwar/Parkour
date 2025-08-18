using System.Reflection;
using Parkour.Reflection;
using Parkour.Semantics;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;
using static Tests.TestHelpers;

namespace Tests;

/// <summary>
/// These tests are specialized in separate test libraries.
/// </summary>
public abstract class EmitterTests
{
    #region declarations

    [TestMethod]
    public void TestDeclaration_Class()
    {
        // class emitted
        TestEmit(
            Class("C", []),
            TypeOf(Symbol("C")),
            result =>
            {
                Assert.IsNotNull(result);
                var type = result as Type;
                Assert.IsNotNull(type);
                Assert.AreEqual("C", type.Name);
            }
            );

        // explicit constructor
        TestEmit(
            Class("C", [Constructor()]),
            Symbol("C").New(),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });

        // instance field
        TestEmit(
            Class("C", [Field("F", Int32Type)]),
            Symbol("C").New().Member("F"),
            0
            );

        // static method
        TestEmit(
            Class("C", [Method("M", [], Int32Type, Block(Constant(123))).WithModifiers(SymbolModifier.Static)]),
            test: Symbol("C.M").Call(),
            expectedResult: 123
            );

        // instance method
        TestEmit(
            Class("C", [Method("M", [], Int32Type, Block(Constant(123)))]),
            test: Symbol("C").New().Member("M").Call(),
            expectedResult: 123
            );

        // instance property
        TestEmit(
            Class("C", [Property("P", Int32Type)]),
            Symbol("C").New().Member("P"),
            expectedResult: 0
            );

        // inside namespace
        TestEmit(
            Namespace("N", [Class("C", [])])
            );
    }

    [TestMethod]
    public void TestDeclaration_Attributes()
    {
        TestEmit(
            Class("C")
                .WithAttributes([
                    Attribute(Symbol("System.ObsoleteAttribute"), [Constant("Message")])
                    ]),
            TypeOf(Symbol("C")),
            result =>
            {
                Assert.IsInstanceOfType(result, typeof(Type));
                var attr = ((Type)result!).GetCustomAttribute<ObsoleteAttribute>();
                Assert.IsNotNull(attr);
                Assert.AreEqual("Message", attr.Message);
            }
            );
    }

    [TestMethod]
    public void TestDeclaration_Struct()
    {
        // struct emitted
        TestEmit(
            Struct("S", []),
            TypeOf(Symbol("S")),
            result =>
            {
                Assert.IsNotNull(result);
                var type = result as Type;
                Assert.IsNotNull(type);
                Assert.AreEqual("S", type.Name);
            }
            );
    }

    [TestMethod]
    public void TestDeclaration_Interface()
    {
        // interface emitted
        TestEmit(
            Interface("I", []),
            TypeOf(Symbol("I")),
            result =>
            {
                Assert.IsNotNull(result);
                var type = result as Type;
                Assert.IsNotNull(type);
                Assert.AreEqual("I", type.Name);
            }
            );
    }

    [TestMethod]
    public void TestDeclaration_Constructor()
    {
        // explicit constructor
        TestEmit(
            Class("C", [Constructor()]),
            Symbol("C").New(),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });

        // with parameter
        TestEmit(
            Class("C", [Constructor([Parameter("P", Int32Type)], Block())]),
            Symbol("C").New([Constant(123)]),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });
    }

    [TestMethod]
    public void TestDeclaration_Field()
    {
        // instance field
        TestEmit(
            Class("C", [Field("F", Int32Type)]),
            Symbol("C").New().Member("F"),
            0
            );

        // static field
        TestEmit(
            Class("C", [Field("F", Int32Type).WithModifiers(SymbolModifier.Static)]),
            Symbol("C").Member("F"),
            0
            );

        // instance field with initializer
        TestEmit(
            Class("C", [Field("F", Int32Type, Constant(10))]),
            Symbol("C").New().Member("F"),
            10
            );

        // static field with initializer
        TestEmit(
            Class("C", [Field("F", Int32Type, Constant(10)).WithModifiers(SymbolModifier.Static)]),
            Symbol("C").Member("F"),
            10
            );

        // const field with initializer
        TestEmit(
            Class("C", [Field("F", Int32Type, Constant(10)).WithModifiers(SymbolModifier.Static | SymbolModifier.Constant)]),
            Symbol("C").Member("F"),
            10
            );
    }

    [TestMethod]
    public void TestDeclaration_Method()
    {
        // instance method
        TestEmit(
            Class("C", [Method("M", [], Int32Type, Block(Constant(123)))]),
            test: Symbol("C").New().Member("M").Call(),
            expectedResult: 123
            );

        // static method
        TestEmit(
            Class("C", [Method("M", [], Int32Type, Block(Constant(123))).WithModifiers(SymbolModifier.Static)]),
            test: Symbol("C.M").Call(),
            expectedResult: 123
            );

        // void method
        TestEmit(
            Class("C", [Method("M", [], VoidType, Block())]),
            test: Symbol("C").New().Member("M").Call(),
            expectedResult: null
            );

        // with parameters
        TestEmit(
            Class("C", [Method("M", [Parameter("P", Int32Type)], Int32Type, Name("P"))]),
            test: Symbol("C").New().Member("M").Call([Constant(123)]),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestDeclaration_Property()
    {
        // instance auto property
        TestEmit(
            Class("C", [Property("P", Int32Type)]),
            Symbol("C").New().Member("P"),
            expectedResult: 0
            );

        // static auto property
        TestEmit(
            Class("C", [Property("P", Int32Type).WithModifiers(SymbolModifier.Static)]),
            Symbol("C").Member("P"),
            expectedResult: 0
            );
    }

    [TestMethod]
    public void TestDeclaration_Indexer()
    {
        // instance indexer
        TestEmit(
            Class("C", [
                Indexer(Int32Type, [Parameter("index", Int32Type)], Name("index"), null),
                ]),
            Symbol("C").New().Element(Constant(123)),
            expectedResult: 123
            );

        // static indexer
        TestEmit(
            Class("C", [
                Indexer(Int32Type, [Parameter("index", Int32Type)], Name("index"), null)
                    .WithModifiers(SymbolModifier.Static),
                ]),
            Symbol("C").Element(Constant(123)),
            expectedResult: 123
            );
    }

#if false
    [TestMethod]
    public void TestDeclaration_GlobalMethod()
    {
        TestEmit(
            Method("M", [], Int32Type, Block(Constant(1))),
            Name("M").Call(),
            expectedResult: 1
            );
    }
#endif

    #endregion

    #region Expressions

    [TestMethod]
    public void TestExpression_Assign()
    {
        // variable assign after initialized
        TestEmit(
            Block(
                Variable("x", Constant(1)),
                Name("x").Assign(Constant(2))
                ),
            expectedResult: 2);

        // array element
        TestEmit(
            Block(
                Variable("a", NewArray(Int32Type, Constant(2))),
                Element(Name("a"), Constant(0)).Assign(Constant(123))
                ),
            expectedResult: 123);
    }

    [TestMethod]
    public void TestExpression_AsType()
    {
        TestEmit(
            Constant("Hello").AsType(StringType),
            expectedResult: "Hello");

        TestEmit(
            Constant(123).AsType(Int32Type),
            expectedResult: 123);

        TestEmit(
            Constant(123).AsType(StringType),
            expectedResult: null);

        TestEmit(
            Constant("Hello").AsType(Symbol("System.IEquatable`1").Construct([StringType])),
            expectedResult: "Hello");

        TestEmit(
            Constant(123).AsType(Symbol("System.IEquatable`1").Construct([Int32Type])),
            expectedResult: 123);

        TestEmit(
            Constant(123).AsType(Symbol("System.IEquatable`1").Construct([StringType])),
            expectedResult: null);
    }

    [TestMethod]
    public void TestExpression_Block()
    {
        TestEmit(
            Block(),
            expectedResult: null);

        TestEmit(
            Block(Constant(123)),
            expectedResult: 123);
    }

    [TestMethod]
    public void TestExpression_Branch()
    {
        TestEmit(
            Block(
                Branch("end", Constant(456)),
                Constant(123),
                Label("end", Int32Type)
                ),
            expectedResult: 456);
    }

    [TestMethod]
    public void TestExpression_Call()
    {
        TestEmit(
            [Class("C", [Method("M", [], Int32Type, Constant(123)).WithModifiers(SymbolModifier.Static)])],
            Symbol("C.M").Call(),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestExpression_Condition()
    {
        TestEmit(
            Condition(Constant(true), Constant(1), Constant(2)),
            expectedResult: 1);

        TestEmit(
            Condition(Constant(false), Constant(1), Constant(2)),
            expectedResult: 2);

        TestEmit(
            Condition(Constant(false), Constant(1), Default(Int32Type)),
            expectedResult: 0);

        TestEmit(
            Condition(Constant(true), Constant(5)),
            expectedResult: 5);

        TestEmit(
            Condition(Constant(false), Constant(5)),
            expectedResult: null); // due to test method definition common type is object
    }

    [TestMethod]
    public void TestExpression_Constant()
    {
        TestEmit(
            Constant(1), 
            expectedResult: 1);
    }

    [TestMethod]
    public void TestExpression_Construct()
    {
        TestEmit(
            New(ListTType.Construct([Int32Type])),
            expectedResult: new List<Int32>()
            );
    }

    [TestMethod]
    public void TestExpression_Convert()
    {
        TestEmit(
            Convert(Constant(1), DoubleType),
            expectedResult: 1.0
            );
    }

    [TestMethod]
    public void TestExpression_Default()
    {
        TestEmit(
            Default(Int32Type),
            expectedResult: 0
            );

        TestEmit(
            Default(StringType),
            expectedResult: null
            );
    }

    [TestMethod]
    public void TestExpression_Element()
    {
        // access sz array element
        TestEmit(
            Block(
                Variable("a", NewArray(Int32Type, [Constant(5)])),
                Name("a").Element(Constant(0))
                ),
            expectedResult: 5);

        // assign sz array element
        TestEmit(
            Block(
                Variable("a", NewArray(Int32Type, Constant(10))),
                Name("a").Element(Constant(0)).Assign(Constant(123))
                ),
            expectedResult: 123);

        // access indexer element
        TestEmit(
            Block(
                Variable("list", New(ListInt32Type, [NewArray(Int32Type, [Constant(7), Constant(11)])])),
                Name("list").Element(Constant(0))
                ),
            expectedResult: 7);

        // assign indexer element
        TestEmit(
            Block(
                Variable("list", New(ListInt32Type, [NewArray(Int32Type, [Constant(7), Constant(11)])])),
                Name("list").Element(Constant(0)).Assign(Constant(123))
                ),
            expectedResult: 123);
    }

    [TestMethod]
    public void TestExpression_IsType()
    {
        TestEmit(
            Constant("Hello").IsType(StringType),
            expectedResult: true);

        TestEmit(
            Constant(1).IsType(Int32Type),
            expectedResult: true);

        TestEmit(
            Constant("Hello").ConvertTo(ObjectType).IsType(StringType),
            expectedResult: true);

        TestEmit(
            Constant(1).ConvertTo(ObjectType).IsType(Int32Type),
            expectedResult: true);

        TestEmit(
            Constant("Hello").IsType(Symbol("System.IEquatable`1").Construct([StringType])),
            expectedResult: true);

        TestEmit(
            Constant(123).IsType(Symbol("System.IEquatable`1").Construct([Int32Type])),
            expectedResult: true);

        TestEmit(
            Constant("Hello").IsType(Int32Type),
            expectedResult: false);

        TestEmit(
            Constant(1).IsType(StringType),
            expectedResult: false);

        TestEmit(
            IsType(Convert(Constant("Hello"), ObjectType), Int32Type),
            expectedResult: false);

        TestEmit(
            Constant(1).ConvertTo(ObjectType).IsType(StringType),
            expectedResult: false);
    }

    [TestMethod]
    public void TestExpression_Label()
    {
        TestEmit(
            Block(
                Branch("end"),
                Label("end")),
            expectedResult: null
            );
    }

    [TestMethod]
    public void TestExpression_Loop()
    {
        // loop with value returning break
        TestEmit(
            Loop(Break(Constant(123))), 
            expectedResult: 123);

        // for loop
        TestEmit(
            For("x", Constant(1), Constant(10), Constant(1), Block()), 
            expectedResult: 10);

        // while loop
        TestEmit(
            While(Constant(false), Block()), 
            expectedResult: null);

        TestEmit(
            While(Constant(true), Break(Constant(123))), 
            expectedResult: 123);
    }

    [TestMethod]
    public void TestExpression_Member()
    {
        // instance property on class
        TestEmit(
            Constant("Hello").Member("Length"),
            expectedResult: 5
            );

        // instance field on struct
        TestEmit(
            Symbol("System.ValueTuple`1").Construct([Int32Type]).New([Constant(123)]).Member("Item1"),
            expectedResult: 123
            );

        // static field on class
        TestEmit(
            Symbol("System.DBNull").Member("Value"),
            expectedResult: System.DBNull.Value
            );

        // constant field on struct
        TestEmit(
            Symbol("System.Int32").Member("MaxValue"),
            expectedResult: int.MaxValue
            );
    }

    [TestMethod]
    public void TestExpression_Name()
    {
        // variable name
        TestEmit(
            Block(
                Variable("x", Constant(123)),
                Name("x")
                ),
            expectedResult: 123
            );

        // symbol table name
        TestEmit(
            Name("System").Member("Int32").Member("MaxValue"),
            expectedResult: int.MaxValue
            );

        // field reference
        TestEmit(
            [Class("C", [Field("F", Int32Type), Method("M", [], Int32Type, Name("F"))])],
            Symbol("C").New().Member("M").Call(),
            expectedResult: 0
            );

        // parameter reference
        TestEmit(
            [Class("C", [Method("M", [Parameter("P", Int32Type)], Int32Type, Name("P"))])],
            Symbol("C").New().Member("M").Call(Constant(123)),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestExpression_New()
    {
        // new object
        TestEmit(
            New(ObjectType),
            expectedResult: new object());

        // new generic type
        TestEmit(
            New(ListInt32Type),
            expectedResult: new List<int>());

        // new generic type with constructor args
        TestEmit(
            New(ListInt32Type, [NewArray(Symbol("System.Int32"), [Constant(7), Constant(11)])]),
            expectedResult: new List<int>() { 7, 11 }
            );
    }

    [TestMethod]
    public void TestExpression_NewArray()
    {
        // new int[2]
        TestEmit(
            Int32Type.NewArray(Constant(2)),
            expectedResult: new int[2]
            );

        // new int[] {}
        TestEmit(
            Int32Type.NewArray([]),
            expectedResult: new int[] { }
            );

        // new int[] {1, 2}
        TestEmit(
            Int32Type.NewArray([Constant(1), Constant(2)]),
            expectedResult: new int[] { 1, 2 }
            );

        // new int[2,3]
        TestEmit(
            StringType.NewMultiDimensionalArray([Constant(2), Constant(3)]),
            expectedResult: new string[2, 3]);
    }

    [TestMethod]
    public void TestExpression_Operator_Add()
    {
        TestEmit(
            Add(Constant(1), Constant(2)),
            expectedResult: 3);

        // + on strings is concat
        TestEmit(
            Add(Constant("one"), Constant("two")),
            expectedResult: "onetwo");

        // add with local variable
        TestEmit(
            Block(
                Variable("x", Constant(1)),
                Add(Name("x"), Constant(2))),
            expectedResult: 3);
    }

    [TestMethod]
    public void TestExpression_Operator_Subtract()
    {
        TestEmit(
            Subtract(Constant(3), Constant(1)),
            expectedResult: 2);
    }

    [TestMethod]
    public void TestExpression_Operator_Multiply()
    {
        TestEmit(
            Multiply(Constant(3), Constant(2)),
            expectedResult: 6);
    }

    [TestMethod]
    public void TestExpression_Operator_Divide()
    {
        TestEmit(
            Divide(Constant(6), Constant(2)),
            expectedResult: 3);

        TestEmit(
            Divide(Constant(5), Constant(2)),
            expectedResult: 2);
    }

    [TestMethod]
    public void TestExpression_Operator_Remainder()
    {
        TestEmit(
            Remainder(Constant(6), Constant(2)),
            expectedResult: 0);

        TestEmit(
            Remainder(Constant(5), Constant(2)),
            expectedResult: 1);
    }

    [TestMethod]
    public void TestExpression_Operator_BitwiseAnd()
    {
        TestEmit(
            BitwiseAnd(Constant(7), Constant(3)),
            expectedResult: 3);

        TestEmit(
            BitwiseAnd(Constant(5), Constant(3)),
            expectedResult: 1);

        TestEmit(
            BitwiseAnd(Constant(1), Constant(2)),
            expectedResult: 0);
    }

    [TestMethod]
    public void TestExpression_Operator_Equal()
    {
        TestEmit(
            Equal(Constant(1), Constant(2)),
            expectedResult: false);

        TestEmit(
            Equal(Constant(1), Constant(1)),
            expectedResult: true);
    }

    [TestMethod]
    public void TestExpression_Operator_NotEqual()
    {
        TestEmit(
            NotEqual(Constant(1), Constant(2)),
            expectedResult: true);

        TestEmit(
            NotEqual(Constant(1), Constant(1)),
            expectedResult: false);
    }

    [TestMethod]
    public void TestExpression_Operator_LessThan()
    {
        TestEmit(
            LessThan(Constant(1), Constant(2)),
            expectedResult: true);

        TestEmit(
            LessThan(Constant(1), Constant(1)),
            expectedResult: false);

        TestEmit(
            LessThan(Constant(2), Constant(1)),
            expectedResult: false);
    }

    [TestMethod]
    public void TestExpression_Operator_LessThanOrEqual()
    {
        TestEmit(
            LessThanOrEqual(Constant(1), Constant(2)),
            expectedResult: true);

        TestEmit(
            LessThanOrEqual(Constant(1), Constant(1)),
            expectedResult: true);

        TestEmit(
            LessThanOrEqual(Constant(2), Constant(1)),
            expectedResult: false);
    }

    [TestMethod]
    public void TestExpression_Operator_GreaterThan()
    {
        TestEmit(
            GreaterThan(Constant(1), Constant(2)),
            expectedResult: false);

        TestEmit(
            GreaterThan(Constant(1), Constant(1)),
            expectedResult: false);

        TestEmit(
            GreaterThan(Constant(2), Constant(1)),
            expectedResult: true);
    }

    [TestMethod]
    public void TestExpression_Operator_GreaterThanOrEqual()
    {
        TestEmit(
            GreaterThanOrEqual(Constant(1), Constant(2)),
            expectedResult: false);

        TestEmit(
            GreaterThanOrEqual(Constant(1), Constant(1)),
            expectedResult: true);

        TestEmit(
            GreaterThanOrEqual(Constant(2), Constant(1)),
            expectedResult: true);
    }

    [TestMethod]
    public void TestExpression_Symbol()
    {
        TestEmit(
            TypeOf(Symbol("System.Int32")),
            expectedResult: typeof(int)
            );
    }

    [TestMethod]
    public void TestExpression_This()
    {
        TestEmit(
            Class("C", [
                Method("M", [], Symbol("C"), This()),
                ]),
            Symbol("C").New().Call("M"),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });

        TestEmit(
            Class("C", [
                Field("F", Int32Type, Constant(123)),
                Method("M",[], Int32Type, This().Member("F"))
                ]),
            Symbol("C").New().Member("M").Call(),
            expectedResult: 123
            );

        TestEmit(
            Class("C", [
                Property("P", Int32Type, Constant(123)),
                Method("M",[], Int32Type, This().Member("P"))
                ]),
            Symbol("C").New().Member("P"),
            expectedResult: 123
            );

        TestEmit(
            Class("C", [
                Method("X", [], Int32Type, Constant(123)),
                Method("M",[], Int32Type, This().Member("X").Call())
                ]),
            Symbol("C").New().Member("M").Call(),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestExpression_TypeOf()
    {
        TestEmit(
            TypeOf(Int32Type),
            expectedResult: typeof(int));
    }

    [TestMethod]
    public void TestExpression_Variable()
    {
        // declared w/o initializer
        TestEmit(
            Block(Variable(Int32Type, "x")),
            expectedResult: 0);

        // declared w/ initializer
        TestEmit(
            Block(Variable("x", Constant(3))),
            expectedResult: 3);
    }

    #endregion

    private void TestEmit(Expression test, object? expectedResult = null) =>
        TestEmit([], test, expectedResult);

    private void TestEmit(Declaration declaration, Expression? test = null, object? expectedResult = null) =>
        TestEmit([declaration], test, expectedResult);

    private void TestEmit(Declaration declaration, Expression? test, Action<object?>? fnCheckResult) =>
        TestEmit([declaration], test, fnCheckResult);

    private void TestEmit(List<Declaration> declarations, Expression? test = null, object? expectedResult = null) =>
        TestEmit(declarations, test, actualResult => AssertAreEquivalent(expectedResult, actualResult));

    private void TestEmit(List<Declaration> declarations, Expression? test, Action<object?>? fnCheckResult)
    {
        var binder = new StandardBinder();
        var imports = GetTestSymbols();

        var elements = ImmutableList<SemanticElement>.Empty.AddRange(declarations);

        if (test != null)
        {
            elements = elements.Add(
                Class("Test", [
                     Method("Run", [], ObjectType, test).WithModifiers(SymbolModifier.Static)
                     ]));
        }

        var binding = binder.Bind(elements, imports);
        var dx = binding.Elements.GetContainedDiagnostics();
        if (dx.Count > 0)
        {
            var dxs = string.Join("\n", dx.Select(d => d.ToString()));
            Assert.Fail($"Unexpected diagnostics:\n{dxs}");
        }

        var lowerer = new StandardLowerer();
        var lowering = lowerer.Lower(binding);

        var resultSymbols = TestEmit(lowering, imports, test != null ? "Run" : null, fnCheckResult);

        VerifyDeclarations(resultSymbols, declarations);
    }

    protected abstract SymbolTable GetTestSymbols();

    protected abstract SymbolTable TestEmit(
        SemanticLowering lowering,
        SymbolTable imports,
        string? testMethodName = null,
        Action<object?>? fnCheckResult = null);

    protected void RunTest(Assembly assembly, string testMethodName, Action<object?>? fnCheckResult)
    {
        var testType = assembly.GetType("Test", throwOnError: true);
        Assert.IsNotNull(testType, "Test type not found");
        var testMethod = testType.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(testMethod, "Test.Run not found");
        var testResult = testMethod.Invoke(null, []);

        if (fnCheckResult != null)
        {
            fnCheckResult(testResult);
        }
    }


    /// <summary>
    /// Verify all the declared symbols exist in the results
    /// </summary>
    private void VerifyDeclarations(
        SymbolTable emittedSymbols,
        IEnumerable<Declaration> declarations)
    {
        VerifyAll(declarations);
        
        void VerifyAll(IEnumerable<Declaration> declarations)
        {
            foreach (var decl in declarations)
            {
                Verify(decl);
            }
        }

        void Verify(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                VerifyAll(nd.Declarations);
            }
            else if (decl is TypeDeclaration td && td.Symbol is TypeSymbol ts)
            {
                if (!emittedSymbols.TryGetTypeSymbol(ts.FullName, out _))
                {
                    Assert.Fail($"Did not find type for '{ts.FullName}'");
                }

                VerifyAll(declarations);
            }
            else if (decl is MemberDeclaration md && md.Symbol is MemberSymbol ms)
            {
                if (!emittedSymbols.TryGetSymbol<MemberSymbol>(ms.FullName, out _))
                {
                    Assert.Fail($"Did not find member for '{ms.FullName}'");
                }
            }
        }
    }
}