namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// An expression that declares a variable
/// </summary>
public sealed class VariableExpression : Expression
{
    protected internal override string DebugText =>
        $"{nameof(VariableExpression)}: {Name}";

    public string Name { get; }
    public Expression? VariableType { get; }
    public Expression? Initializer { get; }
    public VariableSymbol? VariableSymbol { get; }

    private VariableExpression(
        string name,
        Expression? variableType,
        Expression? initializer,
        ISourceLocation? location,
        VariableSymbol? variable,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            State(variableType)
            | State(initializer)
            | NotNullState(variable)
            | NotNullState(resultType),
            location,
            resultType, 
            diagnostics)
    {
        this.Name = name;
        this.VariableType = variableType;
        this.Initializer = initializer;
        this.VariableSymbol = variable;
    }

    public VariableExpression(
        string name,
        Expression? variableType,
        Expression? initializer,
        ISourceLocation? location)
        : this(name, variableType, initializer, location, null, null, null)
    {
    }

    public override VariableExpression WithLocation(ISourceLocation? location) =>
        location == this.Location ? this :
        new VariableExpression(
            this.Name,
            this.VariableType,
            this.Initializer,
            location,
            this.VariableSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override VariableExpression WithDiagnostics(ImmutableList<Diagnostic> diagnostics) =>
        diagnostics == this.Diagnostics ? this :
        new VariableExpression(
            this.Name,
            this.VariableType,
            this.Initializer,
            this.Location,
            this.VariableSymbol,
            this.ResultType,
            diagnostics
            );

    public override VariableExpression WithResultType(TypeSymbol? resultType) =>
        resultType == this.ResultType ? this :
        new VariableExpression(
            this.Name,
            this.VariableType,
            this.Initializer,
            this.Location,
            this.VariableSymbol,
            resultType,
            this.Diagnostics
            );

    public VariableExpression WithName(string name) =>
        name == this.Name ? this :
        new VariableExpression(
            name,
            this.VariableType,
            this.Initializer,
            this.Location,
            this.VariableSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public VariableExpression WithVariableType(Expression? variableType) =>
        variableType == this.VariableType ? this :
        new VariableExpression(
            this.Name,
            variableType,
            this.Initializer,
            this.Location,
            this.VariableSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public VariableExpression WithInitializer(Expression? initializer) =>
        initializer == this.Initializer ? this :
        new VariableExpression(
            this.Name,
            this.VariableType,
            initializer,
            this.Location,
            this.VariableSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public VariableExpression WithVariableSymbol(VariableSymbol? variableSymbol) =>
        variableSymbol == this.VariableSymbol ? this :
        new VariableExpression(
            this.Name,
            this.VariableType,
            this.Initializer,
            this.Location,
            variableSymbol,
            this.ResultType,
            this.Diagnostics
            );

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.VariableType,
            1 => this.Initializer,
            _ => null
        };

    public override VariableExpression RewriteChildren(SemanticRewriter rewriter)
    {
        var variableType = rewriter.Rewrite(this.VariableType);
        var initializer = rewriter.Rewrite(this.Initializer);
        return this
            .WithVariableType(variableType!)
            .WithInitializer(initializer!);
    }
}