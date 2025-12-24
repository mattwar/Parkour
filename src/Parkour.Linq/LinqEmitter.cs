using System.Collections.Immutable;

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

    public override LinqEmitting Emit(
        SemanticLowering lowering)
    {
        var translator = new LinqTranslator(_symbols);
        var exprs = lowering.LoweredElements
            .OfType<Expression>()
            .Select(e => translator.Translate(e))
            .ToImmutableList();
        return new LinqEmitting(
            exprs,
            ImmutableList<Diagnostic>.Empty
            );
    }
}
