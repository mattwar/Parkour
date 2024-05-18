using Parkour.Symbols;

namespace Parkour.Semantics;

/// <summary>
/// Accesses a member of the instance expression.
/// If the instance expression is a type expression, a static member can be accessed.
/// </summary>
public sealed class MemberExpression : Expression
{
    public Expression Instance { get; }
    public string Name { get; }
    public override Symbol? ReferencedSymbol { get; }

    public MemberExpression(
        Expression instance,
        string name,
        ISourceLocation? location,
        Symbol? referencedSymbol,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(instance)
            | NotNullOrDiagnosticState(referencedSymbol, diagnostics)
            | NotNullState(resultType),
            location,
            resultType,
            diagnostics)
    {
        this.Instance = instance;
        this.Name = name;
        this.ReferencedSymbol = referencedSymbol;
    }

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Instance,
            _ => null
        };
}

