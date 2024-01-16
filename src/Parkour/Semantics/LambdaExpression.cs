namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class LambdaExpression : Expression
{
    public string Name { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public FunctionSymbol? Symbol { get; }
    public TypeSymbol? ReturnType { get; }
    public LabelSymbol? ReturnTarget { get; }

    public LambdaExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        TypeSymbol? returnType,
        FunctionSymbol? symbol,
        LabelSymbol? returnTarget,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            body.State,
            location,
            symbol,
            diagnostics)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.Symbol = symbol;
        this.ReturnType = returnType;
        this.ReturnTarget = returnTarget;
    }
}

