namespace Parkour.Emitting;

using Symbols;

/// <summary>
/// Emits types as a module.
/// </summary>
public abstract class ModuleEmitter
{
    /// <summary>
    /// Emits all symbols in the global namespace as a module.
    /// </summary>
    public abstract EmitResult EmitModule(
        GlobalNamespaceSymbol declaredSymbols,
        Action<MemberSymbol, ILEmitter> fnBuildBody);

    public class EmitResult
    {
        public ImmutableList<Diagnostic> Diagnostics { get; }

        public EmitResult(ImmutableList<Diagnostic>? diagnostics)
        {
            this.Diagnostics = diagnostics ?? ImmutableList<Diagnostic>.Empty;
        }
    }
}

