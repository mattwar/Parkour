using Parkour.Symbols;

namespace Parkour.Semantics;

/// <summary>
/// Accesses a member of the instance expression.
/// If the instance expression is a type expression, a static member can be accessed.
/// </summary>
public sealed class MemberExpression : Expression
{
    internal protected override string DebugText => 
        $"{GetType().Name}: {this.Name}: {ResultType?.FullName ?? "???"}";

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

    public override MemberExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new MemberExpression(
            this.Instance,
            this.Name,
            location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override MemberExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new MemberExpression(
            this.Instance,
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            diagnostics
            );

    public override MemberExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new MemberExpression(
            this.Instance,
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            resultType,
            this.Diagnostics
            );

    public MemberExpression WithInstance(Expression instance) =>
        instance == this.Instance ? this :
        new MemberExpression(
            instance,
            this.Name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public MemberExpression WithName(string name) =>
        name == this.Name ? this :
        new MemberExpression(
            this.Instance,
            name,
            this.Location,
            this.ReferencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public MemberExpression WithReferencedSymbol(Symbol? referencedSymbol) =>
        referencedSymbol == this.ReferencedSymbol ? this :
        new MemberExpression(
            this.Instance,
            this.Name,
            this.Location,
            referencedSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 1;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.Instance,
            _ => null
        };

    public override MemberExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var instance = rewriter.Rewrite(this.Instance);
        return this.WithInstance(instance!);
    }
}