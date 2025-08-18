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
            RunTest(emitter.Assembly, testMethodName, fnCheckResult);
        }

        var resultSymbols = ReflectionSymbols.GetOrCreate(reflectionImports.Assemblies.Add(emitter.Assembly));
        return resultSymbols;
    }
}
