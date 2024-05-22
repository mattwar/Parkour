using System.Collections.Immutable;
using L = System.Linq.Expressions;

namespace Parkour.Linq;

using Reflection;
using Semantics;

/// <summary>
/// Emits one or more lowered semantic expressions into linq expressions.
/// </summary>
public class LinqEmitter : SemanticEmitter
{
    private readonly ReflectionSymbols _symbols;

    public LinqEmitter(
        ReflectionSymbols symbols)
    {
        _symbols = symbols;
    }

    public override LinqEmitResult Emit(
        ImmutableList<SemanticElement> elements)
    {
        var translator = new LinqTranslator(_symbols);
        var exprs = elements
            .OfType<Expression>()
            .Select(e => translator.Translate(e))
            .ToImmutableList();
        return new LinqEmitResult(
            exprs,
            ImmutableList<Diagnostic>.Empty
            );
    }

    public class LinqEmitResult : EmitResult
    {
        public ImmutableList<L.Expression> Expressions { get; }

        public LinqEmitResult(
            ImmutableList<L.Expression> expressions,
            ImmutableList<Diagnostic> diagnostics)
            : base(diagnostics)
        {
            this.Expressions = expressions;
        }
    }
}