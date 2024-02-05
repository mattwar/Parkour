using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Emitting;
using Binding;
using Symbols;

/// <summary>
/// Emits declarations and expressions into a dynamic assembly.
/// </summary>
public class ReflectionSemanticEmitter
{
    public ReflectionSemanticEmitter()
    {
    }

    public virtual EmitResult Emit(
        DeclarationBinding binding, 
        string assemblyName)
    {
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName),
            AssemblyBuilderAccess.RunAndCollect);

        var moduleName = "Module" + assemblyBuilder.GetModules().Length;
        return EmitIntoAssembly(binding, assemblyBuilder, moduleName);
    }

    public virtual EmitResult EmitIntoAssembly(
        DeclarationBinding binding, 
        AssemblyBuilder assemblyBuilder, 
        string moduleName)
    {
        if (!RuntimeSymbols.TryGet(binding.ExternalSymbols, out var runtimeSymbols))
            return new EmitResult(null, null, [new Diagnostic("DeclarationBinding.ExternalSymbols is not RuntimeSymbols")]);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
        var diagnostics = new List<Diagnostic>();
        var symbolEmitter = new ReflectionSymbolEmitter(moduleBuilder, runtimeSymbols, diagnostics);
        var context = new SymbolEmitContext(binding, symbolEmitter, diagnostics);
        var emitter = new SemanticEmitter();
        emitter.Emit(context);

        return new EmitResult(assemblyBuilder, moduleBuilder, diagnostics.ToImmutableList());
    }

    /// <summary>
    /// The result of emitting the declarations into a dynamic assembly
    /// </summary>
    public class EmitResult
    {
        /// <summary>
        /// The assembly used or produced by emitting.
        /// </summary>
        public Assembly? Assembly { get; }

        /// <summary>
        /// The module produced by emitting the declarations
        /// </summary>
        public Module? Module { get; }

        /// <summary>
        /// Any diagnostics produced during emit.
        /// </summary>
        public ImmutableList<Diagnostic> Diagnostics { get; }

        internal EmitResult(Assembly? assembly, Module? module, ImmutableList<Diagnostic>? diagnostics)
        {
            Assembly = assembly;
            Module = module;
            Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}