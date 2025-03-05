using System.Reflection;
using Parkour.Reflection;
using Parkour.Semantics;
using Parkour.Symbols;

namespace Tests;

[TestClass]
public class ReflectionEmitterTests : EmitterTests
{
    protected override SymbolTable GetTestSymbols() =>
        ReflectionSymbols.CurrentMscorlib;

    protected override SymbolTable TestEmit(
        SemanticLowering lowering,
        SymbolTable imports,
        string? testMethodName = null,
        Action<object?>? fnCheckResult = null)
    {
        var reflectionImports = (ReflectionSymbols)imports;
        var emitter = new ReflectionEmitter(reflectionImports, "test_assembly");
        var result = emitter.Emit(lowering);

        if (result.Diagnostics.Count > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {result.Diagnostics[0]}");
        }

#if false
        var generator = new Lokad.ILPack.AssemblyGenerator();
        generator.GenerateAssembly(emitter.Assembly, "test_assembly.dll");
#endif

        if (emitter.Module is Module m && testMethodName != null)
        {
            var testType = emitter.Module.GetType("Test");
            Assert.IsNotNull(testType, "Test type not found");
            var testMethod = testType.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(testMethod, "Test.Run not found");
            var testResult = testMethod.Invoke(null, []);

            if (fnCheckResult != null)
            {
                fnCheckResult(testResult);
            }
        }

        var resultSymbols = ReflectionSymbols.GetOrCreate(reflectionImports.Assemblies.Add(emitter.Assembly));
        return resultSymbols;
    }
}
