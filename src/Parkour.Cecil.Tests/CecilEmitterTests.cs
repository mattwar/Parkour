using Mono.Cecil;
using Parkour.Cecil;
using Parkour.Semantics;
using Parkour.Symbols;
using System.Reflection;
using System.Runtime.Loader;

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

        if (testMethodName != null)
        {
            // write to memory stream and load into runtime
            var stream = new MemoryStream();

            var name = new AssemblyName(typeof(int).Assembly.FullName!);
            new AssemblyNameReference(name.Name, name.Version);
            emitter.Module.AssemblyReferences.Add(new AssemblyNameReference(name.Name, name.Version));

#if true
            emitter.Assembly.Write(stream);
            stream.Position = 0;
            var loadContext = new AssemblyLoadContext("test_context", isCollectible: true);
            var assembly = loadContext.LoadFromStream(stream);
#else

            var path = Path.Combine(Environment.CurrentDirectory, "test_assembly.dll");
            emitter.Assembly.Write(path);
            var loadContext = new AssemblyLoadContext("test_context", isCollectible: true);
            //var assembly = loadContext.LoadFromStream(stream);
            var assembly = loadContext.LoadFromAssemblyPath(path);
#endif

            RunTest(assembly, testMethodName, fnCheckResult);
        }

        var resultSymbols = CecilSymbols.GetOrCreate(cecilImports.Assemblies.Add(emitter.Assembly));
        return resultSymbols;
    }
}
