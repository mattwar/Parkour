namespace Parkour.Expressions;
using Symbols;
using Syntax;

public sealed class FunctionExpression : Expression
{
    public string Name { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public FunctionSymbol? Symbol { get; }
    public TypeSymbol? ReturnType { get; }
    public TargetSymbol? ReturnTarget { get; }

    public FunctionExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        TypeSymbol? returnType,
        FunctionSymbol? symbol,
        TargetSymbol? returnTarget,
        ImmutableList<Diagnostic>? diagnostics,
        SyntaxElement? syntax)
        : base(
            body.State, 
            symbol, 
            diagnostics,
            syntax)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.Symbol = symbol;
        this.ReturnType = returnType;
        this.ReturnTarget = returnTarget;
    }
}

