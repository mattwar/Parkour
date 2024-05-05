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
    [TestMethod]
    public void TestEmptyClass()
    {
        TestEmit(
            Class("C", [])
            );

        TestEmit(
            Namespace("N", [Class("C", [])])
            );
    }

    [TestMethod]
    public void TestDeclareClassWithConstructor()
    {
        TestEmit(
            Class("C", [Constructor()]),
            New(Symbol("C")),
            result =>
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("C", result.GetType().Name);
            });
    }

    [TestMethod]
    public void TestDeclareClassWithInstanceField()
    {
        TestEmit(
            Class("C", [Constructor(), Field("F", Symbol("System.Int32"))]),
            New(Symbol("C")).Member("F"),
            0
            );
    }

    [TestMethod]
    public void TestDeclareClassWithStaticMethod()
    {
        TestEmit(
            Class("C", [Method("M", SymbolAccess.Public, SymbolModifier.Static, [], Symbol("System.Int32"), Block(Constant(1)))]),
            test: Call(Symbol("C.M")),
            expectedResult: 1
            );
    }

    [TestMethod]
    public void TestDeclareClassWithInstanceProperty()
    {
        TestEmit(
            Class("C", [Constructor(), Property("P", Symbol("System.Int32"))]),
            New(Symbol("C")).Member("P"),
            0
            );
    }

    [TestMethod]
    public void TestGlobalMethod()
    {
        TestEmit(
            Method("M", [], Symbol("System.Int32"), Block(Constant(1))),
            Call(Name("M")),
            expectedResult: 1
            );
    }

    [TestMethod]
    public void TestConstant()
    {
        TestEmit(Constant(1), expectedResult: 1);
    }

    [TestMethod]
    public void TestAdd()
    {
        TestEmit(
            Add(Constant(1), Constant(2)), 
            3);

        // + on strings is concat
        TestEmit(
            Add(Constant("one"), Constant("two")), 
            "onetwo");

        // add withi local variable
        TestEmit(
            Block(
                Variable("x", Constant(1)),
                Add(Name("x"), Constant(2))),
            3);
    }

    [TestMethod]
    public void TestAssign()
    {
        // assign after initialized
        TestEmit(
            Block(
                Variable("x", Constant(1)),
                Assign(Name("x"), Constant(2))
                ),
            2);
    }

    [TestMethod]
    public void TestAssignArrayElement()
    {
        TestEmit(
            Block(
                Variable("a", NewArray(Symbol("System.Int32"), Constant(2))),
                Assign(Element(Name("a"), Constant(0)), Constant(123))
                ),
            123);
    }

    [TestMethod]
    public void TestSubtract()
    {
        TestEmit(Subtract(Constant(3), Constant(1)), 2);
    }

    [TestMethod]
    public void TestMultiply()
    {
        TestEmit(Multiply(Constant(3), Constant(2)), 6);
    }

    [TestMethod]
    public void TestDivide()
    {
        TestEmit(Divide(Constant(6), Constant(2)), 3);
        TestEmit(Divide(Constant(5), Constant(2)), 2);
    }

    [TestMethod]
    public void TestRemainder()
    {
        TestEmit(Remainder(Constant(6), Constant(2)), 0);
        TestEmit(Remainder(Constant(5), Constant(2)), 1);
    }

    [TestMethod]
    public void TestBitwiseAnd()
    {
        TestEmit(BitwiseAnd(Constant(7), Constant(3)), 3);
        TestEmit(BitwiseAnd(Constant(5), Constant(3)), 1);
        TestEmit(BitwiseAnd(Constant(1), Constant(2)), 0);
    }

    [TestMethod]
    public void TestEqual()
    {
        TestEmit(Equal(Constant(1), Constant(2)), false);
        TestEmit(Equal(Constant(1), Constant(1)), true);
    }

    [TestMethod]
    public void TestNotEqual()
    {
        TestEmit(NotEqual(Constant(1), Constant(2)), true);
        TestEmit(NotEqual(Constant(1), Constant(1)), false);
    }

    [TestMethod]
    public void TestLessThan()
    {
        TestEmit(LessThan(Constant(1), Constant(2)), true);
        TestEmit(LessThan(Constant(1), Constant(1)), false);
        TestEmit(LessThan(Constant(2), Constant(1)), false);
    }

    [TestMethod]
    public void TestLessThanOrEqual()
    {
        TestEmit(LessThanOrEqual(Constant(1), Constant(2)), true);
        TestEmit(LessThanOrEqual(Constant(1), Constant(1)), true);
        TestEmit(LessThanOrEqual(Constant(2), Constant(1)), false);
    }

    [TestMethod]
    public void TestGreaterThan()
    {
        TestEmit(GreaterThan(Constant(1), Constant(2)), false);
        TestEmit(GreaterThan(Constant(1), Constant(1)), false);
        TestEmit(GreaterThan(Constant(2), Constant(1)), true);
    }

    [TestMethod]
    public void TestGreaterThanOrEqual()
    {
        TestEmit(GreaterThanOrEqual(Constant(1), Constant(2)), false);
        TestEmit(GreaterThanOrEqual(Constant(1), Constant(1)), true);
        TestEmit(GreaterThanOrEqual(Constant(2), Constant(1)), true);
    }

    [TestMethod]
    public void TestCondition()
    {
        TestEmit(Condition(Constant(true), Constant(1), Constant(2)), 1);
        TestEmit(Condition(Constant(false), Constant(1), Constant(2)), 2);
        TestEmit(Condition(Constant(false), Constant(1), Default(Symbol("System.Int32"))), 0);
        TestEmit(Condition(Constant(true), Constant(5)), 5);
        TestEmit(Condition(Constant(false), Constant(5)), 0);
    }

    [TestMethod]
    public void TestLoop()
    {
        TestEmit(Loop(Break(Constant(123))), 123);
    }

    [TestMethod]
    public void TestNew()
    {
        // new object
        TestEmit(
            New(Symbol("System.Object")),
            new object());

        // new generic type
        TestEmit(
            New(Symbol("System.Collections.Generic.List`1").Construct([Symbol("System.Int32")])),
            new List<int>());

        // new generic type with constructor args
        TestEmit(
            New(
                Symbol("System.Collections.Generic.List`1").Construct([Symbol("System.Int32")]),
                [NewArray(Symbol("System.Int32"), [Constant(7), Constant(11)])]),
            new List<int>() { 7, 11 }
            );
    }

    [TestMethod]
    public void TestNewArray()
    {
        TestEmit(
            NewArray(Symbol("System.Int32"), Constant(2)),
            new int[2]
            );

        TestEmit(
            NewArray(Symbol("System.Int32"), []),
            new int[] { }
            );

        TestEmit(
            NewArray(Symbol("System.Int32"), [Constant(1), Constant(2)]),
            new int[] { 1, 2 }
            );
    }

    [TestMethod]
    public void TestElement()
    {
        // access sz array element
        TestEmit(
            Block(
                Variable("a", NewArray(Symbol("System.Int32"), [Constant(5)])),
                Name("a").Element(Constant(0))
                ),
            5);

        // assign sz array element
        TestEmit(
            Block(
                Variable("a", NewArray(Symbol("System.Int32"), Constant(2))),
                Assign(Element(Name("a"), Constant(0)), Constant(123))
                ),
            123);

        // access indexer element
        TestEmit(
            Block(
                Variable("list", 
                    New(Symbol("System.Collections.Generic.List`1").Construct([Symbol("System.Int32")]),
                        [NewArray(Symbol("System.Int32"), [Constant(7), Constant(11)])])
                    ),
                Element(Name("list"), Constant(0))
                ),
            7);
    }

    [TestMethod]
    public void TestWhile()
    {
        TestEmit(While(Constant(false), Block()), null);
        TestEmit(While(Constant(true), Break(Constant(123))), 123);
    }

    [TestMethod]
    public void TestFor()
    {
        TestEmit(For("x", Constant(1), Constant(10), Constant(1), Block()), 10);
    }

    [TestMethod]
    public void TestThis()
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
    public void TestVariable()
    {
        // declared w/o initializer
        TestEmit(Block(Variable(Symbol("System.Int32"), "x")), 0);

        // declared w/ initializer
        TestEmit(Block(Variable("x", Constant(3))), 3);
    }

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
        var binder = new StandardDeclarationBinder();
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

        var lowerer = new StandardDeclarationLowerer();
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
        generator.GenerateAssembly(builder.Assembly, "test_assembly.dll");
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
                && cd.ClassSymbol is ClassSymbol cs)
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