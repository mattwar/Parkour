using System.Collections.Immutable;
using L = System.Linq.Expressions;

namespace Parkour.Linq;

using Semantics;

public class LinqEmitting : SemanticEmitting
{
    public ImmutableList<L.Expression> Expressions { get; }

    public LinqEmitting(
        ImmutableList<L.Expression> expressions,
        ImmutableList<Diagnostic> diagnostics)
        : base(diagnostics)
    {
        this.Expressions = expressions;
    }
}
