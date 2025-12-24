using System.Collections.Immutable;
using System.Reflection.Emit;

namespace Parkour.Reflection;

using Parkour;
using Semantics;

public class ReflectionEmitting : SemanticEmitting
{
    public AssemblyBuilder Assembly { get; }
    public ModuleBuilder Module { get; }

    public ReflectionEmitting(
        AssemblyBuilder assembly,
        ModuleBuilder module,
        ImmutableList<Diagnostic> diagnostics)
        : base(diagnostics)
    {
        this.Assembly = assembly;
        this.Module = module;
    }
}
