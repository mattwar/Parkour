using Parkour.Cecil;
using Parkour.Semantics;
using Parkour.Symbols;
using Mono.Cecil;

namespace Tests;

[TestClass]
public class CecilEmitterTests : EmitterTests
{
    protected override SymbolTable GetTestSymbols() =>
        CecilSymbols.CurrentMscorlib;

    protected override SymbolTable TestEmit(
        SemanticLowering lowering,
        SymbolTable imports,
        string? testMethodName = null,
        Action<object?>? fnCheckResult = null)
    {
        var cecilImports = (CecilSymbols)imports;
        var emitter = new CecilEmitter(cecilImports, "test_assembly");
        var result = emitter.Emit(lowering);

        if (result.Diagnostics.Count > 0)
        {
            Assert.Fail($"Unexpected diagnostic: {result.Diagnostics[0]}");
        }

        var resultSymbols = CecilSymbols.GetOrCreate(cecilImports.Assemblies.Add(emitter.Assembly));
        return resultSymbols;
    }
}
