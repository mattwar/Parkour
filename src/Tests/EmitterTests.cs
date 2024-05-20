using System.Reflection;
using Parkour.Binding;
using Parkour.Emitting;
using Parkour.Lowering;
using Parkour.Reflection;
using Parkour.Semantics;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;
using static Tests.TestHelpers;

namespace Tests;

[TestClass]
public class EmitterTests
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
            New(Symbol("C")),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });

        // instance field
        TestEmit(
            Class("C", [Field("F", Int32Type)]),
            New(Symbol("C")).Member("F"),
            0
            );

        // static method
        TestEmit(
            Class("C", [Method("M", SymbolAccess.Public, SymbolModifier.Static, [], Int32Type, Block(Constant(123)))]),
            test: Call(Symbol("C.M")),
            expectedResult: 123
            );

        // instance method
        TestEmit(
            Class("C", [Method("M", [], Int32Type, Block(Constant(123)))]),
            test: New(Symbol("C")).Member("M").Call(),
            expectedResult: 123
            );

        // instance property
        TestEmit(
            Class("C", [Property("P", Int32Type)]),
            New(Symbol("C")).Member("P"),
            expectedResult: 0
            );

        // inside namespace
        TestEmit(
            Namespace("N", [Class("C", [])])
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
            New(Symbol("C")),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });

        // with parameter
        TestEmit(
            Class("C", [Constructor([Parameter("P", Int32Type)], Block())]),
            New(Symbol("C"), [Constant(123)]),
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
            New(Symbol("C")).Member("F"),
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
            New(Symbol("C")).Member("F"),
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
            test: New(Symbol("C")).Member("M").Call(),
            expectedResult: 123
            );

        // static method
        TestEmit(
            Class("C", [Method("M", SymbolAccess.Public, SymbolModifier.Static, [], Int32Type, Block(Constant(123)))]),
            test: Call(Symbol("C.M")),
            expectedResult: 123
            );

        // void method
        TestEmit(
            Class("C", [Method("M", [], VoidType, Block())]),
            test: New(Symbol("C")).Member("M").Call(),
            expectedResult: null
            );

        // with parameters
        TestEmit(
            Class("C", [Method("M", [Parameter("P", Int32Type)], Int32Type, Name("P"))]),
            test: New(Symbol("C")).Member("M").Call([Constant(123)]),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestDeclaration_Property()
    {
        // instance auto property
        TestEmit(
            Class("C", [Property("P", Int32Type)]),
            New(Symbol("C")).Member("P"),
            expectedResult: 0
            );

        // static auto property
        TestEmit(
            Class("C", [Property("P", SymbolAccess.Public, SymbolModifier.Static, Int32Type)]),
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
            New(Symbol("C")).Element(Constant(123)),
            expectedResult: 123
            );

        // static indexer
        TestEmit(
            Class("C", [
                Indexer(SymbolAccess.Public, SymbolModifier.Static, Int32Type, [Parameter("index", Int32Type)], Name("index"), null),
                ]),
            Symbol("C").Element(Constant(123)),
            expectedResult: 123
            );
    }

    [TestMethod]
    public void TestDeclaration_GlobalMethod()
    {
        TestEmit(
            Method("M", [], Int32Type, Block(Constant(1))),
            Call(Name("M")),
            expectedResult: 1
            );
    }
    #endregion

    #region Expressions

    [TestMethod]
    public void TestExpression_Assign()
    {
        // variable assign after initialized
        TestEmit(
            Block(
                Variable("x", Constant(1)),
                Assign(Name("x"), Constant(2))
                ),
            expectedResult: 2);

        // array element
        TestEmit(
            Block(
                Variable("a", NewArray(Int32Type, Constant(2))),
                Assign(Element(Name("a"), Constant(0)), Constant(123))
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
            [Method("M", [], Int32Type, Constant(123))],
            Call(Symbol("M")),
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
            expectedResult: 0);
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
                Assign(Element(Name("a"), Constant(0)), Constant(123))
                ),
            expectedResult: 123);

        // access indexer element
        TestEmit(
            Block(
                Variable("list", New(ListInt32Type, [NewArray(Int32Type, [Constant(7), Constant(11)])])),
                Element(Name("list"), Constant(0))
                ),
            expectedResult: 7);

        // assign indexer element
        TestEmit(
            Block(
                Variable("list", New(ListInt32Type, [NewArray(Int32Type, [Constant(7), Constant(11)])])),
                Assign(Element(Name("list"), Constant(0)), Constant(123))
                ),
            expectedResult: 123);
    }

    [TestMethod]
    public void TestExpression_IsType()
    {
        TestEmit(
            IsType(Constant("Hello"), StringType),
            expectedResult: true);

        TestEmit(
            IsType(Constant(1), Int32Type),
            expectedResult: true);

        TestEmit(
            IsType(Convert(Constant("Hello"), ObjectType), StringType),
            expectedResult: true);

        TestEmit(
            IsType(Convert(Constant(1), ObjectType), Int32Type),
            expectedResult: true);

        TestEmit(
            IsType(Constant("Hello"), Symbol("System.IEquatable`1").Construct([StringType])),
            expectedResult: true);

        TestEmit(
            IsType(Constant(123), Symbol("System.IEquatable`1").Construct([Int32Type])),
            expectedResult: true);

        TestEmit(
            IsType(Constant("Hello"), Int32Type),
            expectedResult: false);

        TestEmit(
            IsType(Constant(1), StringType),
            expectedResult: false);

        TestEmit(
            IsType(Convert(Constant("Hello"), ObjectType), Int32Type),
            expectedResult: false);

        TestEmit(
            IsType(Convert(Constant(1), ObjectType), StringType),
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
            New(Symbol("System.ValueTuple`1").Construct([Int32Type]), [Constant(123)]).Member("Item1"),
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
            New(Symbol("C")).Member("M").Call(),
            expectedResult: 0
            );

        // parameter reference
        TestEmit(
            [Class("C", [Method("M", [Parameter("P", Int32Type)], Int32Type, Name("P"))])],
            New(Symbol("C")).Member("M").Call([Constant(123)]),
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
        TestEmit(
            NewArray(Int32Type, Constant(2)),
            expectedResult: new int[2]
            );

        TestEmit(
            NewArray(Int32Type, []),
            expectedResult: new int[] { }
            );

        TestEmit(
            NewArray(Int32Type, [Constant(1), Constant(2)]),
            expectedResult: new int[] { 1, 2 }
            );

        TestEmit(
            NewMultiDimensionalArray(StringType, [Constant(2), Constant(3)]),
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

        // add withi local variable
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
            [Class("C", [
                Constructor(),
                Method("M", [], Symbol("System.Object"), 
                    This()),
                ])],
            New(Symbol("C")).Call("M"),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });
    }

    [TestMethod]
    public void TestExpression_TypeOf()
    {
        TestEmit(
            TypeOf(Symbol("System.Int32")),
            expectedResult: typeof(int));
    }

    [TestMethod]
    public void TestExpression_Variable()
    {
        // declared w/o initializer
        TestEmit(
            Block(Variable(Symbol("System.Int32"), "x")),
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
        var binder = new StandardSemanticBinder();
        var imports = ReflectionSymbols.CurrentMscorlib;

        if (test != null)
            declarations.Add(
                Class("Test",[
                     Constructor(),
                     Method("Run", SymbolAccess.Public, SymbolModifier.Static, [], Symbol("System.Object"), test)
                     ]));

        var binding = binder.BindDeclarations(declarations.ToImmutableList(), imports);
        if (binding.Diagnostics.Count > 0)
        {
            var dxs = string.Join("\n", binding.Diagnostics.Select(d => d.ToString()));
            Assert.Fail($"Unexpected diagnostics:\n{dxs}");
        }

        var lowerer = new StandardSemanticLowerer();
        var lowering = lowerer.LowerDeclarations(binding.Declarations, imports);

        var emitter = new ReflectionEmitter(imports, "test_assembly");
        var result = emitter.Emit(lowering.Declarations);

        if (result.Diagnostics.Count > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {result.Diagnostics[0]}");
        }

        // verify all delared symbols are represented in the assembly
        VerifyDeclarations(emitter.Assembly, lowering.Declarations);

#if false
        var generator = new Lokad.ILPack.AssemblyGenerator();
        generator.GenerateAssembly(emitter.Assembly, "test_assembly.dll");
#endif

        if (emitter.Module is Module m && test != null)
        {
            var testType = emitter.Module.GetType("Test");
            Assert.IsNotNull(testType, "Test type not found");
            var testMethod = testType.GetMethod("Run", BindingFlags.Public|BindingFlags.Static);
            Assert.IsNotNull(testMethod, "Test.Run not found");
            var testResult = testMethod.Invoke(null, []);

            if (fnCheckResult != null)
            {
                fnCheckResult(testResult);
            }
        }
    }

    private static bool TestIsInt32(object value)
    {
        return value is int;
    }

    private void VerifyDeclarations(Assembly assembly, ImmutableList<Declaration> declarations)
    {
        foreach (var decl in declarations)
        {
            Verify(decl);
        }

        void Verify(Declaration decl)
        {
            if (decl is NamespaceDeclaration nd)
            {
                VerifyDeclarations(assembly, nd.Declarations);
            }
            else if (decl is ClassDeclaration cd
                && cd.Symbol is {} cs)
            {
                var type = assembly.GetType(cs.FullName);
                if (type == null)
                {
                    Assert.Fail($"Did not find type for '{cs.FullName}'");
                }
            }
        }
    }

    private static BindingFlags GetBindingFlags(MemberSymbol symbol)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        
        if (symbol.IsStatic)
            flags |= BindingFlags.Static;
        else
            flags |= BindingFlags.Instance;

        return flags;
    }

    private void VerifySymbols(Type type, Symbol symbol)
    {
        switch (symbol)
        {
            case FieldSymbol fs:
                var fieldInfo = type.GetField(symbol.Name, GetBindingFlags(fs));
                if (fieldInfo == null)
                {
                    Assert.Fail($"Did not find field '{fs.FullName}'");
                }
                break;
            case MethodSymbol ms:
                var methodInfo = type.GetMethod(symbol.Name, GetBindingFlags(ms));
                if (methodInfo == null)
                {
                    Assert.Fail($"Did not find method '{ms.FullName}'");
                }
                break;
            case ConstructorSymbol cs:
                var constructorInfo = type
                    .GetConstructors(GetBindingFlags(cs))
                    .FirstOrDefault(c => c.GetParameters().Length == cs.Parameters.Count);
                if (constructorInfo == null)
                {
                    Assert.Fail($"Did not find constructor '{cs.FullName}'");
                }
                break;
            case PropertySymbol ps:
                var propertyInfo = type
                    .GetProperties(GetBindingFlags(ps))
                    .FirstOrDefault(p => p.GetIndexParameters().Length == 0);
                if (propertyInfo == null)
                {
                    Assert.Fail($"Did not find property '{ps.FullName}'");
                }
                break;
            case IndexerSymbol ins:
                var indexerInfo = type
                    .GetProperties(GetBindingFlags(ins))
                    .FirstOrDefault(p => p.GetIndexParameters().Length > 0);
                if (indexerInfo == null)
                {
                    Assert.Fail($"Did not find indexer '{ins.FullName}'");
                }
                break;

            default:
                throw new InvalidOperationException($"Unhandled symbol kind '{symbol.GetType().Name}' in VerifySymbols");
        }
    }
}