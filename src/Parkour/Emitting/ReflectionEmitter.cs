using System.Reflection;
using System.Reflection.Emit;

namespace Parkour.Emitting;
using Binding;
using Symbols;

/// <summary>
/// Emits declarations and expressions into a dynamic assembly.
/// </summary>
public class ReflectionEmitter : DeclarationEmitter
{
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly SemanticEmitter _emitter;

    public ReflectionEmitter(
        string assemblyName,
        SemanticEmitter? emitter = null)
    {
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName(assemblyName),
            AssemblyBuilderAccess.RunAndCollect);
        _emitter = emitter ?? new SemanticEmitter();
    }

    public override Result Emit(DeclarationBinding binding)
    {
        return CreateModuleEmitter().Emit(binding);
    }

    public ModuleEmitter CreateModuleEmitter(string? moduleName = null)
    {
        return new ModuleEmitter(_assemblyBuilder, moduleName, _emitter);
    }

    public class ModuleEmitter : DeclarationEmitter
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly SemanticEmitter _emitter;

        internal ModuleEmitter(
            AssemblyBuilder assemblyBuilder,
            string? moduleName,
            SemanticEmitter emitter)
        {
            _assemblyBuilder = assemblyBuilder;
            moduleName ??= "Module" + _assemblyBuilder.GetModules().Length;
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(moduleName);
            _emitter = emitter;
        }

        public override Result Emit(DeclarationBinding binding)
        {
            if (!RuntimeSymbols.TryGet(binding.ExternalSymbols, out var runtimeSymbols))
                return new Result([new Diagnostic("DeclarationBinding.ExternalSymbols is not RuntimeSymbols")], _assemblyBuilder, _moduleBuilder);

            var diagnostics = new List<Diagnostic>();
            var symbolEmitter = new ReflectionSymbolEmitter(_moduleBuilder, runtimeSymbols, diagnostics);

            _emitter.Emit(binding, symbolEmitter, diagnostics);

            return new Result(diagnostics.ToImmutableList(), _assemblyBuilder, _moduleBuilder);
        }
    }

    /// <summary>
    /// The result of emitting the declarations into a dynamic assembly
    /// </summary>
    public class Result : EmitResult
    {
        /// <summary>
        /// The assembly used or produced by emitting.
        /// </summary>
        public Assembly? Assembly { get; }

        /// <summary>
        /// The module produced by emitting the declarations
        /// </summary>
        public Module? Module { get; }

        internal Result(ImmutableList<Diagnostic>? diagnostics, Assembly? assembly, Module? module)
            : base(diagnostics)
        {
            Assembly = assembly;
            Module = module;
        }
    }
}