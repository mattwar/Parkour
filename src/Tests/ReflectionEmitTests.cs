using System.Reflection;
using System.Reflection.Emit;
using Parkour;
using Parkour.Binding;
using Parkour.Emitting;
using Parkour.Semantics;
using Parkour.Symbols;
using static Parkour.Semantics.SemanticFactory;

namespace Tests;

[TestClass]
public class ReflectionEmitTests
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
    public void TestClassWithField()
    {
        TestEmit(
            Class("C", [Field("F", Symbol("System.Int32"))])
            );
    }

    [TestMethod]
    public void TestClassWithMethod()
    {
        TestEmit(
            Class("C", [Method("M", SymbolAccess.Public, SymbolModifier.Static, [], Symbol("System.Int32"), Block(Constant(1)))]),
            test: Call(Symbol("C.M")),
            expectedResult: 1
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

    private void TestEmit(Declaration declaration, Expression? test = null, object? expectedResult = null) =>
        TestEmit([declaration], test, expectedResult);

    private void TestEmit(List<Declaration> declarations, Expression? test = null, object? expectedResult = null)
    {
        var binder = new SemanticBinder();
        var runtimeSymbols = RuntimeSymbols.CurrentMscorlib;

        if (test != null)
            declarations.Add(Method("TestRun", [], Symbol("System.Object"), test));

        var binding = binder.BindDeclarations(declarations, runtimeSymbols.GlobalNamespace);
        Assert.AreEqual(0, binding.Diagnostics.Count, "diagnostics");

        var emitter = new ReflectionSemanticEmitter();
        var result = emitter.Emit(binding, "test_assembly");

        if (result.Diagnostics.Count > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {result.Diagnostics[0]}");
        }

        Assert.IsNotNull(result.Assembly, "output assembly not produced");

        // verify all delared symbols are represented in the assembly
        VerifySymbols(result.Assembly, binding.DeclarationSymbols);

        if (result.Module is Module m && test != null)
        {
            var testMethod = result.Module.GetMethod("TestRun");
            if (testMethod != null)
            {
                var testResult = testMethod.Invoke(null, []);
                Assert.AreEqual(expectedResult, testResult);
            }
        }
    }

    private void VerifySymbols(Assembly assembly, Symbol symbol)
    {
        switch (symbol)
        {
            case NamespaceSymbol ns:
                foreach (var member in ns.Members)
                {
                    VerifySymbols(assembly, member);
                }
                break;

            case TypeSymbol ts:
                var type = assembly.GetType(ts.FullName);
                if (type == null)
                {
                    Assert.Fail($"Did not find type for '{ts.FullName}'");
                }

                foreach (var member in ts.Members)
                {
                    VerifySymbols(type, member);
                }

                break;
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
            default:
                throw new InvalidOperationException($"Unhandled symbol kind '{symbol.GetType().Name}' in VerifySymbols");
        }
    }
}