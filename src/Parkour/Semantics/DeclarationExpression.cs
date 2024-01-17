namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class DeclarationExpression : Expression
{
    public string Name { get; }
    public Expression? VariableType { get; }
    public Expression? Initializer { get; }
    public VariableSymbol? Variable { get; }

    public DeclarationExpression(
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
}

