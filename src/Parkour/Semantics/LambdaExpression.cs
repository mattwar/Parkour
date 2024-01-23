namespace Parkour.Semantics;
using Symbols;
using Syntax;

public sealed class LambdaExpression : Expression
{
    public string Name { get; }
    public ImmutableList<ParameterDeclaration> Parameters { get; }
    public Expression Body { get; }
    public LambdaSymbol? LambdaSymbol { get; }
    public TypeSymbol? ReturnType { get; }
    public LabelSymbol? ReturnTarget { get; }

    public LambdaExpression(
        string name,
        ImmutableList<ParameterDeclaration> parameters,
        Expression body,
        ISourceLocation? location,
        TypeSymbol? returnType,
        LambdaSymbol? lambdaSymbol,
        LabelSymbol? returnTarget,
        ImmutableList<Diagnostic>? diagnostics)
        : base(
            CombineState(parameters)
            | body.State
            | NotNullState(returnType)
            | NotNullState(lambdaSymbol)
            | NotNullState(returnTarget),
            location,
            lambdaSymbol,
            diagnostics)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.Body = body;
        this.LambdaSymbol = lambdaSymbol;
        this.ReturnType = returnType;
        this.ReturnTarget = returnTarget;
    }

    public override int ChildCount =>
        this.Parameters.Count + 1;

    public override SemanticElement? GetChild(int index) =>
        index < this.Parameters.Count
            ? this.Parameters[index]
            : this.Body;
}

