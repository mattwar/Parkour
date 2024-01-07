namespace Parkour.Expressions;
using Symbols;

public sealed class DeclarationExpression : Expression
{
    public string Name { get; }
    public Expression Initializer { get; }
    public VariableSymbol? Variable { get; }

    public DeclarationExpression(
        string name,
        Expression initializer,
        VariableSymbol? variable,
        TypeSymbol? resultType,
        ImmutableList<Diagnostic>? diagnostics = null)
        : base(initializer.State, resultType, diagnostics)
    {
        this.Name = name;
        this.Initializer = initializer;
        this.Variable = variable;
    }
}

