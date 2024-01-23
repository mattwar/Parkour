namespace Parkour.Semantics;
using Symbols;

/// <summary>
/// An expression that declares a variable
/// </summary>
public sealed class VariableExpression : Expression
{
    public string Name { get; }
    public Expression? VariableType { get; }
    public Expression? Initializer { get; }
    public VariableSymbol? Variable { get; }

    public VariableExpression(
        string name,
        Expression? variableType,
        Expression? initializer,
        ISourceLocation? location,
        VariableSymbol? variable,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            OptionalState(variableType)
            | OptionalState(initializer)
            | NotNullState(variable)
            | NotNullState(resultType),
            location,
            resultType, 
            diagnostics)
    {
        this.Name = name;
        this.VariableType = variableType;
        this.Initializer = initializer;
        this.Variable = variable;
    }

    public override int ChildCount => 2;

    public override SemanticElement? GetChild(int index) =>
        index switch
        {
            0 => this.VariableType,
            1 => this.Initializer,
            _ => null
        };
}

