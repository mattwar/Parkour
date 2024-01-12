namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class DeclarationExpression : Expression
{
    public string Name { get; }
    public Expression Initializer { get; }
    public VariableSymbol? Variable { get; }

    public DeclarationExpression(
        string name,
        Expression initializer,
        ISourceLocation? location,
        VariableSymbol? variable,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            initializer.State, 
            location,
            resultType, 
            diagnostics)
    {
        this.Name = name;
        this.Initializer = initializer;
        this.Variable = variable;
    }
}

