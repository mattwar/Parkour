using Mono.Cecil;
using System.Collections.Immutable;

namespace Parkour.Cecil;

using Parkour;
using Semantics;

public class CecilEmitting : SemanticEmitting
{
    public AssemblyDefinition Assembly { get; }
    public ModuleDefinition Module { get; }

    public CecilEmitting(
        AssemblyDefinition assembly,
        ModuleDefinition module,
        ImmutableList<Diagnostic> diagnostics)
        : base(diagnostics)
    {
        this.Assembly = assembly;
        this.Module = module;
    }
}
